using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content.Res;
using Newtonsoft.Json;
using RePlay_Exercises;

namespace RePlay.Manager
{
    // Exercise Manager - singleton class used to load exercises from file
    public class ExerciseManager
    {
        #region Properties

        private Dictionary<ExerciseType, String> exercises = new Dictionary<ExerciseType, string>();
        private const string exerciseFile = "exercises.txt";

        #endregion

        #region Singleton Methods

        private static ExerciseManager instance;

        /// <summary>
        /// Constructor
        /// </summary>
        private ExerciseManager()
        {
            //empty
        }

        /// <summary>
        /// Returns the singleton instance of the exercise manager, creating one if needed
        /// </summary>
        public static ExerciseManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ExerciseManager();
                }
                return instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Used to load exercises from a the text file exercises.txt
        /// </summary>
        public void LoadExercises(AssetManager assets)
        {
            exercises.Clear();

            using (var reader = new StreamReader(assets.Open("exercise_images.json")))
            {
                string json_string = reader.ReadToEnd();
                var json_object = JsonConvert.DeserializeObject(json_string) as Newtonsoft.Json.Linq.JArray;
                if (json_object != null)
                {
                    foreach (var element in json_object)
                    {
                        var property = element.First as Newtonsoft.Json.Linq.JProperty;
                        if (property != null)
                        {
                            string key = property.Name;
                            var jval = property.Value as Newtonsoft.Json.Linq.JValue;
                            if (jval != null)
                            {
                                string value = jval.Value as string;
                                exercises.Add(ExerciseTypeConverter.ConvertEnumMemberStringToExerciseType(key), value);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Utility method to map the exercise name (as a string) into a resource drawable identifier
        /// </summary>
        public int MapNameToPic(ExerciseType exercise_type, Activity a)
        {
            try
            {
                string picName = exercises[exercise_type].Trim() + "0";
                int resource = a.Resources.GetIdentifier(picName, "drawable", a.PackageName);
                return resource == 0 ? Resource.Drawable.curls0 : resource;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        /// <summary>
        /// Utility method to map the exercise name to its video
        /// </summary>
        public int MapNameToVideo(ExerciseType exercise_type, Activity a)
        {
            try
            {
                return a.Resources.GetIdentifier(exercises[exercise_type], "raw", a.PackageName);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        #endregion
    }
}
