using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_DeviceCommunications
{
    /// <summary>
    /// Defines the kinds of devices that Replay has available
    /// </summary>
    public enum ReplayDeviceType
    {
        [Description("Left-handed Pinch")]
        Pinch_Left,

        [Description("Pinch")]
        Pinch,

        [Description("Knob")]
        Knob,

        [Description("Isometric knob")]
        Knob_Isometric,

        [Description("Wrist")]
        Wrist,

        [Description("Isometric wrist")]
        Wrist_Isometric,

        [Description("Handle")]
        Handle,

        [Description("Isometric handle")]
        Handle_Isometric,

        [Description("Unknown")]
        Unknown
    }
}
