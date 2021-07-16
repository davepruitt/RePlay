using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace RePlay.Manager
{
    public static class ProjectListManager
    {
        #region Methods

        public static List<string> GetProjectNames (Activity current_activity)
        {
            List<string> result = new List<string>();

            string content = string.Empty;
            using (StreamReader sr = new StreamReader(current_activity.Assets.Open("ProjectIDs.txt")))
            {
                content = sr.ReadToEnd();
            }

            var lines = content.Split(new char[] { '\n' }).ToList();
            lines = lines.Select(x => x.Trim()).ToList();
            lines = lines.Where(x => !string.IsNullOrEmpty(x)).ToList();

            result.AddRange(lines);

            return result;
        }

        #endregion
    }
}