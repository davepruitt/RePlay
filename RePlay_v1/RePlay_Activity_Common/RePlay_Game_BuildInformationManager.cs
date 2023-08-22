using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay_Activity_Common
{
    /// <summary>
    /// A simple class to handle retrieving the build date from the BuildDate.txt file
    /// </summary>
    public static class RePlay_Game_BuildInformationManager
    {
        public static string GetVersionName ()
        {
            var context = global::Android.App.Application.Context;

            PackageManager manager = context.PackageManager;
            PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);

            return info.VersionName;
        }

        public static string GetVersionCode ()
        {
            var context = global::Android.App.Application.Context;
            PackageManager manager = context.PackageManager;
            PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);

            return info.VersionCode.ToString();
        }

        public static DateTime GetBuildDate(Activity current_activity)
        {
            try
            {
                string content = string.Empty;
                AssetManager assets = current_activity.Assets;
                using (StreamReader sr = new StreamReader(assets.Open("BuildDate.txt")))
                {
                    content = sr.ReadToEnd();
                }

                bool success = DateTime.TryParse(content, out DateTime result);
                if (success)
                {
                    return result;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
    }
}