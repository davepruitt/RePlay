using System;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using RePlay_Exercises;
using RePlay_VNS_Triggering;
using RePlay_Activity_Common;
using System.Collections.Generic;
using RePlay_Common;
using System.Linq;

namespace RePlay_Activity_TrafficRacer.Input
{

    static class InputManager
	{

		static KeyboardState PreviousKeyState;
		static KeyboardState CurrentKeyState;

		static MouseState PreviousMouseState;
		static MouseState CurrentMouseState;

        public static ExerciseBase Exercise;

        public static VNSAlgorithm_Standard VNS;
        private static PCM_Manager PCM;

        private static bool is_replay_debug_mode = false;
        private static float lateral_movement = 0;
        private static List<double> debounce_list = new List<double>();
        private static int debounce_size = 10;

        public static float LateralMovement
		{
			get
			{
                return lateral_movement;
			}
		}

		public static bool ZoomOut
		{
			get
			{
				return PreviousKeyState.IsKeyUp(Keys.OemOpenBrackets) && CurrentKeyState.IsKeyDown(Keys.OemOpenBrackets);
			}
		}

		public static bool ZoomIn
		{
			get
			{
				return PreviousKeyState.IsKeyUp(Keys.OemCloseBrackets) && CurrentKeyState.IsKeyDown(Keys.OemCloseBrackets);
			}
		}

		public static Vector2 MoveCameraAmount
		{
			get
			{
				if (Mouse.GetState().MiddleButton == ButtonState.Released)
				{
					return Vector2.Zero;
				}
				Vector2 dir = new Vector2(PreviousMouseState.X - CurrentMouseState.X, PreviousMouseState.Y - CurrentMouseState.Y);

				return dir;
			}
		}

		public static bool Restart
		{
			get
			{
                return false;
            }
		}

		public static bool ToggleDebug
		{
			get
			{
                return false;
			}
		}

		public static bool Quit
		{
			get
			{
				return CurrentKeyState.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
			}
		}

        public static bool Enter
        {
            get
            {
                return CurrentKeyState.IsKeyDown(Keys.Enter);
            }
        }

        public static void Initialize(PCM_Manager pcm, ExerciseDeviceType device, ExerciseType exercise, 
            string tablet, string subject, double gain, bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters, bool debug_mode)
		{
            is_replay_debug_mode = debug_mode;
            Exercise = ExerciseBase.InstantiateCorrectExerciseClass(exercise, Game.Activity, gain);
            bool ready = Exercise.SetupDevice();
            if (!ready) throw new Exception("Device could not be setup!");

            ready = false;
            var start = DateTime.Now;
            while (!ready || (DateTime.Now - start).TotalMilliseconds < 2000)
            {
                ready = Exercise.ResetExercise();
            }

			PCM = pcm;
			VNS = new VNSAlgorithm_Standard();
            VNS.Initialize_VNS_Algorithm(DateTime.Now, vns_algorithm_parameters);
            PCM.PropertyChanged += (b, c) =>
            {
                //empty
            };

            PCM.PCM_Event += (a, b) =>
            {
                try
                {
                    Exercise_SaveData.SaveMessageFromReStoreService(Exercise.DataSaver, b);
                }
                catch (Exception)
                {
                    //empty
                }
            };

            //Get build information
            var build_date = RePlay_Game_BuildInformationManager.GetBuildDate(Game.Activity);
            var version_name = RePlay_Game_BuildInformationManager.GetVersionName();
            var version_code = RePlay_Game_BuildInformationManager.GetVersionCode();

            Exercise.SetupFile(build_date, version_name, version_code,
                "TrafficRacer", ExerciseTypeConverter.ConvertExerciseTypeToEnumMemberString(exercise), 
                tablet, subject, from_prescription, vns_algorithm_parameters);
        }

        public static void Close()
        {
            Exercise.CloseFile();
            Exercise.Close();
        }

		public static void Update(RePlay_Game_GameplayUI gameplay_ui)
		{
            //Get the keyboard and mouse state
			PreviousKeyState = CurrentKeyState;
			CurrentKeyState = Keyboard.GetState();

			PreviousMouseState = CurrentMouseState;
			CurrentMouseState = Mouse.GetState();

            //Get the latest device exercise data
			Exercise.Update();

            //Save the device exercise data to the data file
            Exercise.SaveExerciseData();

            //Transform the device exercise data to get the total lateral movement
            lateral_movement = -(float)Exercise.CurrentNormalizedValue;

            //Determine whether or not to stimulate
            bool stim = VNS.Determine_VNS_Triggering(DateTime.Now, lateral_movement);
            if (stim)
            {
                gameplay_ui.DisplayStimulationIcon(VNS.Parameters.Enabled, TimeSpan.FromSeconds(2.0));
                Exercise_SaveData.SaveStimulationTriggerAtCurrentTime(Exercise.DataSaver);
                if (VNS.Parameters.Enabled)
                {
                    PCM.QuickStim();
                }
            }

            //Plot signal data
            if (is_replay_debug_mode)
            {
                var game_activity = Game.Activity as RePlay_Game_Activity;
                if (game_activity != null)
                {
                    game_activity.game_signal_chart.AddDataPoint(lateral_movement);
                    game_activity.vns_signal_chart.AddDataPoint(
                        VNS.Plotting_Get_Latest_Calculated_Value(),
                        VNS.Plotting_Get_VNS_Positive_Threshold(),
                        VNS.Plotting_Get_VNS_Negative_Threshold()
                        );
                }
            }
        }

        public static bool ReconnectDevice ()
        {
            bool success = Exercise.SetupDevice();
            return success;
        }
	}
}
