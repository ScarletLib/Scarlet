using System;
using System.Collections.Generic;
using Scarlet.Communications;

namespace Scarlet.Communications
{
    /// <summary>
    /// This defines the interface of packet buffer.
    /// </summary>
    public abstract class PacketBuffer
    {
        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        public abstract void Enqueue(Packet Packet, int Priority);

        /// <summary>
        /// Get next packet without removing it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public abstract Packet Peek();

        /// <summary>
        /// Get next packet and remove it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public abstract Packet Dequeue();

        /// <summary>
        /// Get the total number of packets in the buffer.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Return whether the buffer is empty
        /// </summary>
        /// <returns> true if buffer is empty, false otherwise. </returns>
        public bool IsEmpty()
        {
            return Peek() == null;
        }
    }

    /// <summary>
    /// This class is a basic packet buffer. It is just a queue with thread safty guarantee.
    /// </summary>
    public class QueueBuffer : PacketBuffer
    {
        private readonly Queue<Packet> Queue; // Packet queue

        public QueueBuffer()
        {
            Queue = new Queue<Packet>();
        }

        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Should always be zero because there's just one queue. </param>
        /// 
        /// <exception cref="ArgumentException"> If priority is not zero since there's just one queue. </exception>
        public override void Enqueue(Packet Packet, int Priority = 0)
        {
            if (Priority != 0)
                throw new ArgumentException("Leaky bucket controller doesn't have priority.");

            lock (Queue)
            {
                Queue.Enqueue(Packet);
            }
        }

        /// <summary>
        /// Get next packet without removing it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            lock (Queue)
            {
                try
                {
                    return Queue.Peek();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get next packet and remove it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            lock (Queue)
            {
                try
                {
                    return Queue.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the total number of packets in the buffer.
        /// </summary>
        public override int Count
        {
            get
            {
                lock (Queue)
                {
                    return Queue.Count;
                }
            }
        }
    }

    /// <summary>
    /// This defines a priority buffer that dequeue the highest priority packet first.
    /// </summary>
    public class PriorityBuffer : PacketBuffer
    {
        private readonly PacketBuffer[] Buffers; // List of buffers of each priority
        public readonly int NBuffers; // Number of buffers

        /// <summary>
        /// Construct priority buffer with a list of sub-buffers.
        /// </summary>
        /// 
        /// <param name="Buffers"> List of buffers for each priority. Low index means higher priority. </param>
        public PriorityBuffer(PacketBuffer[] Buffers)
        {
            this.Buffers = Buffers;
            NBuffers = Buffers.Length;
        }

        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. Lower number means higher priority. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= NBuffers)
                throw new ArgumentOutOfRangeException("Priority number is out of range");

            Buffers[Priority].Enqueue(Packet, 0);
        }

        /// <summary>
        /// Get next packet without removing it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            for (int i = 0; i < NBuffers; i++)
            {
                Packet Next = Buffers[i].Peek();

                if (Next != null)
                    return Next;
            }
            return null;
        }

        /// <summary>
        /// Get next packet and remove it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            for (int i = 0; i < NBuffers; i++)
            {
                Packet next = Buffers[i].Dequeue();

                if (next != null)
                    return next;
            }
            return null;
        }

        /// <summary>
        /// Get the total number of packets in the buffer.
        /// </summary>
        public override int Count
        {
            get
            {
                int Sum = 0;
                foreach (PacketBuffer Buffer in Buffers)
                    Sum += Buffer.Count;
                return Sum;
            }
        }
    }

    /// <summary>
    /// This defines a bandwidth control buffer that controlls the bandwidth for each priority.
    /// </summary>
    public class BandwidthControlBuffer : PacketBuffer
    {
        private readonly PacketBuffer[] Buffers; // List of buffers for each priority
        private readonly int[] BandwidthAllocations; // Bandwidth allocation for each priority
        private readonly int NBuffers; // Number of buffers
        private readonly int[] TokenBuckets; // Token bucket for each priority
        private int MaximumToken; // Maximum token bucket size for each priority
        private int CurrentBucket; // Current priority that is being sent
        private Packet PacketCache; // Cache for last peeked packet.

        /// <summary>
        /// Construct a controller.
        /// </summary>
        /// 
        /// <remarks>
        /// If `BandwidthAllocation` is null, it will be set to all 1's.
        /// </remarks>
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
                for (int i = 0; i < BandwidthAllocation.Length; i++)
                    BandwidthAllocation[i] = 1;
            }

            if (Buffers.Length != BandwidthAllocation.Length)
                throw new ArgumentException("Number of bandwith assignment doesn't match number of Buffers");

            if (MaximumToken <= 0)
                throw new ArgumentOutOfRangeException("Maximum token should be greater than zero");

            this.Buffers = Buffers;
            this.BandwidthAllocations = BandwidthAllocation;
            this.MaximumToken = MaximumToken;
            NBuffers = Buffers.Length;
            CurrentBucket = 0;
            PacketCache = null;

