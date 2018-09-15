using System;
using System.IO;
using BBBCSIO;
using Scarlet.Utilities;

namespace Scarlet.IO.BeagleBone
{
    /// <summary> Keeps track of all PWM functions on the BBB. </summary>
    /// <remarks>
    /// Hierarchy is as follows:
    /// PWMBBB -> Device -> Output -> Physical Pin
    /// PWMBBB
    /// |- Device0
    /// |  |- OutputA
    /// |  |  |- Physical Pin
    /// |  |  |- Physical Pin
    /// |  |- OutputB
    /// |     |- Physical Pin
    /// |     |- Physical Pin
    /// |- Device1
    /// |  |- OutputA
    /// |  |  |- Physical Pin
    /// |  |  |- Physical Pin
    /// |  |- OutputB
    /// |     |- Physical Pin
    /// |     |- Physical Pin
    /// |- Device2
    ///    |- OutputA
    ///    |  |- Physical Pin
    ///    |  |- Physical Pin
    ///    |- OutputB
    ///       |- Physical Pin
    ///       |- Physical Pin
    /// </remarks>
    public static class PWMBBB
    {
        public static PWMDeviceBBB PWMDevice0 { get; private set; }
        public static PWMDeviceBBB PWMDevice1 { get; private set; }
        public static PWMDeviceBBB PWMDevice2 { get; private set; }

        /// <summary> Gets the PWM output corresponding to a given pin on the BeagleBone Black. </summary>
        /// <param name="Pin"> The pin to find a PWM output associated with. </param>
        /// <returns> A <see cref="PWMOutputBBB"/>, or <c>null</c> if the given pin is not valid. </returns>
        public static PWMOutputBBB GetFromPin(BBBPin Pin)
        {
            switch (Pin)
            {
                case BBBPin.P9_22:
                case BBBPin.P9_31: return PWMDevice0.OutputA;

                case BBBPin.P9_21:
                case BBBPin.P9_29: return PWMDevice0.OutputB;

                case BBBPin.P9_14:
                case BBBPin.P8_36: return PWMDevice1.OutputA;

                case BBBPin.P9_16:
                case BBBPin.P8_34: return PWMDevice1.OutputB;

                case BBBPin.P8_19:
                case BBBPin.P8_45: return PWMDevice2.OutputA;

                case BBBPin.P8_13:
                case BBBPin.P8_46: return PWMDevice2.OutputB;
            }
            return null;
        }

        /// <summary> Prepares the given PWM ports for use. Should only be called from <see cref="BeagleBone.Initialize(SystemMode, bool)"/>. </summary>
        /// <param name="EnableBuses"> Whether to enable each of the PWM devices. </param>
        internal static void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 3) { throw new Exception("Invalid enable array given to PWMBBB.Initialize."); }

