using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Communications
{
    /// <summary>
    /// This class contains the endpoint of the connection, and the status of the connection.
    /// It overloads EventArgs and is used in triggering connection changes between points on the communication stream.
    /// </summary>
    public class ConnectionStatusChanged : EventArgs
    {
        /// <summary> The Endpoint that either is or is not connected to the current system. </summary>
        public string StatusEndpoint { get; set; }

        /// <summary> The status of the endpoints connection with the current system. </summary>
        public bool StatusConnected { get; set; }
    }

    public class BufferLengthChanged : EventArgs
    {
        public int PreviousLength { get; set; }
        public int Length { get; set; }
    }

    public class TimeSynchronizationOccurred : EventArgs { }

    public class ConnectionQualityChanged : EventArgs
    {
        public string Endpoint { get; set; }
        public int PreviousQuality { get; set; }
        public int Quality { get; set; }
    }
}
