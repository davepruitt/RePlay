using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RePlay_Common;

namespace RePlay_VNS_Triggering
{
    /// <summary>
    /// This class handles overall communication with the ReStore service and PCM.
    /// </summary>
    public class PCM_Manager : NotifyPropertyChangedObject
    {
        #region Event handler for debug mode, to notify of various PCM events

        public event EventHandler<PCM_DebugModeEvent_EventArgs> PCM_Event;

        protected void NotifyOfPCMEvent (DateTime timestamp, string primary_message, Dictionary<string, string> secondary_messages)
        {
            PCM_Event?.Invoke(this, new PCM_DebugModeEvent_EventArgs()
            {
                MessageTimestamp = timestamp,
                PrimaryMessage = primary_message,
                SecondaryMessages = secondary_messages
            });
        }

        #endregion

        #region Private handler class that deals with incoming messages

        /// <summary>
        /// This class handles incoming messages from the ReStore service
        /// </summary>
        private class IncomingHandler : Handler
        {
            #region Private members

            PCM_Manager pcm_manager;

            #endregion

            #region Constructor

            public IncomingHandler (PCM_Manager mgr)
                : base()
            {
                pcm_manager = mgr;
            }

            #endregion

            #region The function that handles messages returned from the PCM

            public override void HandleMessage(Message msg)
            {
                Bundle data = msg.Data;
                
                string command_status = data.GetString(command_status_parameter, string.Empty);
                string specific_error = data.GetString(specific_error_parameter, string.Empty);
                string error_description = data.GetString(error_description_parameter, string.Empty);
                string settings = data.GetString(settings_parameter, string.Empty);

                //Parse the settings JSON that is returned to us from the PCM
                var dictionary_of_status = pcm_manager.ParsePCMSettingsJSONAndSetVariables(settings);
                DateTime return_message_timestamp = DateTime.Now;
                pcm_manager.TimeOfLastStatusCheck = return_message_timestamp;

                if (command_status.Equals(stimulation_success_result))
                {
                    //In this case, we attempted to stimulate and we succeeded
                    pcm_manager.IsConnectedToPCM = true;
                }
                else if (command_status.Equals(stimulation_failure_result))
                {
                    //In this case, we attempted to stimulate but failed
                }
                else if (command_status.Equals(system_self_test_success_result))
                {
                    //The PCM has returned a "success" status after we did a "check status" command
                    pcm_manager.IsConnectedToPCM = true;
                }
                else if (command_status.Equals(system_self_test_failure_result))
                {
                    //The PCM has returned a "failed" status after we did a "check status" command
                    pcm_manager.IsConnectedToPCM = false;
                }

                //Notify any listeners that there has been a status update
                pcm_manager.NotifyPropertyChanged("TimeOfLastStatusCheck");

                //Send out a pcm event to all listeners
                dictionary_of_status[command_status_parameter] = command_status;
                dictionary_of_status[specific_error_parameter] = specific_error;
                dictionary_of_status[error_description_parameter] = error_description;
                pcm_manager.NotifyOfPCMEvent(return_message_timestamp, "Return Message from ReStore", dictionary_of_status);
            }

            #endregion
        }

        #endregion

        #region Private ServiceConnection class implementation to deal with connection and disconnection events

        /// <summary>
        /// This class handles connection/disconnection events with the ReStore service
        /// </summary>
        private class ServiceConnection : Java.Lang.Object, IServiceConnection
        {
            #region Private data members

            private PCM_Manager pcm_manager;

            #endregion

            #region Constructor

            public ServiceConnection(PCM_Manager m)
            {
                pcm_manager = m;
            }

            #endregion

            #region Methods to handle service connection and disconnection

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                Log.Debug("CONNECTED", "ServiceConnection OnServiceConnected");

                //Set a flag indicating we are now connected to the ReStore service
                pcm_manager.IsConnectedToReStoreService = true;

                //Define the outgoing messenger
                pcm_manager.outgoing_message_handler = new Messenger(service);
                
                //Send the "check status" command
                pcm_manager.CheckPCMStatus();
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                Log.Debug("DISCONNECTED", "ServiceConnection OnServiceDisconnected");

                //Unbind the service
                try
                {
                    pcm_manager.current_activity.UnbindService(pcm_manager.mMyServiceConnection);
                }
                catch (Exception)
                {
                    //Nothing needs to happen here.
                }

                //Set a flag indicating we are no longer connected to the ReStore service
                pcm_manager.IsConnectedToReStoreService = false;
            }

            #endregion
        }

        #endregion
        
        #region Private data members
        
