using Scarlet.Utilities;
using System;

namespace Scarlet.Components
{
    public interface ISensor
    {
        /// <summary> Determine whether or not the sensor is working. </summary>
        bool Test();

        /// <summary> Updates the state of the sensor, usually done by getting a new reading. </summary>
        void UpdateState();

        /// <summary> Gets the sensor's data in a format that can be used for easy storage. </summary>
        DataUnit GetData();

        /// <summary> The sensor's purpose, like "GroundTemp". i.e. What does it measure? </summary>
        string System { get; set; }
    }
}
