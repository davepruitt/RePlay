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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RePlay.Entity;
using RePlay_Exercises;
using RePlay_VNS_Triggering;

namespace RePlay.Manager
{
    public class GameLaunchManager
    {
        public static int REQUEST_CODE = 5432;

        public static bool LaunchGame (Activity CallerActivity, PrescriptionItem game_parameters, VNSAlgorithmParameters parent_vns_parameters, bool from_prescription)
        {
            //Let's put some objects/variables into a more easily usable form for the purposes
            //of this method
            RePlayGame selected_game = game_parameters.Game;
            string game_device_str = ExerciseDeviceTypeConverter.ConvertExerciseDeviceTypeToDescription(game_parameters.Device);
            if (game_parameters.Device == ExerciseDeviceType.Touchscreen)
            {
                game_parameters.Exercise = ExerciseType.Touch;
            }

            string game_exercise_str = ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(game_parameters.Exercise);
            string table_id = PreferencesManager.GetTabletID(CallerActivity);
            string retrieve_set_ids = string.Empty;
            if (game_parameters.RetrieveSetIDs != null && game_parameters.RetrieveSetIDs.Count > 0)
            {
                retrieve_set_ids = String.Join(",", game_parameters.RetrieveSetIDs);
            }

            /*
             * THIS NEXT SECTION OF CODE DETERMINES THE VNS ALGORITHM PARAMETERS THAT WILL BE PASSED
             * TO THE GAME WHEN IT IS LAUNCHED.
             */

            //Fetch the default noise floors that have been defined
            double default_noise_floor = 0;
            try
            {
                IDictionary<string, JToken> default_noise_floors = PreferencesManager.ReadNoiseFloorPreferences();
                if (default_noise_floors.ContainsKey(game_exercise_str))
                {
                    //Grab the default noise floor for the exercise that the user is about to play
                    default_noise_floor = (double)default_noise_floors[game_exercise_str];
                }
            }
            catch (Exception)
            {
                //empty
            }
            
            //Copy the "parent" VNS algorith parameters (defined for the whole prescription)
            //to a new object. This new object will be used to store the VNS algorithm parameters
            //for the game we are about to launch.
            VNSAlgorithmParameters vns_algo_params = parent_vns_parameters.CopyObject();

            //If the "NoiseFloor" define in the parent VNS algorithm parameters is "NaN", then
            //let's grab the default noise floor for the chosen exercise, and use that as our noise floor.
            if (double.IsNaN(vns_algo_params.NoiseFloor))
            {
                vns_algo_params.NoiseFloor = default_noise_floor;
            }

            //Now let's apply any special VNS parameters that are defined for this game only.
            vns_algo_params.ApplyJson(game_parameters.VNS);

            //Now convert the vns algorithm parameters to json to pass them to the launched game
            string vns_algorithm_parameters_json = string.Empty;
            if (parent_vns_parameters != null)
            {
                vns_algorithm_parameters_json = JsonConvert.SerializeObject(vns_algo_params);
            }

            /*
             * END OF SECTION OF CODE THAT DETERMINES VNS ALGORITHM PARAMETERS
             */

            //Let's also get information about the participant
            var participant = PatientLoader.Load(CallerActivity.Assets);

            //Grab the video resources for this game
            int video_resource = ExerciseManager.Instance.MapNameToVideo(game_parameters.Exercise, CallerActivity);

            //Now let's create an intent to launch the game we want to play
            Type t = Type.GetType(selected_game.AssemblyQualifiedName);
            Intent intent = null;
            if (selected_game.IsExternalApplication)
            {
                //If the game is actually an external application, then we need to grab the launch intent
                //for that application package
                intent = CallerActivity.PackageManager.GetLaunchIntentForPackage(selected_game.AssemblyQualifiedName);

                if (intent != null)
                {
                    intent.AddCategory("android.intent.category.DEFAULT");
                }
            }
            else
            {
                //Otherwise, just create a new intent
                intent = new Intent(CallerActivity, t);
            }
            
            //Make sure intent is not null before continuing
            //It could be null if the game is an external application, but the external application doesn't exist.
            if (intent != null)
            {
                //THE FOLLOWING CODE EXISTS ONLY FOR BACKWARD COMPATIBILITY
                //Now let's fill the intent with parameters
                intent.PutExtra("CONTENT_DIR", selected_game.InternalName);
                intent.PutExtra("device", game_device_str);
                intent.PutExtra("exercise", game_exercise_str);
                intent.PutExtra("duration", game_parameters.Duration);
                intent.PutExtra("tabletID", table_id);
                intent.PutExtra("gain", game_parameters.Gain);
                intent.PutExtra("difficulty", game_parameters.Difficulty);
                intent.PutExtra("subjectID", participant.SubjectID);
                intent.PutExtra("projectID", PreferencesManager.ProjectName);
                intent.PutExtra("siteID", PreferencesManager.SiteName);
                intent.PutExtra("showPCMConnectionStatus", PreferencesManager.ShowPCMConnectionInGames);
                intent.PutExtra("showStimulationRequests", PreferencesManager.ShowStimulationRequestsInGames);
                intent.PutExtra("debugMode", PreferencesManager.DebugMode);
                intent.PutExtra("fromPrescription", from_prescription);
                intent.PutExtra("vnsParameters", vns_algorithm_parameters_json);

                //These parameters are specific to Repetitions Mode
                intent.PutExtra("continuous", !from_prescription);
                intent.PutExtra("resid", video_resource);

                //These parameters are specific to ReTrieve
                intent.PutExtra("setID", retrieve_set_ids);
                //END OF CODE FOR BACKWARD COMPATIBILITY

                //THE FOLLOWING CODE IS THE NEW METHOD OF PASSING GAME LAUNCH PARAMETERS
                GameLaunchParameters game_launch_parameters = new GameLaunchParameters();
                game_launch_parameters.ContentDirectory = selected_game.InternalName;
                game_launch_parameters.Device = game_parameters.Device;
                game_launch_parameters.Exercise = game_parameters.Exercise;
                game_launch_parameters.Duration = game_parameters.Duration;
                game_launch_parameters.TabletID = table_id;
                game_launch_parameters.Gain = game_parameters.Gain;
                game_launch_parameters.Difficulty = game_parameters.Difficulty;
                game_launch_parameters.SubjectID = participant.SubjectID;
                game_launch_parameters.ProjectID = PreferencesManager.ProjectName;
                game_launch_parameters.SiteID = PreferencesManager.SiteName;
                game_launch_parameters.ShowPCMConnectionStatus = PreferencesManager.ShowPCMConnectionInGames;
                game_launch_parameters.ShowStimulationRequests = PreferencesManager.ShowStimulationRequestsInGames;
                game_launch_parameters.DebugMode = PreferencesManager.DebugMode;
                game_launch_parameters.LaunchedFromPrescription = from_prescription;
                game_launch_parameters.VNS_AlgorithmParameters = vns_algo_params;
                game_launch_parameters.Continuous = !from_prescription;
                game_launch_parameters.VideoResourceID = video_resource;
                game_launch_parameters.RetrieveSetIDs = game_parameters.RetrieveSetIDs;

                string game_launch_params_json = JsonConvert.SerializeObject(game_launch_parameters);
                intent.PutExtra("game_launch_parameters_json", game_launch_params_json);
                //END OF GAME LAUNCH PARAMETERS

                //Launch the game
                if (selected_game.IsExternalApplication)
                {
                    CallerActivity.StartActivity(intent);   
                }
                else
                {
                    CallerActivity.StartActivityForResult(intent, REQUEST_CODE);
                }

                return true;
            }
            else
            {
                //In the scenario that the intent was null...
                return false;
            }
        }
    }
}