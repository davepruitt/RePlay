using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay.Manager
{
    /// <summary>
    /// A simple class to handle retrieving the build date from the BuildDate.txt file
    /// </summary>
    public static class BuildInformationManager
    {
        public static string GetVersionName (Activity current_activity)
        {
            var context = current_activity.ApplicationContext;
            string version_name = context.PackageManager.GetPackageInfo(context.PackageName, 0).VersionName;

            return version_name;
        }

        public static DateTime RetrieveBuildDate (Activity current_activity)
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
    }
}