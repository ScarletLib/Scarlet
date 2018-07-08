using System;

namespace Scarlet.Components
{
    public interface ISubsystem
    {
        /// <summary> Prepares the subsystem for use. </summary>
        void Initialize();

        /// <summary> Stops all activities (outputs, motors, etc) immediately. This needs to be passed through to all relevant subcomponents. </summary>
        void EmergencyStop();

        /// <summary> Updates the subsystem. This should be passed through to all subcomponents. </summary>
        void UpdateState();

        /// <summary> Stops and closes all relevant resources. Called when the program is exiting. </summary>
        void Exit();

        /// <summary> Whether to output extended debug information. Actual output varies by subsystem. </summary>
        bool TraceLogging { get; set; }
    }
}
