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

namespace RePlay_Activity_TyperShark.Main
{
    public enum SharkState
    {
        Inactive,
        Inactive_Electrocuted,
        Inactive_Victorious,
        Inactive_Dead,
        Inactive_OutOfBounds,
        Active,
        Unknown
    }
}