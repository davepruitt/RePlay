using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay_Exercises
{
    /// <summary>
    /// An enumeration that defines the set of broad classes that our devices belong to.
    /// </summary>
    public enum ExerciseDeviceType
    {
        [Description("Box")]
        Box,

        [Description("FitMi")]
        FitMi,

        [Description("Keyboard")]
        Keyboard,

        [Description("ReCheck")]
        ReCheck,

        [Description("Touchscreen")]
        Touchscreen,
        
        [Description("Unknown")]
        Unknown
    }
}