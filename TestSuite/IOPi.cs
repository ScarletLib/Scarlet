using Scarlet.IO;
using Scarlet.IO.RaspberryPi;
using Scarlet.Utilities;
using System;
using System.Threading;

namespace Scarlet.TestSuite
{
    public class IOPi
    {

        public static void Start(string[] args)
        {
            if (args.Length < 3) { TestMain.ErrorExit("io pi command requires functionality to test."); }
            RaspberryPi.Initialize();

            switch(args[2].ToLower())
            {
                case "digin":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io pi digin command requires pin to test."); }
                    int PinNum = int.Parse(args[3]);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing digital input on RPi pin " + PinNum);
                    IDigitalIn Input = new DigitalInPi(PinNum);
                    while (true)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Current pin state: " + (Input.GetInput() ? "HIGH" : "LOW"));
                        Thread.Sleep(250);
                    }
                }
                case "digout":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io pi digout command requires pin to test."); }
                    int PinNum = int.Parse(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io pi digout command requires output mode (high/low/blink)."); }
                    if (args[4] != "high" && args[4] != "low" && args[4] != "blink") { TestMain.ErrorExit("Invalid digout test mode supplied."); }
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing digital output on RPi pin " + PinNum);
                    IDigitalOut Output = new DigitalOutPi(PinNum);
                    if (args[4] == "high") { Output.SetOutput(true); }
                    else if (args[4] == "low") { Output.SetOutput(false); }
                    else
                    {
                        bool Out = false;
                        while (true)
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
                    TestMain.ErrorExit("io pi pwm command not yet implemented."); // TODO: Remove when implementing.
                    if (args.Length < 4) { TestMain.ErrorExit("io pi pwm command requires pin to test."); }
                    int PinNum = int.Parse(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io pi pwm command requires frequency."); }
                    int Frequency = int.Parse(args[4]);
                    if (args.Length < 6) { TestMain.ErrorExit("io pi pwm command requires output mode."); }
                    if (args[5] != "per" && args[5] != "sine") { TestMain.ErrorExit("io pi pwm command invalid (per/sine)."); }
                    if (args[5] == "per" && args.Length < 7) { TestMain.ErrorExit("io pi pwm per must be provided duty cycle."); }
                        IPWMOutput Output = null; // TODO: Implement RPi PWM output.
                    Output.SetFrequency(Frequency);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing PWM output on RPi pin " + PinNum + " at " + Frequency + "Hz.");
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
                case "adc": { TestMain.ErrorExit("RPI does not have an ADC."); break; }
                case "int":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io pi int command requires pin to test."); }
                    int PinNum = int.Parse(args[3]);
                    if (args.Length < 5) { TestMain.ErrorExit("io pi int command requires interrupt mode (rise/fall/both)."); }
                    if (args[4] != "rise" && args[4] != "fall" && args[4] != "both") { TestMain.ErrorExit("Invalid interrupt mode supplied."); }

                    IDigitalIn Input = new DigitalInPi(PinNum);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing interrupts on RPi pin " + PinNum);
                    switch (args[4])
                    {
                        case "rise": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.RISING_EDGE); break;
                        case "fall": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.FALLING_EDGE); break;
                        case "both": ((IInterruptSource)Input).RegisterInterruptHandler(GetInterrupt, InterruptType.ANY_EDGE); break;
                    }
                    while (true) { Thread.Sleep(50); } // Program needs to be running to receive.
                }
                case "outperf":
                {
                    if (args.Length < 4) { TestMain.ErrorExit("io pi outperf command requires pin to test."); }
                    int PinNum = int.Parse(args[3]);
                    Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Testing digital output speed on RPi pin " + PinNum);
                    IDigitalOut Output = new DigitalOutPi(PinNum);
                    bool Out = false;
                    while (!Console.KeyAvailable)
                    {
                        Output.SetOutput(Out);
                        Out = !Out;
                    }
                    Output.SetOutput(false);
                    break;
                }
            }
        }

        // Extra methods from Science code:
        /*
        internal static void TestSPI()
        {
            IDigitalOut CS_Thermo = new DigitalOutPi(7);
            MAX31855 Thermo = new MAX31855(new SPIBusPi(0), CS_Thermo);
            Log.SetSingleOutputLevel(Log.Source.SENSORS, Log.Severity.DEBUG);
            for (int i = 0; i < 100; i++)
            {
                Thermo.UpdateState();
                Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "Thermocouple Data, Faults: " + string.Format("{0:G}", Thermo.GetFaults()) + ", Internal: " + Thermo.GetInternalTemp() + ", External: " + Thermo.GetExternalTemp() + " (Raw: " + Thermo.GetRawData() + ")");
                Thread.Sleep(500);
            }
        }

        internal static void TestUART()
        {
            IUARTBus UART = new UARTBusPi(0, 9600);
            for (byte i = 0; i < 255; i++)
            {
                UART.Write(new byte[] { i });
                Thread.Sleep(250);
            }
        }

        internal static void TestI2C()
        {
            II2CBus Bus = new I2CBusPi();
            VEML6070 UV = new VEML6070(Bus);
            Log.SetSingleOutputLevel(Log.Source.SENSORS, Log.Severity.DEBUG);
            for (int i = 0; i < 50; i++)
            {
                UV.UpdateState();
                Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "UV Reading: " + UV.GetData());
                Thread.Sleep(200);
            }
        }
        */

        public static void GetInterrupt(object senser, InputInterrupt evt) => Log.Output(Log.Severity.INFO, Log.Source.HARDWAREIO, "Interrupt Received! Now " + evt.NewState);
    }
}
