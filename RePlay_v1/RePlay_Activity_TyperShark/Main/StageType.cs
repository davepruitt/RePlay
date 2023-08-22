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
    public enum StageType
    {
        SingleShark_WordAtBottom,
        SingleShark_Numbers,
        SingleShark_Sentences,
        Normal,
        Normal_Numbers,
        Jellyfish_Attack,
        OceanFloor_ShipwreckBonus
    }
}