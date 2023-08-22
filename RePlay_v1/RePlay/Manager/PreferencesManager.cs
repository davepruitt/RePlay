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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RePlay_Exercises;
using Xamarin.Essentials;

namespace RePlay.Manager
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static class PreferencesManager
    {
        public static string ProjectName
        {
            get
            {
                return Preferences.Get("ProjectName", string.Empty);
            }
            set
            {
                Preferences.Set("ProjectName", value);
            }
        }

        public static string SiteName
        {
            get
            {
                return Preferences.Get("SiteName", string.Empty);
            }
            set
            {
                Preferences.Set("SiteName", value);
            }
        }

        public static bool DebugMode
        {
            get
            {
                return Preferences.Get("DebugMode", false);
            }
            set
            {
                Preferences.Set("DebugMode", value);
            }
        }

        public static bool ShowPCMConnectionInGames
        {
            get
            {
                return Preferences.Get("ShowPCMConnectionInGames", true);
            }
            set
            {
                Preferences.Set("ShowPCMConnectionInGames", value);
            }
        }

        public static bool ShowStimulationRequestsInGames
        {
            get
            {
                return Preferences.Get("ShowStimulationRequestsInGames", true);
            }
            set
            {
                Preferences.Set("ShowStimulationRequestsInGames", value);
            }
        }

        public static void CreateNoiseFloorPreferencesFile ()
        {
            //Form the full path to the noise floor configuration file
            string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            path = Path.Combine(path, "TxBDC_NotData");
            path = Path.Combine(path, "RePlay");
            path = Path.Combine(path, "Configuration");
            path = Path.Combine(path, "vns_algorithm_noise_floors.json");

            FileInfo f_info = new FileInfo(path);
            if (!f_info.Exists)
            {
                //Create the folder if it does not exist
                new FileInfo(path).Directory.Create();

                //Write the file
                using (var writer = new StreamWriter(path))
                {
                    Dictionary<string, double> default_thresholds = new Dictionary<string, double>();
                    var enum_values = Enum.GetValues(typeof(ExerciseType));
                    foreach (ExerciseType enum_val in enum_values)
                    {
                        string str_val = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(enum_val);
                        double default_noise_floor = ExerciseTypeConverter.ConvertExerciseTypeToDefaultNoiseFloor(enum_val);
                        if (!double.IsNaN(default_noise_floor))
                        {
                            default_thresholds[str_val] = default_noise_floor;
                        }
                    }

                    //string json_str = JsonConvert.SerializeObject(default_thresholds, Formatting.Indented);
                    StringBuilder sb = new StringBuilder(string.Empty, 32767);
                    StringWriter sw = new StringWriter(sb);

                    var jsonSerializer = JsonSerializer.CreateDefault();
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
                    {
                        jsonWriter.Formatting = Formatting.Indented;
                        jsonWriter.IndentChar = ' ';
                        jsonWriter.Indentation = 4;

                        jsonSerializer.Serialize(jsonWriter, default_thresholds, default_thresholds.GetType());
                    }

                    string json_str = sw.ToString();

                    writer.WriteLine(json_str);
                    writer.Close();
                }
            }
        }

        public static IDictionary<string, JToken> ReadNoiseFloorPreferences()
        {
            //Form the full path to the noise floor configuration file
            string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            path = Path.Combine(path, "TxBDC_NotData");
            path = Path.Combine(path, "RePlay");
            path = Path.Combine(path, "Configuration");
            path = Path.Combine(path, "vns_algorithm_noise_floors.json");

            //Create the folder if it does not exist
            FileInfo f_info = new FileInfo(path);
            f_info.Directory.Create();
            if (!f_info.Exists)
            {
                //Create the file if it doesn't exist
                CreateNoiseFloorPreferencesFile();
            }

            //Read the file
            using (var reader = new StreamReader(path))
            {
                string file_contents = reader.ReadToEnd();
                JObject result = JsonConvert.DeserializeObject<JObject>(file_contents);
                return result;
            }
        }

        public static string GetTabletID (Activity current_activity)
        {
            string serial_number = string.Empty;

            try
            {
                var c = Java.Lang.Class.ForName("android.os.SystemProperties");
                var get = c.GetMethod("get", Java.Lang.Class.FromType(typeof(Java.Lang.String)));

                serial_number = (string)get.Invoke(c, "gsm.sn1");
                if (string.IsNullOrEmpty(serial_number))
                {
                    serial_number = (string)get.Invoke(c, "ril.serialnumber");
                }
                if (string.IsNullOrEmpty(serial_number))
                {
                    serial_number = (string)get.Invoke(c, "ro.serialno");
                }
                if (string.IsNullOrEmpty(serial_number))
                {
                    serial_number = (string)get.Invoke(c, "sys.serialnumber");
                }
                if (string.IsNullOrEmpty(serial_number))
                {
                    serial_number = Build.GetSerial();
                }
            }
            catch (Exception e)
            {
                serial_number = string.Empty;
            }

            return serial_number;
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}