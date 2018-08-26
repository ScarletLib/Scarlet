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

        public const ScarletVersion SCARLET_VERSION = ScarletVersion.Master_Build;
    }

    public enum ScarletVersion : byte
    {
        Master_Build = 0x00,
        v0_2_0 = 0x01,
        v0_2_2 = 0x02,
        v0_2_3 = 0x03,
        v0_3_0 = 0x04,
        v0_3_1 = 0x05,
        v0_4_0 = 0x06,
        v0_5_0 = 0x07,
        v0_5_1 = 0x08,
    }

}
