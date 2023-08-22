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

using Microsoft.Xna.Framework;

namespace RePlay_Activity_TyperShark.Main
{
    /// <summary>
    /// The possible shark types in the game and their string descriptions
    /// </summary>
    public enum SharkType
    {
        [Description("piranha1")]
        Pirahna1,

        [Description("shark1")]
        Shark1,

        [Description("hammerhead")]
        Hammerhead,

        [Description("tiger shark")]
        TigerShark,

        [Description("piranha2")]
        Pirahna2,

        [Description("ghost shark")]
        GhostShark,

        [Description("toxic shark")]
        ToxicShark,

        [Description("jellyfish")]
        Jellyfish,

        [Description("unknown")]
        Unknown
    }
}