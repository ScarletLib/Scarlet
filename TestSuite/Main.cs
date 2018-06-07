using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.TestSuite
{
    public class TestMain
    {
        public static void Main(string[] args)
        {
            Log.Begin();
            Log.SetGlobalOutputLevel(Log.Severity.DEBUG);
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "Starting Scarlet test suite...");
            if (args == null || args.Length == 0 || args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase)) { OutputHelp(); }

            StateStore.Start("ScarletTest");

            if (args.Length < 1) { ErrorExit("Must provide system to test."); }
            if (args[0].Equals("io", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length < 2) { ErrorExit("io command requires platform."); }
                if (args[1].Equals("bbb", StringComparison.InvariantCultureIgnoreCase))
                {
                    IOBBB.Start(args);
                }
                else if (args[1].Equals("pi", StringComparison.InvariantCultureIgnoreCase))
                {
                    IOPi.Start(args);
                }
                else { Log.Output(Log.Severity.ERROR, Log.Source.GUI, "io command must be for pi or bbb."); }
            }
            else if(args[0].Equals("perf", StringComparison.InvariantCultureIgnoreCase))
            {
                Performance.Start(args);
            }
            else if(args[0].Equals("utils", StringComparison.InvariantCultureIgnoreCase))
            {
                Utils.Start(args);
            }
        }

        public static void ErrorExit(string error)
        {
            Log.Output(Log.Severity.ERROR, Log.Source.GUI, error);
            Environment.Exit(-1);
        }

        private static void OutputHelp()
        {
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "Scarlet Test Suite: Help");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "Command structure usually is ScarletTest <system> <type> <args>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "Valid Commands:");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "io <bbb/pi> ___");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   digin <pin> [repetitions]");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   digout <pin> <high/low/blink>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   pwm <pin> <freq> ___");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      per <dutycycle (0-100)>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      sine");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   i2c ___");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      read <address (hex)> <register (hex)> <length>");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      write <address (hex)> <register (hex)> <data>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   adc <pin>");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   uart ___");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      read <byte>");
            //Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "      write <data>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   int <pin> <rise/fall/both>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   NOTES: pins should either be a number for Pi, or in the format P9_21 for BBB.");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "perf ___");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   DataUnit <num>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   filter <average/lowpass> <cycles>");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "utils ___");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "   DataLog");
            Log.ForceOutput(Log.Severity.INFO, Log.Source.GUI, "More will be added!");
            Environment.Exit(0);
        }
    }
}