            ////                                                                    A1            A2                             B1            B2
            if (EnableBuses[0]) { PWMDevice0 = new PWMDeviceBBB(new BBBPin[] { BBBPin.P9_22, BBBPin.P9_31 }, new BBBPin[] { BBBPin.P9_21, BBBPin.P9_29 }); }
            if (EnableBuses[1]) { PWMDevice1 = new PWMDeviceBBB(new BBBPin[] { BBBPin.P9_14, BBBPin.P8_36 }, new BBBPin[] { BBBPin.P9_16, BBBPin.P8_34 }); }
            if (EnableBuses[2]) { PWMDevice2 = new PWMDeviceBBB(new BBBPin[] { BBBPin.P8_19, BBBPin.P8_45 }, new BBBPin[] { BBBPin.P8_13, BBBPin.P8_46 }); }
        }

        /// <summary> Converts a pin number to the corresponding PWM device and output number. Needed as every output is connected to 2 physical pins. </summary>
        /// <param name="Pin"> The pin to find the PWM ID of. </param>
        /// <returns> The PWM ID corresponding to the pin, or <see cref="PWMPortEnum.PWM_NONE"/> if the given pin is invalid. </returns>
        internal static PWMPortEnum PinToPWMID(BBBPin Pin)
        {
            switch (Pin)
            {
                case BBBPin.P9_22:
                case BBBPin.P9_31: return PWMPortEnum.PWM0_A;

                case BBBPin.P9_21:
                case BBBPin.P9_29: return PWMPortEnum.PWM0_B;

                case BBBPin.P9_14:
                case BBBPin.P8_36: return PWMPortEnum.PWM1_A;

                case BBBPin.P9_16:
                case BBBPin.P8_34: return PWMPortEnum.PWM1_B;

                case BBBPin.P8_19:
                case BBBPin.P8_45: return PWMPortEnum.PWM2_A;

                case BBBPin.P8_13:
                case BBBPin.P8_46: return PWMPortEnum.PWM2_B;
            }
            return PWMPortEnum.PWM_NONE;
        }
    }

    public class PWMDeviceBBB
    {
        public PWMOutputBBB OutputA { get; private set; }
        public PWMOutputBBB OutputB { get; private set; }

        /// <summary> This should only be initialized from <see cref="PWMBBB"/>. </summary>
        /// <param name="PinsA"> The pins in channel A. </param>
        /// <param name="PinsB"> The pins in channel B. </param>
        internal PWMDeviceBBB(BBBPin[] PinsA, BBBPin[] PinsB)
        {
            this.OutputA = new PWMOutputBBB(PinsA, this);
            this.OutputB = new PWMOutputBBB(PinsB, this);
        }

        /// <summary> Sets the clock frequency of this PWM device. Note that this gets applied to both outputs. </summary>
        /// <param name="Frequency"> The new frequency, in Hz. </param>
        /// <exception cref="InvalidOperationException"> If you attempt to change the frequency before initialization. </exception>
        public void SetFrequency(float Frequency)
        {
            if (this.OutputA.Port != null) { this.OutputA.Port.FrequencyHz = (uint)Math.Round(Frequency); }
            else if (this.OutputB.Port != null) { this.OutputB.Port.FrequencyHz = (uint)Math.Round(Frequency); }
            else { throw new InvalidOperationException("Cannot change frequency of device before initialization."); }

            if (this.OutputA.Port != null) { this.OutputA.ResetOutput(); }
            if (this.OutputB.Port != null) { this.OutputB.ResetOutput(); }
        }
    }

    public class PWMOutputBBB : IPWMOutput
    {
        internal PWMPortMM Port { get; private set; }

        private BBBPin[] Pins;
        private PWMDeviceBBB Parent;
        private float DutyCycle = -1;

        /// <summary> This should only be initialized from <see cref="PWMDeviceBBB"/>. </summary>
        /// <param name="Pins"> The two pins that correspond to this output channel. </param>
        /// <param name="Parent"> The PWM device that this output belongs to. </param>
        internal PWMOutputBBB(BBBPin[] Pins, PWMDeviceBBB Parent)
        {
            this.Pins = Pins;
            this.Parent = Parent;
            Initialize();
        }

        public void Dispose() { }

        /// <summary> Sets the device's (both output A and B) clock frequency to the given one, in Hz. </summary>
        /// <param name="Frequency"> The frequency to set the device to. Rounded to nearest Hz. </param>
        public void SetFrequency(float Frequency) { this.Parent.SetFrequency(Frequency); }

        /// <summary> Sets the output to the given duty cycle. Must be between 0.0 and 1.0. </summary>
        /// <param name="DutyCycle"> The duty cycle to set the output to. </param>
        public void SetOutput(float DutyCycle)
        {
            this.Port.DutyPercent = DutyCycle * 100F;
            this.DutyCycle = DutyCycle * 100F;
        }

        /// <summary> <c>PWMOutputBBB</c> does not support clock delay. This will do nothing. </summary>
        /// <param name="ClockDelay"> Does nothing. </param>
        public void SetDelay(float ClockDelay) { }

        /// <summary> Sets the output state. </summary>
        /// <param name="Enabled"> Whether PWM output is enabled. </param>
        public void SetEnabled(bool Enabled) { this.Port.RunState = Enabled; }

        /// <summary> Gets the current duty cycle, from 0.0 to 1.0. </summary>
        public float GetOutput() { return this.Port.DutyPercent / 100.000F; }

        /// <summary> Gets the current output frequency, in Hz. </summary>
        public uint GetFrequency() { return Port.FrequencyHz; }

        /// <summary> Re-sets the duty cycle. This must be done after changing the clock frequency, as it will have incorrect output afterwards. </summary>
        internal void ResetOutput()
        {
            if (this.DutyCycle != -1) { this.Port.DutyPercent = this.DutyCycle; }
        }

        /// <summary> Prepares the PWM output for use. </summary>
        private void Initialize()
        {
            PWMPortEnum Device = PWMBBB.PinToPWMID(this.Pins[0]);

            // Final path will be: /sys/devices/platform/ocp/4830_000.epwmss/4830_200.pwm/pwm/pwmchip_/pwm_
            //                                                   x               x                   y    z
            // Where x refers to device #. dev 0 = 0, dev 1 = 2, dev 2 = 4.
            //       y is an arbitrary number.
            //          To me, it looks like this is assigned from 0 in steps of +2, in the order that the PWM devices were loaded into the device tree.
            //          Operationally, it seems to have no significance, and there only ever seems to be one in each *.epwmss/*.pwm/ folder.
            //          This is probably because at that point it is already narrowed to a single device (2 pins).
            //       z refers to output #. out A = 0, out B = 2.
            // We need to write the pin number (z) into the "export" file, and a 1 into the "enable" file to prepare the pin for use.
            // At that point, whatever needed to be set in memory is ready for BBBCSIO's PWMPortMM to take over and work.
            // Why does Linux have to be so damn difficult with everything it does? :(
            string Path = "/sys/devices/platform/ocp";

            // Append the memory addresses.
            byte ExportNum = 0;
            switch (Device)
            {
                case PWMPortEnum.PWM0_A:
                    Path += "/48300000.epwmss/48300200.pwm/pwm/";
                    ExportNum = 0;
                    break;
                case PWMPortEnum.PWM0_B:
                    Path += "/48300000.epwmss/48300200.pwm/pwm/";
                    ExportNum = 1;
                    break;
                case PWMPortEnum.PWM1_A:
                    Path += "/48302000.epwmss/48302200.pwm/pwm/";
                    ExportNum = 0;
                    break;
                case PWMPortEnum.PWM1_B:
                    Path += "/48302000.epwmss/48302200.pwm/pwm/";
                    ExportNum = 1;
                    break;
                case PWMPortEnum.PWM2_A:
                    Path += "/48304000.epwmss/48304200.pwm/pwm/";
                    ExportNum = 0;
                    break;
                case PWMPortEnum.PWM2_B:
                    Path += "/48304000.epwmss/48304200.pwm/pwm/";
                    ExportNum = 1;
                    break;
                default: throw new Exception("Invalid PWM pin given.");
            }

            // Append the (arbitrary) pwmchip #, by using the first one we find.
            string[] PWMChips = Directory.GetDirectories(Path);
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "Attempting to find correct pwmchip number...");
            bool FoundPWMChip = false;
            foreach (string SubdirPath in PWMChips)
            {
                string Subdir = new DirectoryInfo(SubdirPath).Name;
                if (Subdir.StartsWith("pwmchip"))
                {
                    Path += Subdir;
                    FoundPWMChip = true;
                    Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "Found \"" + Subdir + "\", using.");
                }
                else { Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "Found \"" + Subdir + "\", no good."); }
            }
            if (!FoundPWMChip) { throw new Exception("Could not find PWM chip number. Is your device tree set correctly?"); }

            // Export and enable the pin.
            if (!Directory.Exists(Path + "/pwm" + ExportNum))
            {
                StreamWriter Exporter = File.AppendText(Path + "/export");
                Exporter.Write(ExportNum);
                Exporter.Flush();
                Exporter.Close();
            }
            Path += ("/pwm" + ExportNum);
            StreamWriter Enabler = File.AppendText(Path + "/enable");
            Enabler.Write("1");
            Enabler.Flush();
            Enabler.Close();
            this.Port = new PWMPortMM(Device);
        }
    }
}
