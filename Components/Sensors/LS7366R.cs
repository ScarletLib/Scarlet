using Scarlet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Implements the LS7366R encoder
    /// reader. 
    /// Datasheet here:
    /// https://cdn.usdigital.com/assets/general/LS7366R.pdf
    /// </summary>
    public class LS7366R : ISensor
    {
        private const float LOAD_READ_DELAY = 0.02f; // seconds

        private IDigitalOut ChipSelect;
        private IDigitalOut CountEnable;
        private ISPIBus SPIBus;
        private volatile bool HadEvent;

        public bool CountEnabled { get; private set; }
        public int Count { get; private set; }
        public event EventHandler<OverflowEvent> OverflowOccured;


        /// <summary>
        /// Initializes the LS7366R SPI
        /// encoder counter chip.
        /// </summary>
        /// <param name="SPIBus">SPI Bus to communicate with</param>
        /// <param name="ChipSelect">Chip Select to use with SPI</param>
        /// <param name="CountEnable">Digital Out to enable the counters</param>
        public LS7366R(ISPIBus SPIBus, IDigitalOut ChipSelect, IDigitalOut CountEnable = null)
        {
            this.ChipSelect = ChipSelect;
            this.CountEnable = CountEnable;
            this.SPIBus = SPIBus;
        }

        /// <summary>
        /// Configures the device given a configuration
        /// </summary>
        /// <param name="Configuration">Configuration to use.</param>
        public void Configure(Configuration Configuration)
        {

        }

        /// <summary>
        /// Configures device with a default configuration
        /// </summary>
        public void Configure()
        {

        }

        /// <summary>
        /// Sets the output of the 
        /// given count enable output
        /// (if given) to the Enable
        /// state. Sets CountEnabled
        /// to the given Enable state.
        /// </summary>
        /// <param name="Enable">Whether or not to enable the chip's CNT_EN pin</param>
        public void EnableCount(bool Enable)
        {
            CountEnable?.SetOutput(Enable);
            CountEnabled = Enable;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Event"></param>
        protected virtual void OnOverflow(OverflowEvent Event) { OverflowOccured?.Invoke(this, Event); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Event"></param>
        public void EventTriggered(object Sender, EventArgs Event)
        {
            if(Event is OverflowEvent) { HadEvent = true; }
        }

        /// <summary>
        /// Tests the LS7366R device.
        /// </summary>
        /// <returns>Whether or not the test passed</returns>
        public bool Test()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads the buffer data into the chips
        /// read buffer in parallel, then read
        /// from the read buffer and update Count.
        /// </summary>
        public void UpdateState()
        {
            if (this.HadEvent) { this.HadEvent = false; }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Structure for the configuration of
        /// the device.
        /// </summary>
        public struct Configuration
        {
            public QuadMode CountMode;
            public CountMode FreeRunCountMode;
            public IndexConfig IndexConfig;
            public CounterMode CounterMode;

            public bool DivideClkBy2;
            public bool SynchronousIndex;

            public bool FlagOnIDX;
            public bool FlagOnCMP;
            public bool FlagOnBW;
            public bool FlagOnCY;
        }

        /// <summary>
        /// 
        /// </summary>
        public enum QuadMode
        {
            NON_QUAD,
            X1_QUAD,
            X2_QUAD,
            X4_QUAD
        }

        /// <summary>
        /// 
        /// </summary>
        public enum CountMode
        {
            FREE_RUNNING,
            SINGLE_CYCLE,
            RANGE_LIMIT,
            MOD_N
        }

        /// <summary>
        /// 
        /// </summary>
        public enum IndexConfig
        {
            DISABLE,
            LOAD_CTR,
            RESET_CTR,
            LOAD_OTR
        }

        /// <summary>
        /// 
        /// </summary>
        public enum CounterMode
        {
            BYTE_4,
            BYTE_3,
            BYTE_2,
            BYTE_1
        }

    }

    /// <summary>
    /// Event to store overflow events.
    /// Includes overflow and underflow
    /// fields.
    /// </summary>
    public class OverflowEvent : EventArgs
    {
        public bool Overflow;
        public bool Underflow;
    }

}
