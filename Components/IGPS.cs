using System;
using Scarlet.Utilities;

namespace Scarlet.Components
{
    public interface IGPS
    {
        /// <summary> Gets the GPS's current coordinates. </summary>
        /// <returns> A Tuple of Latitude then Logitude. </returns>
        Tuple<float, float> GetCoordinates();
    }
}
