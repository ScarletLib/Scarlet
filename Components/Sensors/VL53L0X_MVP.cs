using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary> A minimum viable product implementation of the VL53L0X, based on the simplified library here: https://github.com/pololu/vl53l0x-arduino </summary>
    public class VL53L0X_MVP : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }

        private readonly II2CBus Bus;
        private byte Address;

        private int Timeout;
        private Stopwatch TimeoutCheck;

        public VL53L0X_MVP(II2CBus Bus, byte Address = 0x29, bool Use2V8Mode = false)
        {
            this.Bus = Bus;
            this.Address = Address;

            this.Timeout = 0;

            if (Use2V8Mode)
            {
                byte CurrentReg = this.Bus.ReadRegister(this.Address, (byte)Registers.VHV_CONFIG_PAD_SCL_SDA__EXTSUP_HV, 1)[0];
                this.Bus.WriteRegister(this.Address, (byte)Registers.VHV_CONFIG_PAD_SCL_SDA__EXTSUP_HV, new byte[] { (byte)(CurrentReg | 0x01) });
            }

            this.Bus.WriteRegister(this.Address, 0x88, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x00 });
            byte StopVariable = this.Bus.ReadRegister(this.Address, 0x91, 1)[0];
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x00 });

            byte MSRC_CONFIG_CONTROL = this.Bus.ReadRegister(this.Address, (byte)Registers.MSRC_CONFIG_CONTROL, 1)[0];
            this.Bus.WriteRegister(this.Address, (byte)Registers.MSRC_CONFIG_CONTROL, new byte[] { (byte)(MSRC_CONFIG_CONTROL | 0x12) });

            SetSignalRateLimit((Half)0.25F);

            this.Bus.WriteRegister(this.Address, (byte)Registers.SYSTEM_SEQUENCE_CONFIG, new byte[] { 0xFF });

            GetSPADInfo(out byte SPADCount, out bool SPADTypeIsAperture);

            byte[] SPADMap = this.Bus.ReadRegister(this.Address, (byte)Registers.GLOBAL_CONFIG_SPAD_ENABLES_REF_0, 6);

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, (byte)Registers.DYNAMIC_SPAD_REF_EN_START_OFFSET, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, (byte)Registers.DYNAMIC_SPAD_NUM_REQUESTED_REF_SPAD, new byte[] { 0x2C });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, (byte)Registers.GLOBAL_CONFIG_REF_EN_START_SELECT, new byte[] { 0xB4 });

            byte FirstSPADToEnable = (byte)(SPADTypeIsAperture ? 12 : 0);
            byte SPADsEnabled = 0;

            for (byte i = 0; i < 48; i++)
            {
                if (i < FirstSPADToEnable || SPADsEnabled == SPADCount)
                {
                    SPADMap[i / 8] = (byte)(SPADMap[i / 8] & ~(1 << (i % 8)));
                }
                else if (((SPADMap[i / 8] >> (i % 8)) & 0x1) == 0b1)
                {
                    SPADsEnabled++;
                }
            }

            this.Bus.WriteRegister(this.Address, (byte)Registers.GLOBAL_CONFIG_SPAD_ENABLES_REF_0, SPADMap);

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x09, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x10, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x11, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0x24, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x25, new byte[] { 0xFF });
            this.Bus.WriteRegister(this.Address, 0x75, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x4E, new byte[] { 0x2C });
            this.Bus.WriteRegister(this.Address, 0x48, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x30, new byte[] { 0x20 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x30, new byte[] { 0x09 });
            this.Bus.WriteRegister(this.Address, 0x54, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x31, new byte[] { 0x04 });
            this.Bus.WriteRegister(this.Address, 0x32, new byte[] { 0x03 });
            this.Bus.WriteRegister(this.Address, 0x40, new byte[] { 0x83 });
            this.Bus.WriteRegister(this.Address, 0x46, new byte[] { 0x25 });
            this.Bus.WriteRegister(this.Address, 0x60, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x27, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x50, new byte[] { 0x06 });
            this.Bus.WriteRegister(this.Address, 0x51, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x52, new byte[] { 0x96 });
            this.Bus.WriteRegister(this.Address, 0x56, new byte[] { 0x08 });
            this.Bus.WriteRegister(this.Address, 0x57, new byte[] { 0x30 });
            this.Bus.WriteRegister(this.Address, 0x61, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x62, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x64, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x65, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x66, new byte[] { 0xA0 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x22, new byte[] { 0x32 });
            this.Bus.WriteRegister(this.Address, 0x47, new byte[] { 0x14 });
            this.Bus.WriteRegister(this.Address, 0x49, new byte[] { 0xFF });
            this.Bus.WriteRegister(this.Address, 0x4A, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x7A, new byte[] { 0x0A });
            this.Bus.WriteRegister(this.Address, 0x7B, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x78, new byte[] { 0x21 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x23, new byte[] { 0x34 });
            this.Bus.WriteRegister(this.Address, 0x42, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x44, new byte[] { 0xFF });
            this.Bus.WriteRegister(this.Address, 0x45, new byte[] { 0x26 });
            this.Bus.WriteRegister(this.Address, 0x46, new byte[] { 0x05 });
            this.Bus.WriteRegister(this.Address, 0x40, new byte[] { 0x40 });
            this.Bus.WriteRegister(this.Address, 0x0E, new byte[] { 0x06 });
            this.Bus.WriteRegister(this.Address, 0x20, new byte[] { 0x1A });
            this.Bus.WriteRegister(this.Address, 0x43, new byte[] { 0x40 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x34, new byte[] { 0x03 });
            this.Bus.WriteRegister(this.Address, 0x35, new byte[] { 0x44 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x31, new byte[] { 0x04 });
            this.Bus.WriteRegister(this.Address, 0x4B, new byte[] { 0x09 });
            this.Bus.WriteRegister(this.Address, 0x4C, new byte[] { 0x05 });
            this.Bus.WriteRegister(this.Address, 0x4D, new byte[] { 0x04 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x44, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x45, new byte[] { 0x20 });
            this.Bus.WriteRegister(this.Address, 0x47, new byte[] { 0x08 });
            this.Bus.WriteRegister(this.Address, 0x48, new byte[] { 0x28 });
            this.Bus.WriteRegister(this.Address, 0x67, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x70, new byte[] { 0x04 });
            this.Bus.WriteRegister(this.Address, 0x71, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x72, new byte[] { 0xFE });
            this.Bus.WriteRegister(this.Address, 0x76, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x77, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x0D, new byte[] { 0x01 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x01, new byte[] { 0xF8 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x8E, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, (byte)Registers.SYSTEM_INTERRUPT_CONFIG_GPIO, new byte[] { 0x04 });
            byte Read = this.Bus.ReadRegister(this.Address, (byte)Registers.GPIO_HV_MUX_ACTIVE_HIGH, 1)[0];
            this.Bus.WriteRegister(this.Address, (byte)Registers.GPIO_HV_MUX_ACTIVE_HIGH, new byte[] { (byte)(Read & ~0x10) });
            this.Bus.WriteRegister(this.Address, (byte)Registers.SYSTEM_INTERRUPT_CLEAR, new byte[] { 0x01 });

            uint MeasurementTimingBudget_us = GetMeasurementTimingBudget();
            this.Bus.WriteRegister(this.Address, (byte)Registers.SYSTEM_SEQUENCE_CONFIG, new byte[] { 0xE8 });

            // LINE 253
        }

        public DataUnit GetData()
        {
            return new DataUnit("VL53L0X")
            {

            }.SetSystem(this.System);
        }

        public bool Test() => true;

        public void UpdateState()
        {
            // do things?
        }

        private void SetSignalRateLimit(Half LimitMCPS)
        {
            if (LimitMCPS < 0 || LimitMCPS > 511.99) { throw new ArgumentOutOfRangeException("MCPS Limit must be within 0 to 512."); }
            LimitMCPS *= (1 << 7);
            byte[] Data = Half.GetBytes(LimitMCPS);
            this.Bus.WriteRegister(this.Address, (byte)Registers.FINAL_RANGE_CONFIG_MIN_COUNT_RATE_RTN_LIMIT, Data);
        }

        private void GetSPADInfo(out byte SPADCount, out bool SPADTypeIsAperture)
        {
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x00 });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x06 });
            byte Read = this.Bus.ReadRegister(this.Address, 0x83, 1)[0];
            this.Bus.WriteRegister(this.Address, 0x83, new byte[] { (byte)(Read | 0x04) });

            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x07 });
            this.Bus.WriteRegister(this.Address, 0x81, new byte[] { 0x01 });

            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x01 });

            this.Bus.WriteRegister(this.Address, 0x94, new byte[] { 0x6b });
            this.Bus.WriteRegister(this.Address, 0x83, new byte[] { 0x00 });

            StartTimeout();

            while (this.Bus.ReadRegister(this.Address, 0x83, 1)[0] == 0x00)
            {
                if (CheckTimeout()) { throw new TimeoutException("VL53L0X took too long during SPAD info retrieval."); }
                Thread.Sleep(10);
            }

            StopTimeout();

            this.Bus.WriteRegister(this.Address, 0x83, new byte[] { 0x01 });
            byte TempVal = this.Bus.ReadRegister(this.Address, 0x92, 1)[0];

            SPADCount = (byte)(TempVal & 0x7F);
            SPADTypeIsAperture = ((TempVal >> 7) & 0x01) == 0b1;

            this.Bus.WriteRegister(this.Address, 0x81, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x06 });

            Read = this.Bus.ReadRegister(this.Address, 0x83, 1)[0];
            this.Bus.WriteRegister(this.Address, 0x83, new byte[] { (byte)(Read & ~0x04) });
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x01 });
            this.Bus.WriteRegister(this.Address, 0x00, new byte[] { 0x01 });
            
            this.Bus.WriteRegister(this.Address, 0xFF, new byte[] { 0x00 });
            this.Bus.WriteRegister(this.Address, 0x80, new byte[] { 0x00 });
        }

        private uint GetMeasurementTimingBudget()
        {
            SequenceStepEnables Enables;
            SequenceStepTimeouts Timeouts;

            const ushort StartOverhead = 1910;
            const ushort MsrcOverhead = 660;
            const ushort EndOverhead = 960;
            const ushort TccOverhead = 590;
            const ushort DssOverhead = 690;
            const ushort PreRangeOverhead = 660;
            const ushort FinalRangeOverhead = 550;

            uint Budget_us = StartOverhead + EndOverhead;

            Enables = GetSequenceStepEnables();
            Timeouts = GetSequenceStepTimeouts(Enables);

            if (Enables.TCC) { Budget_us += (Timeouts.MSRC_DSS_TCC_us + TccOverhead); }
            if (Enables.DSS) { Budget_us += (2 * (Timeouts.MSRC_DSS_TCC_us + DssOverhead)); }
            else if (Enables.MSRC) { Budget_us += (Timeouts.MSRC_DSS_TCC_us + MsrcOverhead); }
            if (Enables.PreRange) { Budget_us += (Timeouts.PreRange_us + PreRangeOverhead); }
            if (Enables.FinalRange) { Budget_us += (Timeouts.FinalRange_us + FinalRangeOverhead); }

            return Budget_us;
        }

        private SequenceStepEnables GetSequenceStepEnables()
        {
            byte SequenceConfig = this.Bus.ReadRegister(this.Address, (byte)Registers.SYSTEM_SEQUENCE_CONFIG, 1)[0];
            return new SequenceStepEnables()
            {
                TCC = ((SequenceConfig >> 4) & 0x1) == 0b1,
                DSS = ((SequenceConfig >> 3) & 0x1) == 0b1,
                MSRC = ((SequenceConfig >> 2) & 0x1) == 0b1,
                PreRange = ((SequenceConfig >> 6) & 0x1) == 0b1,
                FinalRange = ((SequenceConfig >> 7) & 0x1) == 0b1
            };
        }

        private SequenceStepTimeouts GetSequenceStepTimeouts(SequenceStepEnables Enables)
        {
            SequenceStepTimeouts Timeouts = new SequenceStepTimeouts();

            Timeouts.PreRangeVCSelPeriodPClks = GetVCSelPulsePeriod(VCSelPeriodType.VCSelPeriodPreRange);

            Timeouts.MSRC_DSS_TCC_MClks = (byte)(this.Bus.ReadRegister(this.Address, (byte)Registers.MSRC_CONFIG_TIMEOUT_MACROP, 1)[0] + 1);
            Timeouts.MSRC_DSS_TCC_us = TimeoutMClksToMicroseconds(Timeouts.MSRC_DSS_TCC_MClks, (byte)Timeouts.PreRangeVCSelPeriodPClks);

            Timeouts.PreRangeMClks = DecodeTimeout(this.Bus.ReadRegister(this.Address, (byte)Registers.PRE_RANGE_CONFIG_TIMEOUT_MACROP_HI, 2));
            Timeouts.PreRange_us = TimeoutMClksToMicroseconds(Timeouts.PreRangeMClks, (byte)Timeouts.PreRangeVCSelPeriodPClks);

            Timeouts.FinalRangeVCSelPeriodPClks = GetVCSelPulsePeriod(VCSelPeriodType.VCSelPeriodFinalRange);

            Timeouts.FinalRangeMClks = DecodeTimeout(this.Bus.ReadRegister(this.Address, (byte)Registers.FINAL_RANGE_CONFIG_TIMEOUT_MACROP_HI, 2));

            if (Enables.PreRange) { Timeouts.FinalRangeMClks -= Timeouts.PreRangeMClks; }

            Timeouts.FinalRange_us = TimeoutMClksToMicroseconds(Timeouts.FinalRangeMClks, (byte)Timeouts.FinalRangeVCSelPeriodPClks);

            return Timeouts;
        }

        private byte GetVCSelPulsePeriod(VCSelPeriodType Type)
        {
            if (Type == VCSelPeriodType.VCSelPeriodPreRange) { return (byte)(((this.Bus.ReadRegister(this.Address, (byte)Registers.PRE_RANGE_CONFIG_VCSEL_PERIOD, 1)[0]) + 1) << 1); }
            else if (Type == VCSelPeriodType.VCSelPeriodFinalRange) { return (byte)(((this.Bus.ReadRegister(this.Address, (byte)Registers.FINAL_RANGE_CONFIG_VCSEL_PERIOD, 1)[0]) + 1) << 1); }
            else { return 255; }
        }

        private uint TimeoutMClksToMicroseconds(ushort TimeoutPeriodMClks, byte VCSelPeriodPClks)
        {
            uint MacroPeriod_ns = CalcMacroPeriod(VCSelPeriodPClks);
            return ((TimeoutPeriodMClks * MacroPeriod_ns) + (MacroPeriod_ns / 2)) / 1000;
        }

        private uint CalcMacroPeriod(byte VCSelPeriodPClks) => ((((uint)2304 * VCSelPeriodPClks * 1655) + 500) / 1000);

        private ushort DecodeTimeout(byte[] RegVals)
        {
            return (ushort)((RegVals[0] << RegVals[1]) + 1);
        }

        private void StartTimeout() => this.TimeoutCheck.Restart();

        private void StopTimeout() => this.TimeoutCheck.Stop();

        private bool CheckTimeout() => (this.Timeout > 0) && (this.TimeoutCheck.ElapsedMilliseconds > this.Timeout);

        private enum Registers : byte
        {
            SYSRANGE_START = 0x00,

            SYSTEM_THRESH_HIGH = 0x0C,
            SYSTEM_THRESH_LOW = 0x0E,

            SYSTEM_SEQUENCE_CONFIG = 0x01,
            SYSTEM_RANGE_CONFIG = 0x09,
            SYSTEM_INTERMEASUREMENT_PERIOD = 0x04,

            SYSTEM_INTERRUPT_CONFIG_GPIO = 0x0A,

            GPIO_HV_MUX_ACTIVE_HIGH = 0x84,

            SYSTEM_INTERRUPT_CLEAR = 0x0B,

            RESULT_INTERRUPT_STATUS = 0x13,
            RESULT_RANGE_STATUS = 0x14,

            RESULT_CORE_AMBIENT_WINDOW_EVENTS_RTN = 0xBC,
            RESULT_CORE_RANGING_TOTAL_EVENTS_RTN = 0xC0,
            RESULT_CORE_AMBIENT_WINDOW_EVENTS_REF = 0xD0,
            RESULT_CORE_RANGING_TOTAL_EVENTS_REF = 0xD4,
            RESULT_PEAK_SIGNAL_RATE_REF = 0xB6,

            ALGO_PART_TO_PART_RANGE_OFFSET_MM = 0x28,

            I2C_SLAVE_DEVICE_ADDRESS = 0x8A,

            MSRC_CONFIG_CONTROL = 0x60,

            PRE_RANGE_CONFIG_MIN_SNR = 0x27,
            PRE_RANGE_CONFIG_VALID_PHASE_LOW = 0x56,
            PRE_RANGE_CONFIG_VALID_PHASE_HIGH = 0x57,
            PRE_RANGE_MIN_COUNT_RATE_RTN_LIMIT = 0x64,

            FINAL_RANGE_CONFIG_MIN_SNR = 0x67,
            FINAL_RANGE_CONFIG_VALID_PHASE_LOW = 0x47,
            FINAL_RANGE_CONFIG_VALID_PHASE_HIGH = 0x48,
            FINAL_RANGE_CONFIG_MIN_COUNT_RATE_RTN_LIMIT = 0x44,

            PRE_RANGE_CONFIG_SIGMA_THRESH_HI = 0x61,
            PRE_RANGE_CONFIG_SIGMA_THRESH_LO = 0x62,

            PRE_RANGE_CONFIG_VCSEL_PERIOD = 0x50,
            PRE_RANGE_CONFIG_TIMEOUT_MACROP_HI = 0x51,
            PRE_RANGE_CONFIG_TIMEOUT_MACROP_LO = 0x52,

            SYSTEM_HISTOGRAM_BIN = 0x81,
            HISTOGRAM_CONFIG_INITIAL_PHASE_SELECT = 0x33,
            HISTOGRAM_CONFIG_READOUT_CTRL = 0x55,

            FINAL_RANGE_CONFIG_VCSEL_PERIOD = 0x70,
            FINAL_RANGE_CONFIG_TIMEOUT_MACROP_HI = 0x71,
            FINAL_RANGE_CONFIG_TIMEOUT_MACROP_LO = 0x72,
            CROSSTALK_COMPENSATION_PEAK_RATE_MCPS = 0x20,

            MSRC_CONFIG_TIMEOUT_MACROP = 0x46,

            SOFT_RESET_GO2_SOFT_RESET_N = 0xBF,
            IDENTIFICATION_MODEL_ID = 0xC0,
            IDENTIFICATION_REVISION_ID = 0xC2,

            OSC_CALIBRATE_VAL = 0xF8,

            GLOBAL_CONFIG_VCSEL_WIDTH = 0x32,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_0 = 0xB0,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_1 = 0xB1,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_2 = 0xB2,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_3 = 0xB3,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_4 = 0xB4,
            GLOBAL_CONFIG_SPAD_ENABLES_REF_5 = 0xB5,

            GLOBAL_CONFIG_REF_EN_START_SELECT = 0xB6,
            DYNAMIC_SPAD_NUM_REQUESTED_REF_SPAD = 0x4E,
            DYNAMIC_SPAD_REF_EN_START_OFFSET = 0x4F,
            POWER_MANAGEMENT_GO1_POWER_FORCE = 0x80,

            VHV_CONFIG_PAD_SCL_SDA__EXTSUP_HV = 0x89,

            ALGO_PHASECAL_LIM = 0x30,
            ALGO_PHASECAL_CONFIG_TIMEOUT = 0x30,
        };

        private enum VCSelPeriodType { VCSelPeriodPreRange, VCSelPeriodFinalRange }

        private struct SequenceStepEnables
        {
            public bool TCC, MSRC, DSS, PreRange, FinalRange;
        }

        private struct SequenceStepTimeouts
        {
            public ushort PreRangeVCSelPeriodPClks, FinalRangeVCSelPeriodPClks;

            public ushort MSRC_DSS_TCC_MClks, PreRangeMClks, FinalRangeMClks;
            public uint MSRC_DSS_TCC_us, PreRange_us, FinalRange_us;
        };
    }
}
