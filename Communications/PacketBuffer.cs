using System;
using System.Collections.Generic;
using Scarlet.Communications;

namespace Scarlet.Communications
{
    /// <summary> This defines the interface of packet buffer. </summary>
    public abstract class PacketBuffer
    {
        /// <summary> Add a packet to buffer. </summary>
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        public abstract void Enqueue(Packet Packet, int Priority);

        /// <summary> Get next packet without removing it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public abstract Packet Peek();

        /// <summary> Get next packet and remove it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public abstract Packet Dequeue();

        /// <summary> Get the total number of packets in the buffer. </summary>
        public abstract int Count { get; }

        /// <summary> Return whether the buffer is empty </summary>
        /// <returns> true if buffer is empty, false otherwise. </returns>
        public bool IsEmpty() { return Peek() == null; }
    }

    /// <summary> This class is a basic packet buffer. It is just a queue with thread safety guarantee. </summary>
    public class QueueBuffer : PacketBuffer
    {
        private readonly Queue<Packet> Queue; // Packet queue

        public QueueBuffer()
        {
            this.Queue = new Queue<Packet>();
        }

        /// <summary> Add a packet to buffer. </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Should always be zero because there's just one queue. </param>
        /// 
        /// <exception cref="ArgumentException"> If priority is not zero since there's just one queue. </exception>
        public override void Enqueue(Packet Packet, int Priority = 0)
        {
            if (Priority != 0) { throw new ArgumentException("Leaky bucket controller doesn't have priority."); }
            lock (this.Queue) { this.Queue.Enqueue(Packet); }
        }

        /// <summary> Get next packet without removing it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            lock (this.Queue)
            {
                try { return this.Queue.Peek(); }
                catch (InvalidOperationException) { return null; }
            }
        }

