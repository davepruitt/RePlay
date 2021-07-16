using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.CurrentActivity;
using RePlay_Common;
using RePlay_GoogleCommunications;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ReCheck.Droid.Model
{
    /// <summary>
    /// This class defines our "configuration parameters" for ReCheck.
    /// Configuration parameters include the subject ID, device used, etc.
    /// </summary>
    public class ReCheckConfigurationModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private string participant_id = string.Empty;

        #endregion

        #region Constructor

        public ReCheckConfigurationModel ()
        {
            SetProjectForGoogleCommunication(this.ProjectIdentifier);
            SetSiteForGoogleCommunication(this.ProjectSiteIdentifier);

            InitializeGoogleDriveConnection();
        }

        #endregion

        #region Methods

        public void InitializeGoogleDriveConnection ()
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (s, e) =>
            {
                var input = CrossCurrentActivity.Current.Activity.Assets.Open("Replay-5b4318531d17.json");
                RePlay_GoogleCommunications.RePlay_Google.InitializeGoogleDrive(input);
            };

            bg.RunWorkerAsync();
        }

        public void SetProjectForGoogleCommunication (string project_name)
        {
            RePlay_GoogleCommunications.RePlay_Google.SetCurrentProject(project_name);
        }

        public void SetSiteForGoogleCommunication (string site_name)
        {
            RePlay_GoogleCommunications.RePlay_Google.SetCurrentSite(site_name);
        }

        #endregion

        #region Properties

        public string BuildDate
        {
            get
            {
                try
                {

                    return BuildInformationManager.RetrieveBuildDate(CrossCurrentActivity.Current.Activity).ToString();
                }
                catch (Exception)
                {
                    return "UNKNOWN";
                }
            }
        }

        public string SoftwareVersion
        {
            get
            {
                var context = CrossCurrentActivity.Current.Activity.ApplicationContext;
                string version_name = context.PackageManager.GetPackageInfo(context.PackageName, 0).VersionName;

                return version_name;
            }
        }

        public string TabletIdentifier
        {
            get
            {
                try
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
                catch (Exception)
                {
                    return "UNKNOWN";
                }
            }
        }

        public string ProjectIdentifier
        {
            get
            {
                return Preferences.Get("ReCheck_Project_Identifier", "DEVELOPMENT");
            }
            set
            {
                Preferences.Set("ReCheck_Project_Identifier", value);
                SetProjectForGoogleCommunication(value);
                NotifyPropertyChanged("ProjectIdentifier");
            }
        }

        public string ProjectSiteIdentifier
        {
            get
            {
                return Preferences.Get("ReCheck_Site_Identifier", "UNKNOWN");
            }
            set
            {
                Preferences.Set("ReCheck_Site_Identifier", value);
                SetSiteForGoogleCommunication(value);
                NotifyPropertyChanged("ProjectSiteIdentifier");
            }
        }

        public int RepetitionsRequiredForTaskCompletion
        {
            get
            {
                return Preferences.Get("reps_required_for_task_completion", 20);
            }
            set
            {
                Preferences.Set("reps_required_for_task_completion", value);
                NotifyPropertyChanged("RepetitionsRequiredForTaskCompletion");
            }
        }

        public bool AutomaticStimulationEnabled
        {
            get
            {
                return Preferences.Get("automatic_stimulation_enabled", false);
            }
            set
            {
                Preferences.Set("automatic_stimulation_enabled", value);
                NotifyPropertyChanged("AutomaticStimulationEnabled");
            }
        }

        public string ParticipantID
        {
            get
            {
                return participant_id;
            }
            set
            {
                participant_id = Participant.CleanParticipantID(value);
                NotifyPropertyChanged("ParticipantID");
            }
        }

        #endregion
    }
}