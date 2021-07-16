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
using RePlay_Exercises;

namespace ReCheck.Droid
{
    [Activity(Label = "ReCheck", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class InitializationActivity : Activity
    {
        private bool waiting_for_permissions_dialog_to_complete = false;
        private DeviceManager device_manager;

        private List<string> device_permissions = new List<string>()
        {
            "recheck.permission.ReCheckDevices"
        };

        private List<string> permissions_needed = new List<string>()
        {
            Manifest.Permission.ReadPhoneState,
            Manifest.Permission.WriteExternalStorage,
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Set the UI for this activity
            SetContentView(Resource.Layout.InitializationLayout);

            //Grab the button in the UI, and define a callback for it
            Button grant_permission_button = FindViewById<Button>(Resource.Id.grant_permission_button);
            grant_permission_button.Click += GrantPermission_HandleButtonClick;

            //Initialize the device manager
            device_manager = new DeviceManager(this);
            device_manager.USBReceiver.ActionUsbPermissionEventHandler += USBReceiver_ActionUsbPermissionEventHandler;
        }

        protected override void OnResume()
        {
            base.OnResume();

            //Request permission for access to storage
            RequestPermissions();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            waiting_for_permissions_dialog_to_complete = false;

            var all_permissions_granted = permissions_needed.All(x => CheckSelfPermission(x) == Permission.Granted);
            if (all_permissions_granted)
            {
                StartReCheck();
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

        private bool RequestReCheckDevicePermissions ()
        {
            if (device_manager.CheckDeviceAttached(this, ExerciseDeviceType.ReCheck))
            {
                //Request permissions to use the RePlay device
                return device_manager.CheckDeviceAttachedAndPermissions(this, ExerciseDeviceType.ReCheck);
            }

            return true;
        }

        private void RequestPermissions()
        {
            bool result = false;

            //Request all device permissions as needed
            for (int i = 0; i < device_permissions.Count; i++)
            {
                if (device_permissions[i] == "recheck.permission.ReCheckDevices")
                {
                    result = RequestReCheckDevicePermissions();
                }
            }

            if (result)
            {
                //Remove our event handler once we reach this point in the code. We don't want it to execute again.
                device_manager.USBReceiver.ActionUsbPermissionEventHandler -= USBReceiver_ActionUsbPermissionEventHandler;

                //Request all other Android permissions
                var all_permissions_granted = permissions_needed.All(x => CheckSelfPermission(x) == Permission.Granted);
                if (all_permissions_granted)
                {
                    StartReCheck();
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
                            }
                        }
                    }
                }
            }
        }

        private void StartReCheck()
        {
            //If all permissions have been granted, then go ahead and launch the main activity
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}