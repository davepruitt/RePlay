using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Views;
using System;
using System.Timers;
using Android.Text.Method;
using Android.Views.InputMethods;
using RePlay_VNS_Triggering;
using System.IO;
using System.Text;
using FitMiAndroid;
using System.ComponentModel;
using Java.Lang;
using System.Collections.Generic;
using System.Linq;
using RePlay_Common;
using RePlay_Exercises;
using Newtonsoft.Json;

namespace RePlay_Activity_TherapistManualMode
{
    [Activity(Label = "Therapist Guided Exercise", WindowSoftInputMode = SoftInput.StateAlwaysHidden)]
    public class MainActivity : Activity
    {
        private HIDPuckDongle puck_dongle;
        private BackgroundWorker background_thread = new BackgroundWorker();

        private TimeSpan min_stimulation_interval = TimeSpan.FromSeconds(8.0);
        private TimeSpan min_scroll_interval = TimeSpan.FromSeconds(1.0);
        private DateTime last_scroll_event = DateTime.MinValue;
        private DateTime first_stim_time = DateTime.MinValue;
        private DateTime last_successful_stim_event = DateTime.MinValue;
        private int stim_counter = 0;
        private int successful_stim_counter = 0;
        private FrameLayout MainLayout;

        private Timer timer_since_first_stim = null;
        private PCM_Manager PCM = null;
        private StreamWriter therapist_mode_file = null;
        private int pcm_connection_icon_tristate = -1;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            StartImmersiveMode();

            //Grab the reps mode layout and add it as a child view to our main layout frame
            View therapistModeLayout = LayoutInflater.Inflate(Resources.GetIdentifier("activity_main", "layout", Application.Context.PackageName), null);
            MainLayout = new FrameLayout(this.ApplicationContext);
            MainLayout.AddView(therapistModeLayout);

            // Set our view from the "main" layout resource
            SetContentView(MainLayout);

            var therapist_mode_quit_button = FindViewById<Button>(Resources.GetIdentifier("therapist_mode_quit_button", "id", Application.Context.PackageName));
            therapist_mode_quit_button.Click += HandleQuitButtonClick;

            var stim_counter_text_view = FindViewById<TextView>(Resources.GetIdentifier("stim_counter_text_view", "id", Application.Context.PackageName));
            stim_counter_text_view.Text = stim_counter.ToString();

            var successful_stim_counter_text_view = FindViewById<TextView>(Resources.GetIdentifier("successful_stim_counter_text_view", "id", Application.Context.PackageName));
            successful_stim_counter_text_view.Text = successful_stim_counter.ToString();

            var timer_text_view = FindViewById<TextView>(Resources.GetIdentifier("timer_text_view", "id", Application.Context.PackageName));
            timer_text_view.Text = "00:00:00";

            var note_entry_button = FindViewById<Button>(Resources.GetIdentifier("note_entry_button", "id", Application.Context.PackageName));
            note_entry_button.Click += HandleNoteEntryButtonClick;

            var new_note_edit_text = FindViewById<EditText>(Resources.GetIdentifier("new_note_edit_text", "id", Application.Context.PackageName));
            new_note_edit_text.EditorAction += HandleEditorAction;

            var all_notes_text_view = FindViewById<TextView>(Resources.GetIdentifier("all_notes_text_view", "id", Application.Context.PackageName));
            all_notes_text_view.MovementMethod = new ScrollingMovementMethod();
            all_notes_text_view.FocusChange += HandleFocusChange;

            MainLayout.GenericMotion += HandleGenericMotionEvent;

            var stim_button = FindViewById<Button>(Resources.GetIdentifier("manual_stim_button", "id", Application.Context.PackageName));

