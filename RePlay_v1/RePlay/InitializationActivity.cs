using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using RePlay.Manager;
using RePlay_Exercises;

namespace RePlay
{
    [Activity(Label = "RePlay", MainLauncher = true, Icon = "@mipmap/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape, LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
    public class InitializationActivity : Activity
    {
        private bool waiting_for_permissions_dialog_to_complete = false;
        private GoogleConnectionManager googleConnectionManager;

        private List<ExerciseDeviceType> device_permissions_needed = new List<ExerciseDeviceType>()
        {
            ExerciseDeviceType.ReCheck,
            ExerciseDeviceType.FitMi
        };

        private List<string> permissions_needed = new List<string>()
        {
            Manifest.Permission.ReadPhoneState,
            Manifest.Permission.WriteExternalStorage,
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Start app center
            /* The following line has been rendered non-functional for the github release, and commented out */
            //AppCenter.Start("", typeof(Analytics), typeof(Crashes));

            //Initialize Xamarin Essentials
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            //Set the UI for this activity
            SetContentView(Resource.Layout.InitializationPage);

            //Set the default project and site name if necessary
            if (string.IsNullOrEmpty(PreferencesManager.ProjectName))
            {
                var list_of_projects = ProjectListManager.GetProjectNames(this);
                if (list_of_projects != null && list_of_projects.Count > 0)
                {
                    PreferencesManager.ProjectName = list_of_projects.FirstOrDefault();
                }
            }

            if (string.IsNullOrEmpty(PreferencesManager.SiteName))
            {
                PreferencesManager.SiteName = "TxBDC";
            }

            //Create the google connection manager and initiate a connection
            googleConnectionManager = new GoogleConnectionManager();
            googleConnectionManager.InitializeGoogleDriveConnection(this);

            //Set the project and site name for the purposes of Google communication
            googleConnectionManager.SetProjectForGoogleCommunication(PreferencesManager.ProjectName);
            googleConnectionManager.SetSiteForGoogleCommunication(PreferencesManager.SiteName);

            //Grab the button in the UI, and define a callback for it
            Button grant_permission_button = FindViewById<Button>(Resource.Id.grant_permission_button);
            grant_permission_button.Click += GrantPermission_HandleButtonClick;

            //Initialize the device manager
            DeviceManager.Instance.Initialize(this);
            DeviceManager.Instance.USBReceiver.ActionUsbPermissionEventHandler += USBReceiver_ActionUsbPermissionEventHandler;
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Request permission for access to storage
            RequestPermissions();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            waiting_for_permissions_dialog_to_complete = false;

            var all_permissions_granted = permissions_needed.All(x => CheckSelfPermission(x) == Permission.Granted);
            if (all_permissions_granted)
            {
                StartReplay();
            }
        }

        private void GrantPermission_HandleButtonClick(object sender, EventArgs e)
        {
            RequestPermissions();
        }

        private void USBReceiver_ActionUsbPermissionEventHandler(object sender, EventArgs e)
        {
            RequestPermissions();
        }

        private bool RequestDevicePermissions(ExerciseDeviceType t)
        {
            if (DeviceManager.Instance.CheckDeviceAttached(this, t))
            {
                //Request permissions to use the RePlay device
                return DeviceManager.Instance.CheckDeviceAttachedAndPermissions(this, t);
            }

            return true;
        }

        private void RequestPermissions ()
        {
            //Ask for permission for the RePlay/ReCheck and FitMi devices.
            bool result = false;
            for (int i = 0; i < device_permissions_needed.Count; i++)
            {
                result = RequestDevicePermissions(device_permissions_needed[i]);
                if (!result)
                {
                    return;
                }
            }

            //Now ask for Android permissions.
            if (result)
            {
                //Remove the event handler that handles USB permission events because we don't want it called anymore.
                DeviceManager.Instance.USBReceiver.ActionUsbPermissionEventHandler -= USBReceiver_ActionUsbPermissionEventHandler;

                var all_permissions_granted = permissions_needed.All(x => CheckSelfPermission(x) == Permission.Granted);
                if (all_permissions_granted)
                {
                    StartReplay();
                }
                else
                {
                    if (!waiting_for_permissions_dialog_to_complete)
                    {
                        //Request permissions
                        for (int i = 0; i < permissions_needed.Count; i++)
                        {
                            if (CheckSelfPermission(permissions_needed[i]) == Permission.Denied)
                            {
                                waiting_for_permissions_dialog_to_complete = true;
                                RequestPermissions(new string[] { permissions_needed[i] }, 0);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void StartReplay ()
        {
            StupidityManager.CleanTheSlatePlease();
            StupidityManager.HoldThisForMe("google", googleConnectionManager);

            //If all permissions have been granted, then go ahead and launch the main activity
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}