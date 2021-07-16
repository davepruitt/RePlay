using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay_VNS_Triggering
{
    public class PCM_DebugModeEvent_EventArgs
    {
        public DateTime MessageTimestamp = DateTime.Now;
        public string PrimaryMessage = string.Empty;
        public Dictionary<string, string> SecondaryMessages = new Dictionary<string, string>();
    }
}