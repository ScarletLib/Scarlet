using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    public class HX711 : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }

        public HX711(IDigitalOut Clock, IDigitalOut Data)
        {

        }

        public void UpdateState()
        {

        }

        public bool Test()
        {
            return true; // TODO: Implement testing.
        }

        public DataUnit GetData()
        {
            return new DataUnit(this.System)
            {
                // TODO: Add data to DataUnit.
            };
        }

    }
}
