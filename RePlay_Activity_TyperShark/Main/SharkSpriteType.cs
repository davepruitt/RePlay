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

namespace RePlay_Activity_TyperShark.Main
{
    /// <summary>
    /// Different classifications for sprites of each shark
    /// </summary>
    public enum SharkSpriteType
    {
        [Description("swim")]
        Swim,

        [Description("death")]
        Death,

        [Description("shock")]
        Shock,

        [Description("unknown")]
        Unknown
    }
}