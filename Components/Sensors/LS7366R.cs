using Scarlet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Components.Sensors
{
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        public struct LS7366RConfiguration
        {

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
