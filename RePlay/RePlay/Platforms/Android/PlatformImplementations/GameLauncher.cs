using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay.CrossPlatform
{
    public partial class GameLauncher
    {
        public static partial void LaunchGame()
        {
            var current_activity = Platform.CurrentActivity;
            if (current_activity != null)
            {
                Type t = Type.GetType("MonogameProject1.Activity1, MonogameProject1");
                Intent intent = new Intent(current_activity, t);
                current_activity.StartActivityForResult(intent, 0);
            }
        }
    }
}
