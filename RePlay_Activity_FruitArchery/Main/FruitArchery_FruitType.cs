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
    /// <summary>
    /// A list of all fruits that can act as targets
    /// </summary>
    public enum FruitArchery_FruitType
    {
        [Description("fruit_apple")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Red)]
        Apple,

        [Description("fruit_cherry")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Red)]
        Cherry,

        [Description("fruit_grape")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Purple)]
        Grape,

        [Description("fruit_lemon")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Yellow)]
        Lemon,

        [Description("fruit_orange")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Orange)]
        Orange,

        [Description("fruit_pear")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Green)]
        Pear,

        [Description("fruit_pineapple")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Yellow)]
        Pineapple,

        [Description("fruit_strawberry")]
        [FruitArchery_FruitAttributes(FruitArchery_FruitSplatColor.Red)]
        Strawberry
    }
}