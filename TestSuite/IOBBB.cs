using Scarlet.Components.Sensors;
using Scarlet.IO;
using Scarlet.IO.BeagleBone;
using Scarlet.Utilities;
using System;
using System.Threading;

namespace Scarlet.TestSuite
{
    public class IOBBB
    {

        public static void Start(string[] args)
        {
            if (args.Length < 3) { TestMain.ErrorExit("io bbb command requires functionality to test."); }
            BeagleBone.Initialize(SystemMode.DEFAULT, true);

            switch (args[2].ToLower())
            {
                case "digin":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io bbb digin command requires pin to test."); }
                    BBBPin InputPin = StringToPin(args[3]);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing digital input on BBB pin " + InputPin.ToString());
                    BBBPinManager.AddMappingGPIO(InputPin, false, ResistorState.PULL_DOWN);
                    BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_REGARDLESS);
                    IDigitalIn Input = new DigitalInBBB(InputPin);
                    while(true)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Current pin state: " + (Input.GetInput() ? "HIGH" : "LOW"));
                        Thread.Sleep(250);
                    }
                }
                case "digout":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io bbb digout command requires pin to test."); }
                    BBBPin OutputPin = StringToPin(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io bbb digout command requires output mode (high/low/blink)."); }
                    if(args[4] != "high" && args[4] != "low" && args[4] != "blink") { TestMain.ErrorExit("Invalid digout test mode supplied."); }
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing digital output on BBB pin " + OutputPin.ToString());
                    BBBPinManager.AddMappingGPIO(OutputPin, true, ResistorState.PULL_DOWN);
                    BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_REGARDLESS);
                    IDigitalOut Output = new DigitalOutBBB(OutputPin);
                    if (args[4] == "high") { Output.SetOutput(true); }
                    else if (args[4] == "low") { Output.SetOutput(false); }
                    else
                    {
                        bool Out = false;
                        while(true)
                        {
                            Output.SetOutput(Out);
                            Out = !Out;
                            Thread.Sleep(250);
                        }
                    }
                    break;
                }
                case "pwm":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io bbb pwm command requires pin to test."); }
                    BBBPin OutputPin = StringToPin(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io bbb pwm command requires frequency."); }
                    int Frequency = int.Parse(args[4]);
                    if (args.Length < 6) { TestMain.ErrorExit("io bbb pwm command requires output mode."); }
                    if (args[5] != "per" && args[5] != "sine") { TestMain.ErrorExit("io bbb pwm command invalid (per/sine)."); }
                    if (args[5] == "per" && args.Length < 7) { TestMain.ErrorExit("io bbb pwm per must be provided duty cycle."); }
                    BBBPinManager.AddMappingPWM(OutputPin);
                    BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_REGARDLESS);
                    IPWMOutput Output = PWMBBB.GetFromPin(OutputPin);
                    Output.SetFrequency(Frequency);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing PWM output on BBB pin " + OutputPin.ToString() + " at " + Frequency + "Hz.");
                    if (args[5] == "per")
                    {
                        Output.SetOutput(int.Parse(args[6]) / 100F);
                        Output.SetEnabled(true);
                        Thread.Sleep(15000); // Not sure if it stops outputting when the program exits.
                    }
                    else
                    {
                        int Cycle = 0;
                        while (true)
                        {
                            float Val = (float)((Math.Sin(Cycle * Math.PI / 180.000D) + 1) / 2);
                            Output.SetOutput(Val);
                            Thread.Sleep(50);
                            Cycle += 20;
                        }
                    }
                    break;
                }
                case "adc":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io bbb adc command requires pin to test."); }
                    BBBPin InputPin = StringToPin(args[3]);
                    BBBPinManager.AddMappingADC(InputPin);
                    BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_REGARDLESS);
                    IAnalogueIn Input = new AnalogueInBBB(InputPin);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing analogue input on BBB pin " + InputPin.ToString());
                    while(true)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "ADC Input: " + Input.GetInput() + " (Raw: " + Input.GetRawInput() + ")");
                        Thread.Sleep(250);
                    }
                }
                case "int":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io bbb int command requires pin to test."); }
                    BBBPin InputPin = StringToPin(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io bbb int command requires interrupt mode (rise/fall/both)."); }
                    if (args[4] != "rise" && args[4] != "fall" && args[4] != "both") { TestMain.ErrorExit("Invalid interrupt mode supplied."); }

                    BBBPinManager.AddMappingGPIO(InputPin, true, ResistorState.PULL_DOWN);
                    BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_REGARDLESS);
                    IDigitalIn Input = new DigitalInBBB(InputPin);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing interrupts on BBB pin " + InputPin.ToString());
                    switch (args[4])
                    {
                        case "rise": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.RISING_EDGE); break;
                        case "fall": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.FALLING_EDGE); break;
                        case "both": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.ANY_EDGE); break;
                    }
                    while(true) { Thread.Sleep(50); } // Program needs to be running to receive.
                }
                case "mtk3339":
                {
                        BBBPinManager.AddMappingUART(BBBPin.P9_24);
                        BBBPinManager.AddMappingUART(BBBPin.P9_26);
                        BBBPinManager.ApplyPinSettings(BBBPinManager.ApplicationMode.APPLY_IF_NONE);
                        IUARTBus UART = UARTBBB.UARTBus1;
                        Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Press any key to stop.");
                        while(Console.KeyAvailable) { Console.ReadKey(); }
                        MTK3339 GPS = new MTK3339(UART);
                        Thread.Sleep(1000);
                        GPS.SetUpdateInterval(1000);
                        while (!Console.KeyAvailable) { Thread.Sleep(10); }
                        GPS.Stop();
                        break;
                }
            }
        }

        //Extra methods from Science code:
        /*
        internal static void TestUART()
        {
            BBBPinManager.AddMappingUART(BBBPin.P9_13);
            BBBPinManager.ApplyPinSettings(RoverMain.ApplyDevTree);
            IUARTBus Bus = UARTBBB.UARTBus4;
            while(true)
            {
                Bus.Write(new byte[] { 0x45, 0x72, 0x7A, 0x61 });
                Thread.Sleep(50);
            }
        }

        internal static void TestSPI()
        {
            BBBPinManager.AddMappingsSPI(BBBPin.P9_21, BBBPin.NONE, BBBPin.P9_22);
            BBBPinManager.AddMappingSPI_CS(BBBPin.P9_12);
            BBBPinManager.ApplyPinSettings(RoverMain.ApplyDevTree);
            IDigitalOut CS_Thermo = new DigitalOutBBB(BBBPin.P9_12);
            MAX31855 Thermo = new MAX31855(SPIBBB.SPIBus0, CS_Thermo);
            Log.SetSingleOutputLevel(Log.Source.SENSORS, Log.Severity.DEBUG);
            for (int i = 0; i < 100; i++)
            {
                Thermo.UpdateState();
                Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "Thermocouple Data, Faults: " + string.Format("{0:G}", Thermo.GetFaults()) + ", Internal: " + Thermo.GetInternalTemp() + ", External: " + Thermo.GetExternalTemp() + " (Raw: " + Thermo.GetRawData() + ")");
                Thread.Sleep(500);
            }
        }

        internal static void TestI2C()
        {
            BBBPinManager.AddMappingGPIO(BBBPin.P8_08, true, Scarlet.IO.ResistorState.PULL_DOWN);
            BBBPinManager.AddMappingsI2C(BBBPin.P9_24, BBBPin.P9_26);
            BBBPinManager.ApplyPinSettings(RoverMain.ApplyDevTree);
            VEML6070 UV = new VEML6070(I2CBBB.I2CBus1);
            Log.SetSingleOutputLevel(Log.Source.SENSORS, Log.Severity.DEBUG);
            for (int i = 0; i < 20; i++)
            {
                UV.UpdateState();
                Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "UV Reading: " + UV.GetData());
                Thread.Sleep(200);
            }
        }

        internal static void TestMotor()
        {
            BBBPinManager.AddMappingPWM(BBBPin.P9_14);
            BBBPinManager.ApplyPinSettings(RoverMain.ApplyDevTree);
            IPWMOutput MotorOut = PWMBBB.PWMDevice1.OutputA;
            IFilter<float> MotorFilter = new Average<float>(5);
            TalonMC Motor = new TalonMC(MotorOut, 1F, MotorFilter);
            Log.SetSingleOutputLevel(Log.Source.MOTORS, Log.Severity.DEBUG);
            Motor.SetSpeed(0.2f);
            int Cycle = 0;
            while (true)
            {
                Motor.SetSpeed(((Cycle / 10) % 2 == 0) ? 1 : -1);
                Thread.Sleep(25);
                Cycle += 1;
            }
        }
        */


        public static void GetInterrupt(object senser, InputInterrupt evt) => Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Interrupt Received! Now " + evt.NewState);

        public static BBBPin StringToPin(string pinName)
        {
            try
            {
                BBBPin Value = (BBBPin)Enum.Parse(typeof(BBBPin), pinName);
                if (Enum.IsDefined(typeof(BBBPin), Value)) { return Value; }
                else { throw new IndexOutOfRangeException("Not a pin"); }
            }
            catch
            {
                TestMain.ErrorExit("Given BBBPin is invalid.");
                return BBBPin.NONE;
            }
            
        }

    }
}
