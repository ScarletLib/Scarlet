using System;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    // Math based on http://davidegironi.blogspot.com/2017/05/mq-gas-sensor-correlation-function.html#.Wo84_UxFx9A
    public class MQ135 : ISensor
    {
        public string System { get; set; }
        private IAnalogueIn Input;
        private int Resistor;
        private float SupplyVoltage;
        private int ResistanceCal = 41763;
        private float LastReading, Temperature, Humidity;

        /* Typical circuit setup:
         * 
         *   V+    MQ135
         *   ^-----/\/\/\----o----> Analogue Input
         *                   |
         *           RL      |
         *   v-----/\/\/\----/
         *  GND
         *  
         *  Where the MQ135 resistance changes based on pollutant concentration in the air, RL is constant and specified in <c>Resistor</c>.
         */

        public MQ135(IAnalogueIn Input, int Resistor, float SupplyVoltage = 5)
        {
            this.Input = Input;
            this.Resistor = Resistor;
            this.SupplyVoltage = SupplyVoltage;
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
        public float GetReading() => CalculatePPM(this.Input, this.Resistor, this.ResistanceCal, this.SupplyVoltage, true, this.Humidity, this.Temperature);

        /// <summary> Gets the most recent sensor reading, without applying temperature or humidity compensation. </summary>
        /// <returns> The estimated total ppm of pollutants in the air. </returns>
        public float GetReadingUncalibrated() => CalculatePPM(this.Input, this.Resistor, this.ResistanceCal, this.SupplyVoltage, false, 0, 0);

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

        public static float CalculatePPM(IAnalogueIn Input, int Resistor, int ResistanceCal, float SupplyVoltage, bool UseCal, float Humidity, float Temperature)
        {
            double SensorResistance = ((Resistor * SupplyVoltage) / Input.GetInput()) - Resistor; // R2 = ((R1 * Vin) / Vout) - R1
            double ResistanceRatio = SensorResistance / ResistanceCal;
            double CalibrationMult = (UseCal ? GetCalibrationMultipler(Humidity, Temperature) : 1.0);
            Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "MQ135 Calculated resistance: " + SensorResistance + ", Ratio: " + ResistanceRatio);
            if (ResistanceRatio < 0.358 || ResistanceRatio > 2.428) { throw new Exception("Sensor values are out of spec. Cannot get reading."); }
            return (float)(116.6020682 * Math.Pow(ResistanceRatio, -2.769034857));
        }

        // TODO: This still needs to be implemented.
        // I'm not sure if it will actually be useful.
        private static double GetCalibrationMultipler(float Humidity, float Temperature)
        {
            return 1.0;
        }
    }
}