            PCM = new PCM_Manager(this);
            PCM.PropertyChanged += (b, c) =>
            {
                //Update the PCM connection status icon
                int pcm_status = PCM.IsConnectedToPCM ? 1 : 0;
                if (pcm_status != pcm_connection_icon_tristate)
                {
                    pcm_connection_icon_tristate = pcm_status;

                    var pcm_connection_status_image = FindViewById<ImageView>(Resources.GetIdentifier("tmode_mode_pcm_connection_status_icon", "id", Application.Context.PackageName));
                    if (pcm_connection_status_image != null)
                    {
                        string img_name = PCM.IsConnectedToPCM ? "tmode_pcm_connected" : "tmode_pcm_disconnected";
                        int res_image = Resources.GetIdentifier(img_name, "drawable", Application.Context.PackageName);
                        pcm_connection_status_image.SetImageResource(res_image);
                    }
                }

                if (PCM.CurrentStimulationTimeoutPeriod_SafeToUse != TimeSpan.Zero)
                {
                    min_stimulation_interval = PCM.CurrentStimulationTimeoutPeriod_SafeToUse;
                }
            };

            PCM.PCM_Event += (b, c) =>
            {
                //Check to see if the PCM event is notifying us of a successful stimulation
                if (c.SecondaryMessages.ContainsKey("COMMAND_STATUS"))
                {
                    if (c.SecondaryMessages["COMMAND_STATUS"].Equals("STIM_SUCCESS"))
                    {
                        //If this indicates a successful stimulation, then increment the number of successful
                        //stims in this app, and also update the textview in the UI to reflect that.
                        successful_stim_counter++;
                        last_successful_stim_event = DateTime.Now;
                        successful_stim_counter_text_view.Text = successful_stim_counter.ToString();
                    }
                }

                //We write ALL events that are returned to us by the ReStore app to a datafile.
                try
                {
                    WritePCMEventToFile(therapist_mode_file, c);
                }
                catch (System.Exception)
                {
                    //empty
                }
            };

            stim_button.Click += (b, c) =>
            {
                SendStimulationRequest();
            };

            //Set up the background thread to fetch puck data
            background_thread.WorkerReportsProgress = true;
            background_thread.WorkerSupportsCancellation = true;
            background_thread.DoWork += Background_thread_DoWork;
            background_thread.ProgressChanged += Background_thread_ProgressChanged;
            background_thread.RunWorkerCompleted += Background_thread_RunWorkerCompleted;
            background_thread.RunWorkerAsync();


            string game_launch_params_json = Intent.GetStringExtra("game_launch_parameters_json");
            GameLaunchParameters game_launch_parameters = new GameLaunchParameters();
            try
            {
                game_launch_parameters = JsonConvert.DeserializeObject<GameLaunchParameters>(game_launch_params_json);
            }
            catch (System.Exception ex)
            {
                //empty
            }

            string subject_id = game_launch_parameters.SubjectID;
            therapist_mode_file = OpenFileForSaving(subject_id);
        }

        #region Background thread stuff

        private List<int> mins_per_second_blue = new List<int>();
        private List<int> mins_per_second_yellow = new List<int>();
        private List<int> all_values_prev_second_blue = new List<int>();
        private List<int> all_values_prev_second_yellow = new List<int>();
        

        private int min_blue_val = 1000;
        private int min_yellow_val = 1000;
        private int puck_touch_threshold = 30;

        private DateTime last_blue_touch = DateTime.MinValue;
        private DateTime last_yellow_touch = DateTime.MinValue;
        private TimeSpan min_inter_touch_interval = TimeSpan.FromSeconds(1.0);
        private bool blue_puck_touch = false;
        private bool yellow_puck_touch = false;
        
        

