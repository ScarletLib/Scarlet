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
        private IAnalogueIn Input;
        private double Slope, Intercept;
        private int ResistanceCal = 41763;
        private float LastReading, Temperature, Humidity;

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
         *  The resistor network yields a linear equation relating the sensor's resistance to the output voltage, which is defined by <c>Slope</c> and <c>Intercept</c>.
         *  So Resistance = (Slope * Input) + Intercept.
         */

        public MQ135(IAnalogueIn Input, double Slope, double Intercept)
        {
            this.Input = Input;
            this.Slope = Slope;
            this.Intercept = Intercept;
            this.Humidity = 0.65F;
            this.Temperature = 20;
        }

        public DataUnit GetData()
        {
            return new DataUnit("MQ135")
            {
                { "PPMReading", this.LastReading }
            }
            .SetSystem(this.System);
        }

        /// <summary> Due to this being an analogue sensor, this simply tries to get a reading and sees if it is within range. </summary>
        public bool Test()
        {
            try { GetReading(); }
            catch (Exception) { return false; }
            return true;
        }

        public void UpdateState() => this.LastReading = GetReading();

        /// <summary> Gets the most recent sensor reading. </summary>
        /// <returns> The estimated total ppm of pollutants in the air. </returns>
        public float GetReading() => CalculatePPM(this.Input, this.Slope, this.Intercept, this.ResistanceCal, true, this.Humidity, this.Temperature);

        /// <summary> Gets the most recent sensor reading, without applying temperature or humidity compensation. </summary>
        /// <returns> The estimated total ppm of pollutants in the air. </returns>
        public float GetReadingUncalibrated() => CalculatePPM(this.Input, this.Slope, this.Intercept, this.ResistanceCal, false, 0, 0);

        /// <summary> Applies some basic calibration to the sensor to compensate for air temperature. </summary>
        /// <remarks> Defaults to 20 C. </remarks>
        /// <param name="Temperature"> The current air temperature in degrees C, between -10 and 45. </param>
        public void CalibrateTemperature(float Temperature)
        {
            if (Temperature < -10 || Temperature > 45) { throw new Exception("MQ135 only works in -10 to 45 C temperatures."); }
            this.Temperature = Temperature;
        }

        /// <summary> Applies some basic calibration to the sensor to compensate for environmental humidity. </summary>
        /// <remarks> Defaults to 65% RH. </remarks>
        /// <param name="Humidity"> % Relative Humidity value, between 0.0 and 0.95. </param>
        public void CalibrateHumidity(float Humidity)
        {
            if (Humidity < 0 || Humidity > 0.95) { throw new Exception("MQ135 only works in 0 to 95% RH environment."); }
            this.Humidity = Humidity;
        }

        /// <summary> Applies calibration for base sensor resistance. This is probably constant for each individual sensor. </summary>
        /// <remarks> You'll need to put the sensor into a 100 ppm CO2 environment at 20C and 65% RH to get this value. 41763 is used as default. </remarks>
        /// <param name="Resistance"> The measured resistance of the sensor, in the specific calibration conditions as listed above. Should be near 42K. </param>
        public void CalibrateResistance(int Resistance) { this.ResistanceCal = Resistance; }

        public static float CalculatePPM(IAnalogueIn Input, double Slope, double Intercept, int ResistanceCal, bool UseCal, float Humidity, float Temperature)
        {
            double SensorResistance = Slope * Input.GetInput() + Intercept;
            double ResistanceRatio = SensorResistance / ResistanceCal;
            double CalibrationMult = (UseCal ? GetCalibrationMultipler(Humidity, Temperature) : 1.0);
            Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "MQ135 Calculated resistance: " + SensorResistance + ", Ratio: " + ResistanceRatio);
            if (ResistanceRatio < 0.358 || ResistanceRatio > 2.428) { return -1; }
            return (float)(116.6020682 * Math.Pow(ResistanceRatio, -2.769034857));
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
