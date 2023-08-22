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

namespace RePlay_Activity_RepetitionsMode
{
    public enum SessionState
    {
        NotStarted,
        BeginResetBaseline,
        WaitResetBaseline,
        FinishResetBaseline,
        SessionRunning,
        ErrorDetected,
        DeviceMissing,
        SetupFailed
    }
}