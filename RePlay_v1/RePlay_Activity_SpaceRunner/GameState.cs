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

namespace RePlay_Activity_SpaceRunner
{
    public enum GameState
    {
        STARTING,
        PAUSED,
        RUNNING,
        GAMEOVER,
        ERROR_ENCOUNTERED,
        ERROR_RECOVERED,
    }
}