using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors.Platform
{
    public class CPUMonitor : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }

        public List<CPU> CPUs { get; private set; }

        public void UpdateState()
        {
            FindCPUs();
            foreach (CPU CPU in CPUs) { CPU.GetInfo(); }
        }

        public bool Test() { throw new NotImplementedException(); }

        public DataUnit GetData()
        {
            DataUnit CPUData = new DataUnit("CPU Monitor")
            {
                { "CPUs ( # )", CPUs.Count }
            }.SetSystem(System);

            // Add data from each CPU in the sensor 
            foreach (CPU CPU in CPUs)
            {
                CPUData.Add("CPU#" + CPU.ID.ToString() + " Model", CPU.Model);
                CPUData.Add("CPU#" + CPU.ID.ToString() + " Utilization ( % )", CPU.Utilization);
                CPUData.Add("CPU#" + CPU.ID.ToString() + " Clock Speed ( Hz )", CPU.ClockSpeed);
                CPUData.Add("CPU#" + CPU.ID.ToString() + " Cores ( # )", CPU.Cores);
                CPUData.Add("CPU#" + CPU.ID.ToString() + " Logical Processors ( # )", CPU.LogicalProcessors);
            }

            return CPUData;
        }

        private void FindCPUs() { throw new NotImplementedException(); }

        public class CPU
        {
            public readonly int ID;
            public float Utilization { get; private set; }
            public float ClockSpeed { get; private set; }
            public int Cores { get; private set; }
            public int LogicalProcessors { get; private set; }
            public string Model { get; private set; }

            internal CPU(int ID) { this.ID = ID; }

            public void GetInfo()
            {
                Utilization = CPUSensor.GetUtilization(ID);
                Cores = CPUSensor.GetCoreCount(ID);
                ClockSpeed = CPUSensor.GetRawClockSpeed(ID);
                LogicalProcessors = CPUSensor.GetLogicalProcessors(ID);
                Model = CPUSensor.GetModel(ID);
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
