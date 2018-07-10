using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors.Platform
{
    public class CPUMonitor
    {
        public List<CPU> CPUs { get; private set; }

        /// <summary> Creates CPUMonitor immediately gets all CPU information. </summary>
        public CPUMonitor() { UpdateAllCPUs(); }

        public void UpdateAllCPUs()
        {
            FindCPUs();
            foreach(CPU CPU in CPUs) { CPU.UpdateState(); }
        }

        private void FindCPUs() { throw new NotImplementedException(); }

        public class CPU : ISensor
        {
            public string System { get; set; }
            public bool TraceLogging { get; set; }

            public readonly int ID;
            public float Utilization { get; private set; }
            public float ClockSpeed { get; private set; }
            public int Cores { get; private set; }
            public int LogicalProcessors { get; private set; }
            public string Model { get; private set; }

            internal CPU(int ID) { this.ID = ID; }

            public void UpdateState()
            {
                Utilization = CPUSensor.GetUtilization(ID);
                Cores = CPUSensor.GetCoreCount(ID);
                ClockSpeed = CPUSensor.GetRawClockSpeed(ID);
                LogicalProcessors = CPUSensor.GetLogicalProcessors(ID);
                Model = CPUSensor.GetModel(ID);
            }

            public bool Test() { return true; }

            public DataUnit GetData()
            {
                return new DataUnit("CPU#" + ID.ToString())
                {
                    { "Model", Model },
                    { "Utilization", Utilization },
                    { "ClockSpeed", ClockSpeed },
                    { "Cores", Cores },
                    { "LogicalProcessors", LogicalProcessors }
                }.SetSystem(System);
            }
        }

        // Not 100% sure how to best do this... Going to start some research.
        private static class CPUSensor
        {
            internal static float GetUtilization(int ID = 0) { throw new NotImplementedException(); }
            internal static float GetRawClockSpeed(int ID = 0) { throw new NotImplementedException(); }
            internal static int GetCoreCount(int ID = 0) { throw new NotImplementedException(); }
            internal static int GetLogicalProcessors(int ID = 0) { throw new NotImplementedException(); }
            internal static string GetModel(int ID = 0) { throw new NotImplementedException(); }
        }
    }
}
