using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using RePlay_Exercises;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReCheck.Droid
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

        /// <summary>
        /// Order of the tuple: Arduino name/description, vendor ID, product ID
        /// </summary>
        private List<Tuple<string, int, int>> RecognizedRePlayArduinoDevices = new List<Tuple<string, int, int>>()
        {
            new Tuple<string, int, int>("Arduino Micro", 0x2341, 0x8037),
            new Tuple<string, int, int>("Arduino Uno", 0x2A03, 0x0043),
            new Tuple<string, int, int>("Arduino Nano 33 BLE", 0x2341, 0x805A)
        };

        #endregion

        #region Public Properties

        public List<UsbDevice> AttachedUSBDevices { get; private set; } = new List<UsbDevice>();
        public List<ExerciseDeviceType> AttachedDeviceTypes { get; set; } = new List<ExerciseDeviceType>();

        #endregion

        #region Singleton Constructor

        public DeviceManager(Activity activity)
        {
            Initialize(activity);
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

        private void Initialize(Activity mainActivity)
        {
            Activity = mainActivity;
            USBReceiver = new UsbReceiver();
        }

        public bool CheckDeviceAttachedAndPermissions(Activity a, string selected)
        {
            if (selected == ExerciseDeviceType.ReCheck.ToString())
            {
                CheckAttachedDevices(a);
                int device_idx = AttachedDeviceTypes.FindIndex(x => x.ToString().Equals(selected));
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

        public bool CheckDeviceAttachedAndPermissions(Activity a, ExerciseDeviceType selected)
        {
            if (selected == ExerciseDeviceType.ReCheck)
            {
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
            if (selected == ExerciseDeviceType.ReCheck)
            {
                CheckAttachedDevices(a);
                int device_idx = AttachedDeviceTypes.FindIndex(x => x.Equals(selected));
                bool is_device_attached = device_idx > -1;
                return is_device_attached;
            }

            return false;
        }

        public void CheckAttachedDevices(Activity a)
        {
            //Clear these lists before we refresh them
            AttachedUSBDevices.Clear();
            AttachedDeviceTypes.Clear();

            //Get the current device list
            USBManager = (UsbManager)a.GetSystemService(Context.UsbService);
            var DeviceList = USBManager.DeviceList;

            foreach (var key in DeviceList.Keys)
            {
                if (DeviceList[key].VendorId == FitMiVendorID && DeviceList[key].ProductId == FitMiProductID)
                {
                    AttachedUSBDevices.Add(DeviceList[key]);
                    AttachedDeviceTypes.Add(ExerciseDeviceType.FitMi);
                }
                else
                {
                    foreach (var recognized_replay_device in RecognizedRePlayArduinoDevices)
                    {
                        var replay_vendor_id = recognized_replay_device.Item2;
                        var replay_device_id = recognized_replay_device.Item3;
                        if (DeviceList[key].VendorId == replay_vendor_id && DeviceList[key].ProductId == replay_device_id)
                        {
                            AttachedUSBDevices.Add(DeviceList[key]);
                            AttachedDeviceTypes.Add(ExerciseDeviceType.ReCheck);
                            break;
                        }
                    }
                }
            }
        }

        public string GetDeviceMessage(string selected)
        {
            if (selected == ExerciseDeviceType.ReCheck.ToString())
            {
                return "Please ensure that your Replay device is plugged in and permissions have been granted.";
            }

            return "Please ensure your device is connected!";
        }

        public string GetDeviceInstruction(ExerciseDeviceType device, ExerciseType exercise)
        {
            switch (device)
            {
                case ExerciseDeviceType.ReCheck:
                    return "Set up your RePlay device!";
                default:
                    return "Make sure this device is connected";
            }
        }

        #endregion

    }
}