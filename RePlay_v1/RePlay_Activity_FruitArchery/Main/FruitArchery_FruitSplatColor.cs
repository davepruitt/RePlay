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

namespace RePlay_Activity_FruitArchery.Main
{
    public enum FruitArchery_FruitSplatColor
    {
        [Description("splat_green")]
        Green,

        [Description("splat_orange")]
        Orange,

        [Description("splat_purple")]
        Purple,

        [Description("splat_red")]
        Red,

        [Description("splat_yellow")]
        Yellow,

        [Description("unknown")]
        Unknown
    }
}