        private void Background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Close the connection to the FitMi pucks
            puck_dongle.Close();
        }

        private void Background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                //empty
            }
            else if (e.ProgressPercentage == 1)
            {
                //Send a stimulation request
                SendStimulationRequest();

                //The yellow puck was touched
                EnterNewNote("A stimulation request was issued by touching the YELLOW puck.");
            }
        }

        private void Background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Open a connection to the FitMi pucks
            puck_dongle = new HIDPuckDongle(this);
            puck_dongle.Open();

            while (!background_thread.CancellationPending)
            {
                //Check for new data from the pucks
                puck_dongle.CheckForNewPuckData();

                var cur_blue_val = puck_dongle.PuckPack0.Loadcell;
                all_values_prev_second_blue.Add(cur_blue_val);
                if (all_values_prev_second_blue.Count >= 30)
                {
                    mins_per_second_blue.Add(all_values_prev_second_blue.Min());
                    all_values_prev_second_blue.Clear();

                    if (mins_per_second_blue.Count > 10)
                    {
                        mins_per_second_blue.RemoveAt(0);
                    }

                    min_blue_val = TxBDC_Math.Median(mins_per_second_blue);
                }

                var cur_yellow_val = puck_dongle.PuckPack1.Loadcell;
                all_values_prev_second_yellow.Add(cur_yellow_val);
                if (all_values_prev_second_yellow.Count >= 30)
                {
                    mins_per_second_yellow.Add(all_values_prev_second_yellow.Min());
                    all_values_prev_second_yellow.Clear();

                    if (mins_per_second_yellow.Count > 10)
                    {
                        mins_per_second_yellow.RemoveAt(0);
                    }

                    min_yellow_val = TxBDC_Math.Median(mins_per_second_yellow);
                }

                //Code to handle the "blue_puck_touch" state
                if (cur_blue_val >= (min_blue_val + puck_touch_threshold))
                {
                    if (!blue_puck_touch && (DateTime.Now >= (last_blue_touch + min_inter_touch_interval)))
                    {
                        blue_puck_touch = true;
                        last_blue_touch = DateTime.Now;
                        background_thread.ReportProgress(0);
                    }
                }
                else
                {
                    blue_puck_touch = false;
                }


                //Code to handle the "yellow_puck_touch" state
                if (cur_yellow_val >= (min_yellow_val + puck_touch_threshold))
                {
                    if (!yellow_puck_touch && (DateTime.Now >= (last_yellow_touch + min_inter_touch_interval)))
                    {
                        yellow_puck_touch = true;
                        last_yellow_touch = DateTime.Now;
                        background_thread.ReportProgress(1);
                    }
                }
                else
                {
                    yellow_puck_touch = false;
                }

                //Sleep the thread for a bit.
                Thread.Sleep(33);
            }
        }

        #endregion

        private void QuitTherapistMode ()
        {
            //Close down the background thread
            if (background_thread != null && background_thread.IsBusy)
            {
                background_thread.CancelAsync();
            }

            //Close the file we are saving stuff to
            CloseFile(therapist_mode_file);

            //Close the activity
            this.Finish();
        }

        private void HandleQuitButtonClick(object sender, EventArgs e)
        {
            QuitTherapistMode();
        }

        public override void OnBackPressed()
        {
            QuitTherapistMode();
        }

        private void HandleFocusChange(object sender, View.FocusChangeEventArgs e)
        {
            var all_notes_text_view = FindViewById<TextView>(Resources.GetIdentifier("all_notes_text_view", "id", Application.Context.PackageName));
            all_notes_text_view.RequestFocus();
        }

        private void SendStimulationRequest ()
        {
            //Increment the number of trigger requests
            stim_counter++;

            //Save the current datetime as the most recent trigger request
            last_scroll_event = DateTime.Now;

            //Update the stimulation requests textview to have the new count of trigger requests
            var stim_counter_text_view = FindViewById<TextView>(Resources.GetIdentifier("stim_counter_text_view", "id", Application.Context.PackageName));
            stim_counter_text_view.Text = stim_counter.ToString();

            //Send a trigger request to the PCM
            PCM.QuickStim();

            //If this is the first trigger requests, start a timer to keep track of the total time elapsed
            //since the first trigger request
            if (timer_since_first_stim == null)
            {
                first_stim_time = DateTime.Now;

                timer_since_first_stim = new Timer(1000);
                timer_since_first_stim.Elapsed += HandleTimerEvent;
                timer_since_first_stim.AutoReset = true;
                timer_since_first_stim.Enabled = true;
                timer_since_first_stim.Start();
            }
        }

        private void EnterNewNote ()
        {
            var new_note_edit_text = FindViewById<EditText>(Resources.GetIdentifier("new_note_edit_text", "id", Application.Context.PackageName));
            var all_notes_text_view = FindViewById<TextView>(Resources.GetIdentifier("all_notes_text_view", "id", Application.Context.PackageName));

            string new_note = new_note_edit_text.Text;
            if (!string.IsNullOrEmpty(new_note))
            {
                WriteNoteToFile(therapist_mode_file, new_note);
                
                new_note_edit_text.Text = string.Empty;

                if (!string.IsNullOrEmpty(all_notes_text_view.Text))
                {
                    all_notes_text_view.Text += "\n";
                }
                all_notes_text_view.Text += "(" + DateTime.Now.ToString("h:mm tt") + ") " + new_note;
            }
        }

        private void EnterNewNote (string text)
        {
            var all_notes_text_view = FindViewById<TextView>(Resources.GetIdentifier("all_notes_text_view", "id", Application.Context.PackageName));

            string new_note = text;
            if (!string.IsNullOrEmpty(new_note))
            {
                WriteNoteToFile(therapist_mode_file, new_note);

                if (!string.IsNullOrEmpty(all_notes_text_view.Text))
                {
                    all_notes_text_view.Text += "\n";
                }
                all_notes_text_view.Text += "(" + DateTime.Now.ToString("h:mm tt") + ") " + new_note;
            }
        }

        private void HandleEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            EnterNewNote();
        }

        private void HandleNoteEntryButtonClick(object sender, EventArgs e)
        {
            EnterNewNote();
        }

        private void HandleGenericMotionEvent(object sender, View.GenericMotionEventArgs e)
        {
            if (DateTime.Now >= (last_scroll_event + min_scroll_interval))
            {
                if (e != null && e.Event != null)
                {
                    var motionEvent = e.Event;
                    if (motionEvent.Source != InputSourceType.ClassNone && (motionEvent.Source & InputSourceType.ClassPointer) > 0)
                    {
                        switch (motionEvent.Action)
                        {
                            case MotionEventActions.Scroll:

                                //SendStimulationRequest();
                                
                                break;
                        }
                    }
                }
            }
        }

        private void HandleTimerEvent(object sender, ElapsedEventArgs e)
        {
            this.RunOnUiThread(() =>
            {
                string timer_text = (DateTime.Now - first_stim_time).ToString(@"hh\:mm\:ss");

                var timer_text_view = FindViewById<TextView>(Resources.GetIdentifier("timer_text_view", "id", Application.Context.PackageName));
                timer_text_view.Text = timer_text;
            });
        }

        protected void StartImmersiveMode()
        {
            View decorView = Window.DecorView;
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            var uiOptions = (int)decorView.SystemUiVisibility;
            var newUiOptions = (int)uiOptions;

            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.Immersive;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        }

        public StreamWriter OpenFileForSaving(string subject_id)
        {
            //Initialize the save file
            string current_date_time_stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string file_name = "TherapistMode_" + current_date_time_stamp + ".txt";

            string date_string = DateTime.Now.ToString("yyyy_MM_dd");

            string external_file_storage = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            string replay_path = Path.Combine(external_file_storage, "TxBDC");
            replay_path = Path.Combine(replay_path, subject_id);
            replay_path = Path.Combine(replay_path, date_string);
            replay_path = Path.Combine(replay_path, "TherapistMode");
            
            string file_path = Path.Combine(replay_path, file_name);
            
            //Create the folder if it does not exist
            new FileInfo(file_path).Directory.Create();

            //Open a handle to be able to write to the file
            var f_stream = new FileStream(file_path, FileMode.Create);
            StreamWriter result = new StreamWriter(f_stream);
            
            //Return the file handle
            return result;
        }

        public void WriteNoteToFile (StreamWriter writer, string note)
        {
            writer.Write("(" + DateTime.Now.ToString() + ") ");
            writer.WriteLine(note);
            writer.Flush();
        }

        public void WritePCMEventToFile (StreamWriter writer, PCM_DebugModeEvent_EventArgs msg)
        {
            string result = string.Empty;
            result += msg.PrimaryMessage;
            if (msg.SecondaryMessages.Count > 0)
            {
                result += " (";
                int i = 0;
                int key_count = msg.SecondaryMessages.Count;
                foreach (var kvp in msg.SecondaryMessages)
                {
                    result += kvp.Key + " = " + kvp.Value;
                    if (i < (key_count - 1))
                    {
                        result += ", ";
                    }
                }
                result += ")";
            }

            writer.Write("(" + msg.MessageTimestamp.ToString() + ") ");
            writer.WriteLine(result);
            writer.Flush();
        }

        public void CloseFile (StreamWriter writer)
        {
            writer.Close();
        }
    }
}