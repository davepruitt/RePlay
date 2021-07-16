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

namespace RePlay_Activity_Common
{
    public class RePlayGamePauseMenuItemPressedEventArgs : EventArgs
    {
        public int MenuItemIndex { get; set; } = 0;
        public string MenuItemName { get; set; } = string.Empty;
    }
}