        //Define the package name where the ReStore service is located:
        private static string pcm_intent_str = ""; /* This string has been removed for the github release */
        private static string package_str = ""; /* This string has been removed for the github release */
        private static string remote_param_name = "remote";

        //The private key needed to be able to communicate with the PCM properly
        private static string restore_key = ""; /* This key has been removed for the github release */

        //These are names of parameters (keys) that show up in response messages from the PCM:
        private static string command_status_parameter = "COMMAND_STATUS";
        private static string specific_error_parameter = "SPECIFIC_ERROR";
        private static string error_description_parameter = "ERROR_DESCRIPTION";
        private static string settings_parameter = "SETTINGS";

        //These are values (part of a key-value pair) that show up in response messages from the PCM:
        private static string stimulation_success_result = "STIM_SUCCESS";
        private static string stimulation_failure_result = "STIM_FAILURE";
        private static string system_self_test_success_result = "SYSTEM_SELF_TEST_SUCCESS";
        private static string system_self_test_failure_result = "SELF_TEST_FAILURE";
        private static string train_duration_parameter = "TRAIN_DURATION";

        //These are commands/requests that we can send to the PCM:
        private static string quick_stim_event_string = "QUICK_STIM";
        private static string check_status_event_string = "CHECK_STATUS";
        
        //Define some messengers to handle incoming/outgoing messages and a service connection
        private Messenger return_message_handler = null;
        private Messenger outgoing_message_handler = null;
        private ServiceConnection mMyServiceConnection = null;
        
        //Keep tabs on the current activity because it may be needed for some functions
        public Activity current_activity;
        
        #endregion

        #region Private methods

        private Dictionary<string, string> ParsePCMSettingsJSONAndSetVariables (string settings_json)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            JObject response_json = JObject.Parse(settings_json);
            
            //We need to use a "nullable int" here to avoid a crash since casting a possible null value to an int may throw an error.
            var temp_value = (int?)response_json["BATTERY_VALUE"];
            if (temp_value.HasValue)
            {
                CurrentBatteryPercentage = temp_value.Value;
            }
            else
            {
                CurrentBatteryPercentage = 0;
            }
            
            CurrentStimulationAmplitudeString = (string)response_json["AMPLITUDE"];
            CurrentStimulationFrequencyString = (string)response_json["FREQUENCY"];
            CurrentStimulationPulseWidthString = (string)response_json["PULSE_WIDTH"];
            CurrentStimulationTrainDurationString = (string)response_json["TRAIN_DURATION"];
            Current_PCM_Identifier = (string)response_json["PCM_ID"];
            Current_IPG_Identifier = (string)response_json["IPG_ID"];
            if (string.IsNullOrEmpty(Current_IPG_Identifier))
            {
                Current_IPG_Identifier = string.Empty;
            }

            string[] train_duration_parts = CurrentStimulationTrainDurationString.Trim().Split(new char[] { '_' }, 4);
            if (train_duration_parts.Length == 4)
            {
                string ms_str = train_duration_parts[2];
                if (!string.IsNullOrEmpty(ms_str))
                {
                    bool success = Double.TryParse(ms_str, out double train_ms);
                    if (success)
                    {
                        double train_duration_seconds = train_ms / 1000;
                        double timeout_period = Math.Round(train_duration_seconds * 11.0);

                        CurrentStimulationTrainDuration = TimeSpan.FromMilliseconds(train_ms);
                        CurrentStimulationTimeoutPeriod_Actual = TimeSpan.FromSeconds(timeout_period);
                        CurrentStimulationTimeoutPeriod_SafeToUse = CurrentStimulationTimeoutPeriod_Actual + TimeSpan.FromSeconds(1.0);
                    }
                }
            }

            result["AMPLITUDE"] = CurrentStimulationAmplitudeString;
            result["FREQUENCY"] = CurrentStimulationFrequencyString;
            result["PULSE_WIDTH"] = CurrentStimulationPulseWidthString;
            result["TRAIN_DURATION"] = CurrentStimulationTrainDurationString;
            result["PCM_ID"] = Current_PCM_Identifier;
            result["IPG_ID"] = Current_IPG_Identifier;
            result["BATTERY_VALUE"] = CurrentBatteryPercentage.ToString();

