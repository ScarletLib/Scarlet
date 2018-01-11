using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Utilities
{
    /// <summary>
    /// Class to store constants
    /// </summary>
    public class Constants
    {
        // PHYSICAL CONSTANTS
        public const float GRAVITY = 9.80655f; // meters per second per second
        public const float EARTH_RADIUS = 6371000; // meters

        // OPERATIONAL CONSTANTS
        public const int DEFAULT_MIN_THREAD_SLEEP = 15; // milliseconds
    }

    public static class UtilConstants
    {
        public static string SEARCH_NOT_FOUND_STR = "*-+{}SEARCH_NOT_FOUND?/\\";
    }

    public enum SearchType
    {
        BreadthFirst,
        DepthFirst,
        SingleFolder,
    }
}