            TokenBuckets = new int[NBuffers];
            for (int i = 0; i < NBuffers; i++)
                TokenBuckets[i] = 0;
        }

        /// <summary>
        /// Add `Multiple * BandwidthAllocation[i]` tokens to each token bucket.
        /// </summary>
        /// 
        /// <param name="Multiple"> How many tokens to add. </param>
        private void AddToken(int Multiple)
        {
            for (int i = 0; i < NBuffers; i++)
            {
                TokenBuckets[i] = Math.Min(TokenBuckets[i] + Multiple * BandwidthAllocations[i], MaximumToken);
            }
        }

        /// <summary>
        /// Find next bucket that have both packet and sufficient token.
        /// If no bucket have both packet and sufficient token, find the bucket with packet but insufficient token.
        /// </summary>
        /// 
        /// <remarks>
        /// It changes `CurrentBucket` to next candidate.
        /// </remarks>
        /// 
        /// <returns> true if all buffers are empty. </returns>
        private bool NextBucket()
        {
            // Find the bucket with packet and sufficient token
            int Previous = CurrentBucket;
            do
            {
                CurrentBucket = CurrentBucket + 1;
                CurrentBucket = CurrentBucket >= NBuffers ? 0 : CurrentBucket;
            } while (CurrentBucket != Previous &&
                (TokenBuckets[CurrentBucket] < 0 || Buffers[CurrentBucket].Peek() == null));

            if (CurrentBucket != Previous)
            {
                // Found the packet
                return false;
            }
            else
            {
                // Find the bucket with packet
                for (CurrentBucket = 0; CurrentBucket < NBuffers; CurrentBucket++)
                    if (Buffers[CurrentBucket].Peek() != null)
                        return false;

                // Didn't find anything
                CurrentBucket = 0;
                return true;
            }
        }

        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= NBuffers)
                throw new ArgumentOutOfRangeException("Priority number is out of range");

            Buffers[Priority].Enqueue(Packet, 0);
        }

        /// <summary>
        /// Get next packet without removing it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            if (PacketCache == null)
                PacketCache = Dequeue(); // Store next packet into cache so it can be removed on next call of dequeue

            return PacketCache;
        }

        /// <summary>
        /// Get next packet and remove it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            // Use the packet stored in cache if available
            if (PacketCache != null)
            {
                Packet LastPacket = PacketCache;
                PacketCache = null;
                return LastPacket;
            }

            // Find next packet
            if (TokenBuckets[CurrentBucket] < 0 || Buffers[CurrentBucket].Peek() == null)
                if (NextBucket())
                    return null; // Can't find any packet

            // Update token bucket
            Packet Next = Buffers[CurrentBucket].Dequeue();
            TokenBuckets[CurrentBucket] -= Next.GetLength();

            // Refill token bucket if it is not enough
            if (TokenBuckets[CurrentBucket] < 0)
            {
                AddToken(1 + (-TokenBuckets[CurrentBucket]) / BandwidthAllocations[CurrentBucket]);

                // Switch to the buffer of next prioirity
                CurrentBucket = CurrentBucket + 1;
                CurrentBucket = CurrentBucket >= NBuffers ? 0 : CurrentBucket;
            }
            return Next;
        }

        /// <summary>
        /// Get the total number of packets in the buffer.
        /// </summary>
        public override int Count
        {
            get
            {
                int Sum = 0;
                foreach (PacketBuffer Buffer in Buffers)
                    Sum += Buffer.Count;
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
        public readonly QueueBuffer[] Buffers;
        public readonly PriorityBuffer PriorityBuffer;
        public readonly BandwidthControlBuffer BandwidthBuffer;

        /// <summary>
        /// Constrict a generic Controller.
        /// </summary>
        public GenericController()
        {
            Buffers = new QueueBuffer[5];
            for (int i = 0; i < 5; i++)
                Buffers[i] = new QueueBuffer();

            PacketBuffer[] BandwidthSubcontrollers = { Buffers[1], Buffers[2], Buffers[3] };
            int[] BandwidthAllocation = { 3, 2, 1 };
            BandwidthBuffer = new BandwidthControlBuffer(BandwidthSubcontrollers, BandwidthAllocation);

            PacketBuffer[] PrioritySubcontrollers = { Buffers[0], BandwidthBuffer, Buffers[4] };
            PriorityBuffer = new PriorityBuffer(PrioritySubcontrollers);
        }

        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public override void Enqueue(Packet Packet, int Priority)
        {
            if (Priority < 0 || Priority >= 5)
                throw new ArgumentOutOfRangeException("Priority number is out of range");

            Buffers[Priority].Enqueue(Packet);
        }

        /// <summary>
        /// Add a packet to buffer.
        /// </summary>
        /// 
        /// <param name="Packet"> The packet to add. </param>
        /// <param name="Priority"> Priority of packet. </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException"> If priority is out of range. </exception>
        public void Enqueue(Packet Packet, PacketPriority priority = PacketPriority.MEDIUM)
        {
            if (priority < 0 || (int)priority >= 5)
                throw new ArgumentOutOfRangeException("Priority number is out of range");

            Buffers[(int)priority].Enqueue(Packet);
        }

        /// <summary>
        /// Get next packet without removing it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Peek()
        {
            return PriorityBuffer.Peek();
        }


        /// <summary>
        /// Get next packet and remove it.
        /// </summary>
        /// 
        /// <remarks>
        /// It will return null if the buffer is empty instead of throwing an exception.
        /// </remarks>
        /// 
        /// <returns> Next packet if buffer is not empty, or null otherwise. </returns>
        public override Packet Dequeue()
        {
            return PriorityBuffer.Dequeue();
        }

        /// <summary>
        /// Get the total number of packets in the buffer.
        /// </summary>
        public override int Count
        {
            get
            {
                return PriorityBuffer.Count;
            }
        }
    }
}
