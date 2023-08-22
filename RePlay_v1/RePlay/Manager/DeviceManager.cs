using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using RePlay.Entity;
using RePlay_DeviceCommunications;
using RePlay_Exercises;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RePlay.Manager
{
    public class DeviceManager
    {
        #region UsbReceiver class

        public class UsbReceiver : BroadcastReceiver
        {
            public static string ACTION_USB_PERMISSION = "USB_PERMISSION";
            public event EventHandler ActionUsbPermissionEventHandler;
            
            public UsbReceiver()
                : base()
            {
                //empty
            }

            public override void OnReceive(Context context, Intent intent)
            {
                string action = intent.Action;

                lock (this)
                {
                    UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);

                    if (device != null)
                    {
                        if (ACTION_USB_PERMISSION.Equals(action))
                        {
                            ActionUsbPermissionEventHandler?.Invoke(this, new EventArgs());
                        }
                        else if (UsbManager.ActionUsbDeviceAttached.Equals(action))
                        {
                            //empty
                        }
                        else if (UsbManager.ActionUsbDeviceDetached.Equals(action))
                        {
                            //empty
                        }
                    }

                }
            }
        }

        #endregion

        #region Private Properties

        private Activity Activity;
        private UsbManager USBManager;
        public UsbReceiver USBReceiver;

        private const int FitMiVendorID = 0x04d8;
        private const int FitMiProductID = 0x2742;
        private const int ReplayVendorID = 9025;
        private const int ReplayProductID = 32823;

        #endregion

        #region Public Properties

        public List<UsbDevice> AttachedUSBDevices { get; private set; } = new List<UsbDevice>();
        public List<ExerciseDeviceType> AttachedDeviceTypes { get; set; } = new List<ExerciseDeviceType>();
        
        #endregion

        #region Singleton Constructor

        private static DeviceManager _instance = null;

        public static DeviceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DeviceManager();
                }
                return _instance;
            }
        }

        private DeviceManager()
        {
            //empty
        }

        #endregion

        #region Private methods

        private bool PermissionsGranted(UsbDevice d)
        {
            if (d != null)
            {
                return USBManager.HasPermission(d);
            }
            else return false;
        }

        private void RequestPermissionForDevice(UsbDevice device)
        {
            //Check to see if we have permission to access the USB device
            if (USBManager != null && device != null)
            {
                if (!USBManager.HasPermission(device))
                {
                    try
                    {
                        //Request permission to interact with the USB device
                        PendingIntent pending_intent = PendingIntent.GetBroadcast(Activity, 0,
                            new Android.Content.Intent(UsbReceiver.ACTION_USB_PERMISSION), 0);
                        IntentFilter intent_filter = new IntentFilter(UsbReceiver.ACTION_USB_PERMISSION);
                        Activity.RegisterReceiver(USBReceiver, intent_filter);
                        USBManager.RequestPermission(device, pending_intent);
                    }
                    catch (Exception e)
                    {
                        Crashes.TrackError(e);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void Initialize (Activity mainActivity)
        {
            Activity = mainActivity;
            USBReceiver = new UsbReceiver();
        }

        public bool CheckCorrectReCheckModuleAttached(Activity mainActivity, ExerciseType recheck_exercise)
        {
            //Initialize a variable to hold the result of this function.
            bool result = false;

            //Get the current datetime
            var current_time = DateTime.Now;

            //Instantiate a ReCheck microcontroller object
            ReplayMicrocontroller recheck_microcontroller = new ReplayMicrocontroller(mainActivity);

            //Attempt to connect to the microcontroller
            recheck_microcontroller.Open();

            //Make sure streaming is disabled
            recheck_microcontroller.EnableStreaming(false);

            //If we were able to connect successfully...
            if (recheck_microcontroller.IsSetupComplete())
            {
                bool done = false;
                while (!done)
                {
                    if (recheck_microcontroller.LastCurrentDeviceTypeUpdateTime > current_time)
                    {
                        //Indicate that we are done with this loop
                        done = true;

                        //Check to see if the correct module was connected
                        var connected_recheck_module = recheck_microcontroller.CurrentDeviceType;
                        var required_recheck_module = ExerciseTypeConverter.ConvertExerciseTypeToReCheckModuleType(recheck_exercise);
                        if (connected_recheck_module == required_recheck_module)
                        {
                            result = true;
                        }
                    }
                    else
                    {
                        //Time-out after 5 seconds.
                        if (DateTime.Now > (current_time + TimeSpan.FromSeconds(5.0)))
                        {
                            done = true;
                        }
                    }
                }
            }

            //Close the connection to the ReCheck microcontroller
            recheck_microcontroller.Close();

            //Return the result
            return result;
        }
        
        public bool CheckDeviceAttachedAndPermissions(Activity a, ExerciseDeviceType selected)
        {
            // If we are using touchscreen or retrieve box we're good
            if (selected == ExerciseDeviceType.Touchscreen || selected == ExerciseDeviceType.Box)
            {
                return true;
            }
            else if (selected == ExerciseDeviceType.Keyboard)
            {
                return CheckKeyboardIsConnected(a);
            }
            else if (selected == ExerciseDeviceType.FitMi || selected == ExerciseDeviceType.ReCheck)
            {
                // For replay and fitmi we need to ensure the appropriate device is plugged in and permissions are granted
                CheckAttachedDevices(a);
                int device_idx = AttachedDeviceTypes.FindIndex(x => x.Equals(selected));
                bool is_device_attached = device_idx > -1;
                if (is_device_attached)
                {
                    try
                    {
                        var usb_device = AttachedUSBDevices[device_idx];
                        var permissions_granted = PermissionsGranted(usb_device);

                        if (!permissions_granted)
                        {
                            RequestPermissionForDevice(usb_device);
                        }

                        return (is_device_attached && permissions_granted);
                    }
                    catch (Exception e)
                    {
                        Crashes.TrackError(e);
                        return false;
                    }
                }
            }

            return false;
        }

        public bool CheckDeviceAttached(Activity a, ExerciseDeviceType selected)
        {
            if (selected == ExerciseDeviceType.FitMi || selected == ExerciseDeviceType.ReCheck)
            {
                CheckAttachedDevices(a);
                int device_idx = AttachedDeviceTypes.FindIndex(x => x.Equals(selected));
                bool is_device_attached = device_idx > -1;
                return is_device_attached;
            }
            else
            {
                return true;
            }
        }

        public void CheckAttachedDevices (Activity a)
        {
            //Clear these lists before we refresh them
            AttachedUSBDevices.Clear();
            AttachedDeviceTypes.Clear();

            //Get the current device list
            USBManager = (UsbManager)a.GetSystemService(Context.UsbService);
            var DeviceList = USBManager.DeviceList;

            foreach(var key in DeviceList.Keys)
            {
                if (DeviceList[key].VendorId == FitMiVendorID && DeviceList[key].ProductId == FitMiProductID)
                {
                    AttachedUSBDevices.Add(DeviceList[key]);
                    AttachedDeviceTypes.Add(ExerciseDeviceType.FitMi);
                }
                else if (DeviceList[key].VendorId == ReplayVendorID && DeviceList[key].ProductId == ReplayProductID)
                {
                    AttachedUSBDevices.Add(DeviceList[key]);
                    AttachedDeviceTypes.Add(ExerciseDeviceType.ReCheck);
                }
            }
        }

        public int GetImage(Activity a, ExerciseDeviceType device)
        {
            int resource; 
            switch (device)
            {
                case ExerciseDeviceType.FitMi:
                    resource = Resource.Drawable.FitMi;
                    break;
                case ExerciseDeviceType.ReCheck:
                    resource = Resource.Drawable.replay;
                    break;
                case ExerciseDeviceType.Box:
                    resource = Resource.Drawable.box;
                    break;
                case ExerciseDeviceType.Keyboard:
                    resource = Resource.Drawable.keyboard;
                    break;
                case ExerciseDeviceType.Touchscreen:
                default:
                    resource = Resource.Drawable.touchscreen;
                    break;
            }
            return resource;
        }

        public List<string> FindSupportedGames(string device, Activity a)
        {
            List<string> supported = new List<string>();

            foreach(RePlayGame game in GameManager.Instance.Games)
            {
                string resourceString = device.ToString().ToLower() + "_" + game.ExternalName.Replace(' ', '_').ToLower();
                int res = a.Resources.GetIdentifier(resourceString, "array", a.PackageName);
                if (res != 0) supported.Add(game.ExternalName.ToString());
            }

            return supported;
        }

        public bool CheckKeyboardIsConnected(Activity current_activity)
        {
            return (current_activity.Resources.Configuration.Keyboard == Android.Content.Res.KeyboardType.Qwerty);
        }

        public string GetDeviceMessage(string selected)
        {
            // If we are using touchscreen or retrieve box we're good
            if (selected == ExerciseDeviceType.FitMi.ToString())
                return "Please ensure that your FitMi device is plugged in.";
            else if (selected == ExerciseDeviceType.Keyboard.ToString())
            {
                return "Please ensure that your keyboard usb dongle is plugged in.";
            }
            else if (selected == ExerciseDeviceType.ReCheck.ToString())
            {
                return "Please ensure that your ReCheck device is plugged in.";
            }

            return "Please ensure your device is connected!";
        }

        public string GetDeviceInstruction(ExerciseDeviceType device, ExerciseType exercise)
        {
            switch (device)
            {
                case ExerciseDeviceType.Box:
                    return "Set up your Retrieve system!";
                case ExerciseDeviceType.FitMi:
                    if (ExerciseTypeConverter.IsMultiPuck(exercise)) return "Grab both the FitMi pucks!";
                    else return "Grab only the blue FitMi puck!";
                case ExerciseDeviceType.ReCheck:
                    return "Set up your ReCheck device!";
                case ExerciseDeviceType.Keyboard:
                    return "Grab your keyboard!";
                case ExerciseDeviceType.Touchscreen:
                    return "Get ready to use your touchscreen!";
                default:
                    return "Make sure this device is connected";
            }
        }

        #endregion

    }
}