            return result;
        }

        #endregion

        #region Public data members

        public bool DemoMode { get; private set; } = true;
        
        public int CurrentBatteryPercentage { get; private set; } = 0;
        public string CurrentStimulationAmplitudeString { get; private set; } = string.Empty;
        public string CurrentStimulationFrequencyString { get; private set; } = string.Empty;
        public string CurrentStimulationPulseWidthString { get; private set; } = string.Empty;
        public string CurrentStimulationTrainDurationString { get; private set; } = string.Empty;
        public string Current_PCM_Identifier { get; private set; } = string.Empty;
        public string Current_IPG_Identifier { get; private set; } = string.Empty;

        public bool IsConnectedToReStoreService { get; private set; } = false;
        public bool IsConnectedToPCM { get; private set; } = false;
        public DateTime TimeOfLastStatusCheck { get; private set; } = DateTime.MinValue;

        public TimeSpan CurrentStimulationTrainDuration { get; private set; } = TimeSpan.Zero;
        public TimeSpan CurrentStimulationTimeoutPeriod_Actual { get; private set; } = TimeSpan.Zero;
        public TimeSpan CurrentStimulationTimeoutPeriod_SafeToUse { get; private set; } = TimeSpan.Zero;

        public DateTime MostRecentTriggerAttempt { get; private set; } = DateTime.MinValue;

        #endregion

        #region Public methods

        /// <summary>
        /// Default constructor
        /// </summary>
        public PCM_Manager(Activity a)
        {
            current_activity = a;
            mMyServiceConnection = new ServiceConnection(this);

            ConnectToPCM(a);
        }

        /// <summary>
        /// Object finalizer/destructor.
        /// </summary>
        ~PCM_Manager()
        {
            //We need to attempt to unbind from the ReStore service when this object is finalized.
            try
            {
                current_activity.UnbindService(mMyServiceConnection);
            }
            catch (Exception)
            {
                //No need to do anything here.
            }
        }

        /// <summary>
        /// Connects to the ReStore service and PCM
        /// </summary>
        public void ConnectToPCM (Activity activ)
        {
            Intent intent = new Intent(pcm_intent_str);
            intent.SetPackage(package_str);
            intent.PutExtra(remote_param_name, remote_param_name);
            bool bind_result = activ.BindService(intent, mMyServiceConnection, Bind.AutoCreate);
            Handler msgHandler = new IncomingHandler(this);
            return_message_handler = new Messenger(msgHandler);
        }

        /// <summary>
        /// Requests the ReStore service to respond regarding the current status of the PCM and IPG
        /// </summary>
        public void CheckPCMStatus ()
        {
            Message message = new Message();
            Bundle bdl = new Bundle();
            bdl.PutString("event", check_status_event_string);
            bdl.PutString("key", restore_key);
            message.ReplyTo = return_message_handler;
            message.Data = bdl;

            try
            {
                outgoing_message_handler.Send(message);
            }
            catch (Exception e)
            {
                Log.Debug("SYSTEM SELF TEST ERROR", e.StackTrace);
            }

            NotifyOfPCMEvent(DateTime.Now, "Sent PCM status check request", new Dictionary<string, string>());
        }

        /// <summary>
        /// Issues a command to the ReStore service to trigger a stimulation
        /// </summary>
        public void QuickStim ()
        {
            //Check to see if the timeout period has passed since the most recent stimulation attempt
            if (DateTime.Now >= (MostRecentTriggerAttempt + CurrentStimulationTimeoutPeriod_SafeToUse))
            {
                //Check to see if we are connected to the PC
                if (IsConnectedToPCM)
                {
                    //Trigger the PCM
                    Message message = new Message();
                    Bundle bdl = new Bundle();
                    bdl.PutString("event", quick_stim_event_string);
                    bdl.PutString("key", restore_key);
                    message.ReplyTo = return_message_handler;
                    message.Data = bdl;

                    try
                    {
                        outgoing_message_handler.Send(message);
                        MostRecentTriggerAttempt = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        Log.Debug("QUICKSTIM ERROR", e.StackTrace);
                    }

                    NotifyOfPCMEvent(DateTime.Now, "Sent stim trigger request", new Dictionary<string, string>());
                }
                else if (DemoMode)
                {
                    //If the PCM is not connected, but DemoMode is turned on...

                    //If "DemoMode" is set to true, then we call "CheckPCStatus" if the PCM is not already connected.
                    //The reason is: Calling CheckPCMStatus will do 2 things: (1) attempt to restore the connection to the PCM, and 
                    //(2) if there is no IPG sitting on the PCM, it will cause the PCM to beep, which is nice for demoing the system
                    //to other people. The one downside of keeping doing this is that it may possibly suck up the PCM's battery, so 
                    //we can turn off this feature by setting "DemoMode" to false.
                    CheckPCMStatus();
                }
            }
        }

        #endregion
    }
}