        /// <summary> Get next packet and remove it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            lock (this.Queue)
            {
                return (this.Queue.Count > 0) ? this.Queue.Dequeue() : null;
                //try { return this.Queue.Dequeue(); }
                //catch (InvalidOperationException) { return null; }
            }
        }

        /// <summary> Get the total number of packets in the buffer. </summary>
        public override int Count
        {
            get
            {
                lock (this.Queue) { return this.Queue.Count; }
            }
        }
    }

    /// <summary> This defines a priority buffer that dequeue the highest priority packet first. </summary>
    public class PriorityBuffer : PacketBuffer
    {
        private readonly PacketBuffer[] Buffers; // List of buffers of each priority
        public readonly int NBuffers; // Number of buffers

        /// <summary> Construct priority buffer with a list of sub-buffers. </summary>
        /// <param name="Buffers"> List of buffers for each priority. Low index means higher priority. </param>
        public PriorityBuffer(PacketBuffer[] Buffers)
        {
            this.Buffers = Buffers;
            this.NBuffers = Buffers.Length;
        }

        /// <summary> Add a packet to buffer. </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. Lower number means higher priority. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= this.NBuffers) { throw new ArgumentOutOfRangeException("Priority number is out of range"); }
            this.Buffers[Priority].Enqueue(Packet, 0);
        }

        /// <summary> Get next packet without removing it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            for (int i = 0; i < this.NBuffers; i++)
            {
                Packet Next = this.Buffers[i].Peek();
                if (Next != null) { return Next; }
            }
            return null;
        }

        /// <summary> Get next packet and remove it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            for (int i = 0; i < this.NBuffers; i++)
            {
                Packet Next = this.Buffers[i].Dequeue();
                if (Next != null) { return Next; }
            }
            return null;
        }

        /// <summary> Get the total number of packets in the buffer. </summary>
        public override int Count
        {
            get
            {
                int Sum = 0;
                foreach (PacketBuffer Buffer in this.Buffers) { Sum += Buffer.Count; }
                return Sum;
            }
        }
    }

    /// <summary> This defines a bandwidth control buffer that controlls the bandwidth for each priority. </summary>
    public class BandwidthControlBuffer : PacketBuffer
    {
        private readonly PacketBuffer[] Buffers; // List of buffers for each priority
        private readonly int[] BandwidthAllocations; // Bandwidth allocation for each priority
        private readonly int NBuffers; // Number of buffers
        private readonly int[] TokenBuckets; // Token bucket for each priority
        private int MaximumToken; // Maximum token bucket size for each priority
        private int CurrentBucket; // Current priority that is being sent
        private Packet PacketCache; // Cache for last peeked packet.

        /// <summary> Construct a controller. </summary>
        /// 
        /// <remarks> If `BandwidthAllocation` is null, it will be set to all 1's. </remarks>
        /// 
        /// <param name="Buffers"> List of buffers for each priority. </param>
        /// <param name="BandwidthAllocation"> List of integers that represent the portion of bandwidth assigned to each priority. </param>
        /// <param name="MaximumToken"> Maximum token bucket size. </param>
        /// 
        /// <exception cref="ArgumentException"> If length of `Buffers` and `BandwidthAllocation` is not equal. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If maximum token bucket size is not positive. </exception>
        public BandwidthControlBuffer(PacketBuffer[] Buffers, int[] BandwidthAllocation = null, int MaximumToken = 256)
        {
            // Default value for `BandwidthAllocation`
            if (BandwidthAllocation == null)
            {
                BandwidthAllocation = new int[Buffers.Length];
                for (int i = 0; i < BandwidthAllocation.Length; i++) { BandwidthAllocation[i] = 1; }
            }

            if (Buffers.Length != BandwidthAllocation.Length) { throw new ArgumentException("Number of bandwith assignment doesn't match number of Buffers"); }
            if (MaximumToken <= 0) { throw new ArgumentOutOfRangeException("Maximum token should be greater than zero"); }

            this.Buffers = Buffers;
            this.BandwidthAllocations = BandwidthAllocation;
            this.MaximumToken = MaximumToken;
            this.NBuffers = Buffers.Length;
            this.CurrentBucket = 0;
            this.PacketCache = null;

            this.TokenBuckets = new int[this.NBuffers];
            for (int i = 0; i < this.NBuffers; i++) { this.TokenBuckets[i] = 0; }
        }

        /// <summary> Add `Multiple * BandwidthAllocation[i]` tokens to each token bucket. </summary>
        /// <param name="Multiple"> How many tokens to add. </param>
        private void AddToken(int Multiple)
        {
            for (int i = 0; i < this.NBuffers; i++) { this.TokenBuckets[i] = Math.Min(this.TokenBuckets[i] + Multiple * BandwidthAllocations[i], this.MaximumToken); }
        }

        /// <summary>
        /// Find next bucket that have both packet and sufficient token.
        /// If no bucket have both packet and sufficient token, find the bucket with packet but insufficient token.
        /// </summary>
        /// 
        /// <remarks> It changes `CurrentBucket` to next candidate. </remarks>
        /// 
        /// <returns> true if all buffers are empty. </returns>
        private bool NextBucket()
        {
            // Find the bucket with packet and sufficient token
            int Previous = this.CurrentBucket;
            do
            {
                this.CurrentBucket = this.CurrentBucket + 1;
                this.CurrentBucket = this.CurrentBucket >= this.NBuffers ? 0 : this.CurrentBucket;
            }
            while ((this.CurrentBucket != Previous) &&
                   (this.TokenBuckets[this.CurrentBucket] < 0 || this.Buffers[this.CurrentBucket].Peek() == null));

            if (this.CurrentBucket != Previous) { return false; } // Found the packet
            else
            {
                // Find the bucket with packet
                for (this.CurrentBucket = 0; this.CurrentBucket < this.NBuffers; this.CurrentBucket++)
                {
                    if (this.Buffers[this.CurrentBucket].Peek() != null) { return false; }
                }

                // Didn't find anything
                this.CurrentBucket = 0;
                return true;
            }
        }

        /// <summary> Add a packet to buffer. </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= this.NBuffers) { throw new ArgumentOutOfRangeException("Priority number is out of range"); }
            this.Buffers[Priority].Enqueue(Packet, 0);
        }

        /// <summary> Get next packet without removing it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            if (this.PacketCache == null) { this.PacketCache = Dequeue(); } // Store next packet into cache so it can be removed on next call of dequeue
            return this.PacketCache;
        }

        /// <summary> Get next packet and remove it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            // Use the packet stored in cache if available
            if (this.PacketCache != null)
            {
                Packet LastPacket = this.PacketCache;
                this.PacketCache = null;
                return LastPacket;
            }

            // Find next packet
            if (this.TokenBuckets[this.CurrentBucket] < 0 || this.Buffers[this.CurrentBucket].Peek() == null)
            {
                if (NextBucket()) { return null; } // Can't find any packet
            }

            // Update token bucket
            Packet Next = this.Buffers[this.CurrentBucket].Dequeue();
            this.TokenBuckets[this.CurrentBucket] -= Next.GetLength();

            // Refill token bucket if it is not enough
            if (this.TokenBuckets[this.CurrentBucket] < 0)
            {
                AddToken(1 + (-this.TokenBuckets[this.CurrentBucket]) / this.BandwidthAllocations[this.CurrentBucket]);

                // Switch to the buffer of next prioirity
                this.CurrentBucket++;
                this.CurrentBucket = this.CurrentBucket >= this.NBuffers ? 0 : this.CurrentBucket;
            }
            return Next;
        }

        /// <summary> Get the total number of packets in the buffer. </summary>
        public override int Count
        {
            get
            {
                int Sum = 0;
                foreach (PacketBuffer Buffer in this.Buffers) { Sum += Buffer.Count; }
                return Sum;
            }
        }
    }

    /// <summary>
    /// This defines a generic controller.
    /// The order of absolute priority is EMERGENT > HIGH == MEDIUM == LOW > LOWEST,
    /// and the bandwidth allocation of HIGH : MEDIUM : LOW is 3 : 2 : 1.
    /// </summary>
    public class GenericController : PacketBuffer
    {
        public static readonly int N_PRIORITIES = 5;

        public readonly QueueBuffer[] Buffers;
        public readonly PriorityBuffer PriorityBuffer;
        public readonly BandwidthControlBuffer BandwidthBuffer;

        /// <summary> Construct a generic Controller. </summary>
        public GenericController()
        {
            this.Buffers = new QueueBuffer[N_PRIORITIES];
            for (int i = 0; i < N_PRIORITIES; i++) { this.Buffers[i] = new QueueBuffer(); }

            PacketBuffer[] BandwidthSubcontrollers = { this.Buffers[1], this.Buffers[2], this.Buffers[3] };
            int[] BandwidthAllocation = { 3, 2, 1 };
            this.BandwidthBuffer = new BandwidthControlBuffer(BandwidthSubcontrollers, BandwidthAllocation);

            PacketBuffer[] PrioritySubcontrollers = { this.Buffers[0], this.BandwidthBuffer, this.Buffers[4] };
            this.PriorityBuffer = new PriorityBuffer(PrioritySubcontrollers);
        }

        /// <summary> Add a packet to buffer. </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= N_PRIORITIES) { throw new ArgumentOutOfRangeException("Priority number is out of range"); }
            this.Buffers[Priority].Enqueue(Packet);
        }

        /// <summary> Add a packet to buffer. </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public void Enqueue(Packet Packet, PacketPriority Priority = PacketPriority.MEDIUM)
        {
            if (Priority < 0 || (int)Priority >= N_PRIORITIES) { throw new ArgumentOutOfRangeException("Priority number is out of range"); }
            this.Buffers[(int)Priority].Enqueue(Packet);
        }

        /// <summary> Get next packet without removing it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek() { return this.PriorityBuffer.Peek(); }


        /// <summary> Get next packet and remove it. </summary>
        /// <remarks> It will return null if the buffer is empty instead of throwing an exception. </remarks>
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue() { return this.PriorityBuffer.Dequeue(); }

        /// <summary> Get the total number of packets in the buffer. </summary>
        public override int Count
        {
            get { return this.PriorityBuffer.Count; }
        }
    }
}
