using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scarlet.Components.Sensors;
using Scarlet.IO;
using Scarlet.IO.BeagleBone;
using Scarlet.IO.RaspberryPi;
using Scarlet.Utilities;

namespace Scarlet.TestSuite
{
    public class Device
    {
        public static void Start(string[] args)
        {
            if (args.Length < 2) { TestMain.ErrorExit("Device testing needs device to test."); }
            switch (args[1].ToLower())
            {
                case "hx711":
                {
                    if (args.Length < 5) { TestMain.ErrorExit("Insufficient info to run HX711 test. See help."); }
                    IDigitalIn DataPin = null;
                    IDigitalOut ClockPin = null;
                    if (args[2].Equals("pi", StringComparison.InvariantCultureIgnoreCase))
                    {
                        RaspberryPi.Initialize();
                        DataPin = new DigitalInPi(int.Parse(args[3]));
                        ClockPin = new DigitalOutPi(int.Parse(args[4]));
                    }
                    else if (args[2].Equals("bbb", StringComparison.InvariantCultureIgnoreCase))
                    {
                        BeagleBone.Initialize(SystemMode.DEFAULT, true);
                        BBBPin DataBBBPin = IOBBB.StringToPin(args[3]);
                        BBBPin ClockBBBPin = IOBBB.StringToPin(args[4]);
                        BBBPinManager.AddMappingGPIO(DataBBBPin, false, ResistorState.NONE);
                        BBBPinManager.AddMappingGPIO(ClockBBBPin, true, ResistorState.NONE);
                        BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_IF_NONE);
                        DataPin = new DigitalInBBB(DataBBBPin);
                        ClockPin = new DigitalOutBBB(ClockBBBPin);
                    }
                    else { TestMain.ErrorExit("HX711 test: Unknown platform. See help."); }
                    HX711 DUT = new HX711(ClockPin, DataPin);
                    while (Console.KeyAvailable) { Console.ReadKey(); }
                    Log.Output(Log.Severity.INFO, Log.Source.GUI, "[w] to increase gain, [s] to decrease. [z] to zero.");
                    Log.Output(Log.Severity.INFO, Log.Source.GUI, "Press any other key to exit.");

                    HX711.Gain Gain = HX711.Gain.GAIN_128x;
                    bool Continue = true;
                    while (Continue)
                    {
                        if (Console.KeyAvailable)
                        {
                            char Key = Console.ReadKey().KeyChar;
                            switch (Key)
                            {
                                case 'w':
                                {
                                    if (Gain == HX711.Gain.GAIN_32x) { Gain = HX711.Gain.GAIN_64x; }
                                    else if (Gain == HX711.Gain.GAIN_64x) { Gain = HX711.Gain.GAIN_128x; }
                                    else { Log.Output(Log.Severity.ERROR, Log.Source.SENSORS, "Gain at maximum already."); }
                                    DUT.SetGain(Gain);
                                    Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "Gain now at " + Gain);
                                    break;
                                }
                                case 's':
                                {
                                    if (Gain == HX711.Gain.GAIN_128x) { Gain = HX711.Gain.GAIN_64x; }
                                    else if (Gain == HX711.Gain.GAIN_64x) { Gain = HX711.Gain.GAIN_32x; }
                                    else { Log.Output(Log.Severity.ERROR, Log.Source.SENSORS, "Gain at minimum already."); }
                                    DUT.SetGain(Gain);
                                    Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "Gain now at " + Gain);
                                    break;
                                }
                                case 'z':
                                {
                                    DUT.Tare();
                                    Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "Tared.");
                                    break;
                                }
                                default:
                                {
                                    Continue = false;
                                    break;
                                }
                            }
                        }
                        DUT.UpdateState();
                        Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "HX711 readings: Raw: " + DUT.GetRawReading() + ", Adjusted: " + DUT.GetAdjustedReading());
                        Thread.Sleep(250);
                    }
                    break;
                }
                default:
                {
                    TestMain.ErrorExit("Unknown device.");
                    break;
                }
            }
        }
    }
}
