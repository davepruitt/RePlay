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

namespace RePlay.Helpers
{
    public class StringHelper
    {
        public const int DefaultMaxAllowedLength = 16;

        public static string TruncateStringForUI (string source_string, 
            int max_allowed_length = DefaultMaxAllowedLength)
        {
            string result = string.Empty;
            
            if (!string.IsNullOrEmpty(source_string))
            {
                if (max_allowed_length > 0 && source_string.Length > max_allowed_length)
                {
                    if (max_allowed_length > 3)
                    {
                        result = source_string.Substring(0, max_allowed_length - 3) + "...";
                    }
                    else
                    {
                        result = source_string.Substring(0, max_allowed_length);
                    }
                }
                else
                {
                    result = source_string;
                }
            }

            return result;
        }
    }
}