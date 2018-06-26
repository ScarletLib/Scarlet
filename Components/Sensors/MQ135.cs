using System;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Simple sensor to detect approximate concentration of air pollutants.
    /// Datasheet here: https://www.olimex.com/Products/Components/Sensors/SNS-MQ135/resources/SNS-MQ135.pdf
    /// Math based on: http://davidegironi.blogspot.com/2017/05/mq-gas-sensor-correlation-function.html#.Wo84_UxFx9A
    /// </summary>
    public class MQ135 : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }
        private readonly IAnalogueIn Input;
        private readonly double RS, RL;
        private double SupplyVoltage;
        private int ResistanceCal = 41763;
        private float LastReadingCal, LastReadingUncal, Temperature, Humidity;

        /* Typical circuit setup:
         * 
         *   V+    MQ135      RS
         *   ^-----/\/\/\---/\/\/\---o----> Analogue Input
         *                           |
         *               RL          |
         *   v---------/\/\/\--------/
         *  GND
         *  
         *  Where the MQ135 resistance changes based on pollutant concentration in the air.
         */

        public MQ135(IAnalogueIn Input, double RS, double RL, double SupplyVoltage = 5)
        {
            this.Input = Input;
            this.RS = RS;
            this.RL = RL;
            this.SupplyVoltage = SupplyVoltage;
            this.Humidity = 0.65F;
            this.Temperature = 20;
        }

        public DataUnit GetData()
        {
            return new DataUnit("MQ135")
            {
                { "PPMReadingCal", this.LastReadingCal }
            }
            .SetSystem(this.System);
        }

        /// <summary> Due to this being an analogue sensor, this simply tries to get a reading and sees if it is within range. </summary>
        public bool Test()
        {
            float Output;
            try { Output = GetReadingUncalibrated(); }
            catch (Exception) { return false; }
            return Output != -1;
        }

        public void UpdateState() => CalculatePPM();

        /// <summary> Gets the most recent sensor reading. </summary>
        /// <remarks> Currently, temperature/humidity calibration settings are not applied. </remarks>
        /// <returns> The estimated total ppm of pollutants in the air. </returns>
        public float GetReading() => this.LastReadingCal;

        /// <summary> Gets the most recent sensor reading, without applying temperature or humidity compensation. </summary>
        /// <returns> The estimated total ppm of pollutants in the air. </returns>
        public float GetReadingUncalibrated() => this.LastReadingUncal;

        /// <summary> Applies some basic calibration to the sensor to compensate for air temperature. </summary>
        /// <remarks> Defaults to 20 C. </remarks>
        /// <param name="Temperature"> The current air temperature in degrees C, between -10 and 45. </param>
        [Obsolete("This does not yet apply any calibration.")]
        public void CalibrateTemperature(float Temperature)
        {
            if (Temperature < -10 || Temperature > 45) { throw new Exception("MQ135 only works in -10 to 45 C temperatures."); }
            this.Temperature = Temperature;
        }

        /// <summary> Applies some basic calibration to the sensor to compensate for environmental humidity. </summary>
        /// <remarks> Defaults to 65% RH. </remarks>
        /// <param name="Humidity"> % Relative Humidity value, between 0.0 and 0.95. </param>
        [Obsolete("This does not yet apply any calibration.")]
        public void CalibrateHumidity(float Humidity)
        {
            if (Humidity < 0 || Humidity > 0.95) { throw new Exception("MQ135 only works in 0 to 95% RH environment."); }
            this.Humidity = Humidity;
        }

        /// <summary> Applies calibration for base sensor resistance. This is probably constant for each individual sensor. Gets used in both calibrated and uncalibrated readings. </summary>
        /// <remarks> You'll need to put the sensor into a 100 ppm CO2 environment at 20C and 65% RH to get this value. 41763 is used as default. </remarks>
        /// <param name="Resistance"> The measured resistance of the sensor, in the specific calibration conditions as listed above. Should be near 42K. </param>
        public void CalibrateResistance(int Resistance) { this.ResistanceCal = Resistance; }

        /// <summary> Updates the current supply voltage used for readings. Gets used in both calibrated and uncalibrated readings. </summary>
        /// <param name="SupplyVoltage"> The current supply voltage (should be close to 5V). </param>
        public void CalibrateSupply(double SupplyVoltage) { this.SupplyVoltage = SupplyVoltage; }

        /// <summary> Approximates pollutant PPM from sensor output. </summary>
        /// <remarks> See comments at the top of this class to understand the expected electrical setup and values. </remarks>
        private void CalculatePPM()
        {
            double SensorResistance = ((this.RL * this.SupplyVoltage) / this.Input.GetInput()) - (this.RS + this.RL);
            double ResistanceRatio = SensorResistance / this.ResistanceCal;
            if (this.TraceLogging) { Log.Trace(this, "Calculated resistance: " + SensorResistance + ", Ratio: " + ResistanceRatio); }
            if (ResistanceRatio < 0.358 || ResistanceRatio > 2.428)
            {
                this.LastReadingUncal = -1;
                this.LastReadingCal = -1;
                return;
            }
            this.LastReadingUncal = (float)(116.6020682 * Math.Pow(ResistanceRatio, -2.769034857));

            // double CalibrationMult = (UseCal ? GetCalibrationMultipler(this.Humidity, this.Temperature) : 1.0);
            // this.LastReadingCal = ...
        }

        // TODO: This still needs to be implemented.
        // I'm not sure if it will actually be useful.
        // Math here: http://davidegironi.blogspot.com/2017/07/mq-gas-sensor-correlation-function.html#.WxltRyAh2Un
        private static double GetCalibrationMultipler(float Humidity, float Temperature)
        {
            return 1.0;
        }
    }
}
