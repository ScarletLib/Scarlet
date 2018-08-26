using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Utilities
{
    /// <summary> Class to store constants </summary>
    public class Constants
    {
        // PHYSICAL CONSTANTS
        public const float GRAVITY = 9.80655f; // meters per second per second
        public const float EARTH_RADIUS = 6371000; // meters

        // OPERATIONAL CONSTANTS
        public const int DEFAULT_MIN_THREAD_SLEEP = 15; // milliseconds
    }

    public enum ScarletVersions : byte
    {
        Version_0_2_0 = 0x00,
        Version_0_2_2 = 0x01,
        Version_0_2_3 = 0x02,
        Version_0_3_0 = 0x03,
        Version_0_3_1 = 0x04,
        Version_0_4_0 = 0x05,
        Version_0_5_0 = 0x06,
        Version_0_5_1 = 0x07,
    }

}
