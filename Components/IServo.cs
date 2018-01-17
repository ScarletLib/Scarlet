using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Components
{
    public interface IServo
    {
        /// <summary> Updates the position of the servo </summary>
        void SetPosition(int NewPosition);

        /// <summary> Enables or disables the motor immediately. </summary>
        void SetEnabled(bool Enabled);

        /// <summary> Used to send events to servos. </summary>
        void EventTriggered(object Sender, EventArgs Event);
    }
}
