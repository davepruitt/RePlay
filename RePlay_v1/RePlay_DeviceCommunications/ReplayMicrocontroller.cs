using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RePlay_Common;
using Hoho.Android.UsbSerial;
using Hoho.Android.UsbSerial.Driver;
using Android.Hardware.Usb;
using Android.Widget;
using Android.Content;
using Android.Views;
using Hoho.Android.UsbSerial.Util;
using Android.App;
using Android.Util;
using Hoho.Android.UsbSerial.Extensions;
using System.IO;
using Java.Security;

namespace RePlay_DeviceCommunications
{
    public class ReplayMicrocontroller : NotifyPropertyChangedObject, IDisposable
    {
        #region UsbReceiver subclass

        public Activity MainActivity;
        public UsbManager USB_Manager = null;
        public UsbReceiver USB_Receiver = null;
        public UsbDevice USB_Device = null;
        public UsbSerialPort SerialPort = null;
        public UsbDeviceConnection USB_Device_Connection = null;
        public int VendorID = 0x2341;
        public int ProductID = 0x8037;
        private bool IsSerialPortOpen = false;
        public object _property_lock = new object();

        /// <summary>
        /// Order of the tuple: Arduino name/description, vendor ID, product ID
        /// </summary>
        private List<Tuple<string, int, int>> RecognizedArduinoDevices = new List<Tuple<string, int, int>>()
        {
            new Tuple<string, int, int>("Arduino Micro", 0x2341, 0x8037),
            new Tuple<string, int, int>("Arduino Uno", 0x2A03, 0x0043),
            new Tuple<string, int, int>("Arduino Nano 33 BLE", 0x2341, 0x805A)
        };

        /// <summary>
        /// This class 
        /// </summary>
        public class UsbReceiver : BroadcastReceiver
        {
            public static string ACTION_USB_PERMISSION = "USB_PERMISSION";
            private ReplayMicrocontroller replay_microcontroller = null;

            public UsbReceiver(ReplayMicrocontroller p)
                : base()
            {
                replay_microcontroller = p;
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
                            if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                            {
                                replay_microcontroller.OnPermissionGranted(device);
                            }
                        }
                        else if (UsbManager.ActionUsbDeviceAttached.Equals(action))
                        {
                            //replay_microcontroller.VerifyCorrectDeviceIsAttached();
                            //replay_microcontroller.OnDeviceAttached(device);
                        }
                        else if (UsbManager.ActionUsbDeviceDetached.Equals(action))
                        {
                            replay_microcontroller.OnDeviceDetached(device);
                        }
                    }

                }
            }
        }

        protected void OnDeviceAttached(UsbDevice device)
        {
            //Check to see if we have permission to access the USB device
            if (USB_Manager != null && USB_Device != null)
            {
                if (device.VendorId == USB_Device.VendorId && device.ProductId == USB_Device.ProductId)
                {
                    if (USB_Manager.HasPermission(device))
                    {
                        //Permission has already been granted, so proceed...
                        OnPermissionGranted(device);
                    }
                    else
                    {
                        //Request permission to interact with the USB device
                        PendingIntent pending_intent = PendingIntent.GetBroadcast(MainActivity, 0,
                            new Android.Content.Intent(UsbReceiver.ACTION_USB_PERMISSION), 0);
                        IntentFilter intent_filter = new IntentFilter(UsbReceiver.ACTION_USB_PERMISSION);
                        MainActivity.RegisterReceiver(USB_Receiver, intent_filter);
                        USB_Manager.RequestPermission(USB_Device, pending_intent);
                    }
                }
            }
        }

        protected void OnDeviceDetached(UsbDevice device)
        {
            try
            {
                if (USB_Device != null)
                {
                    if (device != null && device.VendorId == USB_Device.VendorId && device.ProductId == USB_Device.ProductId)
                    {
                        Disconnect();
                    }
                }
            }
            catch (Exception)
            {
                //empty
            }
        }

        protected void OnPermissionGranted(UsbDevice device)
        {
            USB_Device = device;
            Connect();
        }

        protected void Connect()
        {
            //If we reach this function, then the device is connected and we have permission to interact with it...
            //So let's set everything up...
            if (USB_Device != null)
            {
                CdcAcmSerialDriver serial_driver = new CdcAcmSerialDriver(USB_Device);
                USB_Device_Connection = USB_Manager.OpenDevice(serial_driver.GetDevice());
                SerialPort = serial_driver.Ports.FirstOrDefault();
                if (SerialPort != null && USB_Device_Connection != null)
                {
                    try
                    {
                        //Open the serial port and set up the connection
                        SerialPort.Open(USB_Device_Connection);
                        SerialPort.SetParameters(115200, UsbSerialPort.DATABITS_8, StopBits.One, Parity.None);
                        SerialPort.SetDTR(true);
                        IsSerialPortOpen = true;
                        Setup();

                        //Listen for "usb device detached" events in case this device gets detached
                        IntentFilter intent_filter = new IntentFilter();
                        intent_filter.AddAction(UsbManager.ActionUsbDeviceDetached);
                        USB_Receiver = new UsbReceiver(this);
                        MainActivity.ApplicationContext.RegisterReceiver(USB_Receiver, intent_filter);
                    }
                    catch (Exception e)
                    {
                        if (USB_Receiver != null)
                        {
                            try
                            {
                                MainActivity.UnregisterReceiver(USB_Receiver);
                            }
                            catch (Exception)
                            {
                                //empty
                            }
                        }
                        
                        IsSerialPortOpen = false;
                        SerialPort = null;
                        Console.WriteLine(e.StackTrace);
                    }
                }
            }
        }

        protected async Task Disconnect()
        {
            if (USB_Receiver != null)
            {
                MainActivity.UnregisterReceiver(USB_Receiver);
            }

            if (SerialPort != null && IsSerialPortOpen)
            {
                try
                {
                    //Send a command to the background streaming thread to close
                    _background_streamer.CancelAsync();

                    //Wait for the background streaming thread to finish closing
                    await Task.Run(async () =>
                    {
                        while (_background_streamer_running)
                        {
                            await Task.Delay(100);
                        }
                    });

                    //Close the serial port
                    SerialPort.Close();

                    //Set a boolean indicating the serial port is closed
                    IsSerialPortOpen = false;
                }
                catch (Exception)
                {
                    //empty
                }
            }
        }

        protected void VerifyCorrectDeviceIsAttached()
        {
            //If the USB device object does not yet exist
            if (USB_Device == null)
            {
                //Grab the Android USB manager and get a list of connected devices
                var attached_devices = USB_Manager.DeviceList;

                //Find the FitMi device in the list of connected devices
                foreach (var d in attached_devices.Keys)
                {
                    foreach (var recognizedDevice in RecognizedArduinoDevices)
                    {
                        int vendor_id = recognizedDevice.Item2;
                        int product_id = recognizedDevice.Item3;

                        if (attached_devices[d].VendorId == vendor_id && attached_devices[d].ProductId == product_id)
                        {
                            USB_Device = attached_devices[d];

                            //We found the correct device, so break out of the loop
                            break;
                        }
                    }
                }
            }
        }

        public void Open ()
        {
            VerifyCorrectDeviceIsAttached();

            if (USB_Device != null)
            {
                OnDeviceAttached(USB_Device);
            }
        }

        #endregion

        #region Enumerations specific to this class

        private enum ControllerState
        {
            NotStarted,
            WaitForControllerResponse,
            WaitForDeviceValue,
            WaitForGain,
            WaitForOffset1,
            WaitForOffset2,
            WaitForSlope,
            Ready
        }

        public enum ControllerDeviceState
        {
            DeviceNotAttached,
            DeviceAttached_WaitingForCalibrationInformation,
            DeviceReady
        }

        #endregion

        #region Private data members

        private const int adc_threshold = 8_388_608;
        private const int adc_threshold_2x = 16_777_216;
        private char[] seps = new char[4] { ' ', '\t', '\n', '\r' };

        private BackgroundWorker _background_streamer = new BackgroundWorker();
        private bool _background_streamer_running = false;
        private object _background_streamer_running_lock = new object();

        private List<Int64> _stream_timestamps = new List<long>();
        private List<Int64> _stream_data_1 = new List<long>();
        private List<Int64> _stream_data_2 = new List<long>();
        private object _stream_lock = new object();
        private List<byte> _input_data = new List<byte>();

        private ControllerState current_controller_state;
        private ControllerDeviceState current_device_state;

        private bool microcontroller_debug = false;

        #endregion

        #region Constructor
        
        public ReplayMicrocontroller(Activity a)
        {
            //Grab the Android activity
            MainActivity = a;

            //Grab the USB manager from the system
            USB_Manager = MainActivity.ApplicationContext.GetSystemService(Android.Content.Context.UsbService) as Android.Hardware.Usb.UsbManager;
            
            //Set the controller state
            current_controller_state = ControllerState.NotStarted;

            //Set the device state
            current_device_state = ControllerDeviceState.DeviceNotAttached;

            //Start the background streamer
            _background_streamer.WorkerSupportsCancellation = true;
            _background_streamer.WorkerReportsProgress = true;
            _background_streamer.DoWork += BackgroundStreamingThread_DoWork;
            _background_streamer.ProgressChanged += HandleProgressChangesFromBackgroundStreamingThread;
            _background_streamer.RunWorkerCompleted += HandleExitFromBackgroundStreamingThread;
        }

        #endregion

        #region IDisposable implementation
        
        public void Dispose()
        {
            try
            {
                if (USB_Receiver != null)
                {
                    MainActivity.UnregisterReceiver(USB_Receiver);
                }
            }
            catch (Exception)
            {
                //empty
            }
        }

        #endregion

        #region Background streamer stuff

        private void HandleExitFromBackgroundStreamingThread(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (_background_streamer_running_lock)
            {
                _background_streamer_running = false;
            }

            if (e.Error != null)
            {
                Log.Error("Streamer", "BACKGROUND STREAMER FAILED");
                Log.Error("Streamer", e.Error.ToString());
            }
        }

        private void HandleProgressChangesFromBackgroundStreamingThread(object sender, ProgressChangedEventArgs e)
        {
            NotifyPropertyChanged("CurrentDeviceType");
        }

        private void BackgroundStreamingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            lock (_background_streamer_running_lock)
            {
                _background_streamer_running = true;
            }

            while (!_background_streamer.CancellationPending)
            {
                if (SerialPort != null && IsSerialPortOpen) // connected
                {
                    ReadStream();

                    if (IsDeviceConnectedToController)
                    {
                        if (current_device_state == ControllerDeviceState.DeviceAttached_WaitingForCalibrationInformation)
                        {
                            RequestLoadcellCalibrationSlope();
                            RequestLoadcellGain();
                            RequestLoadcellOffset(0);
                            RequestLoadcellOffset(1);
                        }
                    }
                }
                else
                {
                    Connect();
                }

                Thread.Sleep(10);
            }
        }

        #endregion

        #region Private methods

        private void Setup()
        {
            //This function is purposely a BLOCKING function. It may take some time to return since we are sending and
            //receiving serial communication in a synchronous manner.
            DateTime current_state_start_time = DateTime.Now;
            DateTime last_request_time = DateTime.MinValue;
            TimeSpan inter_request_interval = TimeSpan.FromMilliseconds(100);

            //Set some initial setup variables
            IsDeviceConnectedToController = false;
            LastPingResponse = DateTime.MinValue;
            current_controller_state = ControllerState.NotStarted;
            current_device_state = ControllerDeviceState.DeviceNotAttached;

            //Loop until we are confident the controller is ready
            while (current_controller_state != ControllerState.Ready)
            {
                //Read the latest data coming in from the microcontroller
                ReadStream();

                //Now take a certain action depending on the current state of the microcontroller
                switch (current_controller_state)
                {
                    case ControllerState.NotStarted:
                        
                        //Check to see if the USB connection is open and ready-to-go
                        if (IsConnectionOpen())
                        {
                            //If so, disable streaming (in case streaming was already enabled previously for some reason)
                            EnableStreaming(false);  

                            //Set the new state
                            current_controller_state = ControllerState.WaitForControllerResponse;
                        }

                        break;
                    case ControllerState.WaitForControllerResponse:

                        if (LastPingResponse > current_state_start_time)
                        {
                            current_controller_state = ControllerState.WaitForDeviceValue;
                            LastCurrentDeviceTypeUpdateTime = DateTime.MinValue;
                            current_state_start_time = DateTime.Now;
                            last_request_time = DateTime.MinValue;
                        }
                        else
                        {
                            //Ping the controller
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                PingController();
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.WaitForDeviceValue:

                        if (this.LastCurrentDeviceTypeUpdateTime > current_state_start_time)
                        {
                            if (ReplayDeviceTypeConverter.IsIsometricModule(CurrentDeviceType))
                            {
                                current_controller_state = ControllerState.WaitForGain;
                                LastCalibrationGainLoadTime = DateTime.MinValue;
                                current_state_start_time = DateTime.Now;
                                last_request_time = DateTime.MinValue;
                            }
                            else
                            {
                                current_controller_state = ControllerState.Ready;
                            }
                        }
                        else
                        {
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                RequestUpdatedDeviceValue();
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.WaitForGain:

                        if (this.LastCalibrationGainLoadTime > current_state_start_time)
                        {
                            current_controller_state = ControllerState.WaitForOffset1;
                            LastCalibrationOffset1LoadTime = DateTime.MinValue;
                            current_state_start_time = DateTime.Now;
                            last_request_time = DateTime.MinValue;
                        }
                        else
                        {
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                RequestLoadcellGain();
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.WaitForOffset1:

                        if (this.LastCalibrationOffset1LoadTime > current_state_start_time)
                        {
                            current_controller_state = ControllerState.WaitForOffset2;
                            LastCalibrationOffset2LoadTime = DateTime.MinValue;
                            current_state_start_time = DateTime.Now;
                            last_request_time = DateTime.MinValue;
                        }
                        else
                        {
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                RequestLoadcellOffset(0);
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.WaitForOffset2:

                        if (this.LastCalibrationOffset2LoadTime > current_state_start_time)
                        {
                            current_controller_state = ControllerState.WaitForSlope;
                            LastCalibrationSlopeLoadTime = DateTime.MinValue;
                            current_state_start_time = DateTime.Now;
                            last_request_time = DateTime.MinValue;
                        }
                        else
                        {
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                RequestLoadcellOffset(1);
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.WaitForSlope:

                        if (this.LastCalibrationSlopeLoadTime > current_state_start_time)
                        {
                            current_controller_state = ControllerState.Ready;
                            current_state_start_time = DateTime.Now;
                            last_request_time = DateTime.MinValue;
                        }
                        else
                        {
                            if (DateTime.Now >= (last_request_time + inter_request_interval))
                            {
                                RequestLoadcellCalibrationSlope();
                                last_request_time = DateTime.Now;
                            }
                        }

                        break;
                    case ControllerState.Ready:
                        break;
                    default:
                        break;
                }
            }

            //Run the background thread to asynchronously stream data
            _background_streamer.RunWorkerAsync();
        }

        private void SerialPortWrite (byte[] bytes, int timeout_millis)
        {
            if (microcontroller_debug)
            {
                Console.WriteLine("[MICROCONTROLLER DEBUG WRITE] " + BitConverter.ToString(bytes));
            }

            SerialPort.Write(bytes, timeout_millis);
        }

        private void SimpleCommand(string command, int parameter, bool use_ascii = true)
        {
            try
            {
                if (IsSerialPortOpen)
                {
                    if (use_ascii)
                    {
                        string parameterString = parameter.ToString();
                        string newCommandString = command.Replace("i", parameterString);
                        SerialPortWrite(Encoding.ASCII.GetBytes(newCommandString), 0);
                    }
                    else
                    {
                        var p = Convert.ToChar(Convert.ToByte(parameter)).ToString();
                        string newCommandString = command.Replace("i", p);
                        SerialPortWrite(Encoding.ASCII.GetBytes(newCommandString), 0);
                    }
                }
            }
            catch(Exception)
            {
                ReOpenUsbConnection();
            }
        }

        private void ReOpenUsbConnection()
        {
            SerialPort.Close();
            Connect();
        }

        private List<byte> input_bytes = new List<byte>();
        private byte end_of_line_byte = 0x0a;

        public void ReadStream ()
        {
            //Grab the new bytes that have come in from the Arduino board
            if (SerialPort != null && IsSerialPortOpen)
            {
                byte[] b = new byte[1024];
                int num_bytes_read = SerialPort.Read(b, 20);
                if (num_bytes_read > 0)
                {
                    var bytes_read = b.ToList().GetRange(0, num_bytes_read);
                    input_bytes.AddRange(bytes_read);
                    int index_of_last_newline = input_bytes.FindLastIndex(x => x == end_of_line_byte);
                    if (index_of_last_newline > -1)
                    {
                        var subset_of_bytes = input_bytes.GetRange(0, index_of_last_newline + 1);
                        input_bytes = input_bytes.Skip(index_of_last_newline + 1).ToList();

                        var string_data = System.Text.Encoding.ASCII.GetString(subset_of_bytes.ToArray()).Trim();
                        var individual_lines = string_data.Split('\n').ToList();

                        foreach (string line in individual_lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                if (microcontroller_debug)
                                {
                                    Console.WriteLine("[MICROCONTROLLER DEBUG READ] " + line);
                                }

                                HandleNewDataRow(line);
                                //Log.Debug("Stream", line);
                            }
                        }
                    }
                }
            }
        }

        private void HandleNewDataRow(string line_of_data)
        {
            try
            {
                var split_line = line_of_data.Split(seps);
                if (split_line.Length > 0)
                {
                    bool success = Int32.TryParse(split_line[0], out int line_id);

                    switch (line_id)
                    {
                        //Line ID of 1 indicates isometric data, line ID of 2 indicates quadrature encoder data
                        case 1:
                        case 2:

                            if (split_line.Length >= 4)
                            {
                                bool s1 = Int64.TryParse(split_line[1], out long millis);
                                bool s2 = Int64.TryParse(split_line[2], out long d1);
                                bool s3 = Int64.TryParse(split_line[3], out long d2);

                                lock (_stream_lock)
                                {
                                    _stream_timestamps.Add(millis);

                                    if (d1 > adc_threshold)
                                    {
                                        d1 -= adc_threshold_2x;
                                    }
                                    _stream_data_1.Add(d1);

                                    if (s3)
                                    {
                                        if (d2 > adc_threshold)
                                        {
                                            d2 -= adc_threshold_2x;
                                        }
                                        _stream_data_2.Add(d2);
                                    }
                                }
                            }
                            else if (split_line.Length >= 3)
                            {
                                bool s1 = Int64.TryParse(split_line[1], out long millis);
                                bool s2 = Int64.TryParse(split_line[2], out long d1);

                                lock (_stream_lock)
                                {
                                    _stream_timestamps.Add(millis);
                                    _stream_data_1.Add(d1);
                                }
                            }
                            else if (split_line.Length == 2 && split_line[0].Trim().Equals("1") && split_line[1].Trim().Equals("0"))
                            {
                                LastPingResponse = DateTime.Now;
                            }

                            break;

                        //Line ID of 3 indicates that this row tells us the type of device currently connected
                        //Line ID of 4 indicates that a "disconnect" event has occurred.
                        //Line ID of 5 indicates that a new device has been connected
                        case 3:
                        case 4:
                        case 5:
                            
                            success = Int32.TryParse(split_line[2], out int device_type_int);
                            if (success)
                            {
                                var new_device_type = ReplayDeviceTypeConverter.ConvertBoardValueToEnumeratedType(device_type_int);

                                IsDeviceConnectedToController = ((line_id == 3) || (line_id == 5));
                                if (IsDeviceConnectedToController)
                                {
                                    if (new_device_type == ReplayDeviceType.Handle_Isometric ||
                                        new_device_type == ReplayDeviceType.Knob_Isometric ||
                                        new_device_type == ReplayDeviceType.Wrist_Isometric ||
                                        new_device_type == ReplayDeviceType.Pinch ||
                                        new_device_type == ReplayDeviceType.Pinch_Left)
                                    {
                                        current_device_state = ControllerDeviceState.DeviceAttached_WaitingForCalibrationInformation;
                                    }
                                    else
                                    {
                                        current_device_state = ControllerDeviceState.DeviceReady;
                                    }
                                }
                                else
                                {
                                    current_device_state = ControllerDeviceState.DeviceNotAttached;
                                }

                                LastCurrentDeviceTypeUpdateTime = DateTime.Now;
                                if (CurrentDeviceType != new_device_type)
                                {
                                    CurrentDeviceType = new_device_type;
                                }
                            }

                            success = Int32.TryParse(split_line[3], out int set_type_int);
                            if (success)
                            {
                                CurrentDeviceSet = set_type_int;
                            }

                            _background_streamer.ReportProgress(0);

                            break;

                        //Line ID of 6 indicates that the stream has failed to send data
                        case 6:

                            //If the stream has failed to send data, then let's say that no device is currently attached
                            IsDeviceConnectedToController = false;
                            current_device_state = ControllerDeviceState.DeviceNotAttached;
                            CurrentDeviceType = ReplayDeviceType.Unknown;
                            _background_streamer.ReportProgress(0);

                            break;
                        case 12:

                            bool success_a = Int32.TryParse(split_line[1], out int response_a);
                            bool success_b = Int32.TryParse(split_line[2], out int response_b);
                            if (success_a && success_b)
                            {
                                Loadcell_1_Slope = response_a / Loadcell_Slope_Multiplier;
                                Loadcell_2_Slope = response_b / Loadcell_Slope_Multiplier;
                                LastCalibrationSlopeLoadTime = DateTime.Now;

                                if (IsDeviceConnectedToController && 
                                    current_device_state == ControllerDeviceState.DeviceAttached_WaitingForCalibrationInformation)
                                {
                                    current_device_state = ControllerDeviceState.DeviceReady;
                                    _background_streamer.ReportProgress(0);
                                }
                            }

                            break;

                        case 13:
                            break;
                        case 14:
                        case 15:
                        case 16:

                            bool success_offset = Byte.TryParse(split_line[1], out byte arduino_response_value);
                            if (success_offset)
                            {
                                if (line_id == 14)
                                {
                                    Loadcell_1_Offset = arduino_response_value;
                                    LastCalibrationOffset1LoadTime = DateTime.Now;
                                }
                                else if (line_id == 15)
                                {
                                    Loadcell_2_Offset = arduino_response_value;
                                    LastCalibrationOffset2LoadTime = DateTime.Now;
                                }
                                else
                                {
                                    //If the line id is 16, then it is actually a GAIN and not an OFFSET
                                    Loadcell_Gain = arduino_response_value - 1;
                                    LastCalibrationGainLoadTime = DateTime.Now;
                                }
                            }

                            break;

                        default:

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug("ERROR", e.ToString());
            }
        }

        private void TransmitCalibrationSlopes()
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    int slope1_to_transmit = Convert.ToInt32(Math.Round(this.Loadcell_1_Slope * Loadcell_Slope_Multiplier));
                    int slope2_to_transmit = Convert.ToInt32(Math.Round(this.Loadcell_2_Slope * Loadcell_Slope_Multiplier));
                    var slope_a_bytes = BitConverter.GetBytes(slope1_to_transmit);
                    var slope_b_bytes = BitConverter.GetBytes(slope2_to_transmit);

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("e");
                        //var b2 = Encoding.ASCII.GetBytes(device_type_board_value.ToString());
                        //var b3 = Encoding.ASCII.GetBytes(this.CurrentDeviceSet.ToString());

                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        //master_array.AddRange(b2);
                        //master_array.AddRange(b3);
                        master_array.AddRange(slope_a_bytes);
                        master_array.AddRange(slope_b_bytes);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private void TransmitCalibrationOffset(int loadcell_select)
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    int loadcell_offset = loadcell_select == 0 ? this.Loadcell_1_Offset : this.Loadcell_2_Offset;
                    var offset_to_transmit = Convert.ToByte(Math.Max(Byte.MinValue, Math.Min(Byte.MaxValue, loadcell_offset)));

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("i");
                        //var b2 = Encoding.ASCII.GetBytes(device_type_board_value.ToString());
                        //var b3 = Encoding.ASCII.GetBytes(this.CurrentDeviceSet.ToString());
                        var b4 = Encoding.ASCII.GetBytes((loadcell_select + 1).ToString());
                        //var b5 = Encoding.ASCII.GetBytes(Loadcell_Gain.ToString());

                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        //master_array.AddRange(b2);
                        //master_array.AddRange(b3);
                        master_array.AddRange(b4);
                        //master_array.AddRange(b5);
                        master_array.Add(offset_to_transmit);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private void TransmitCalibrationGain()
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("k");
                        var b2 = Encoding.ASCII.GetBytes(Loadcell_Gain.ToString());

                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        master_array.AddRange(b2);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        #endregion

        #region Public Methods

        public void PingController()
        {
            try
            {
                this.SimpleCommand("A", 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
        
        public void RequestUpdatedDeviceValue()
        {
            try
            {
                this.SimpleCommand("D", 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void TareQuadratureEncoder()
        {
            try
            {
                this.SimpleCommand("F", 0);
            }
            catch (Exception)
            {
                //empty
            }
        }

        public void EnableStreaming(bool enable)
        {
            if (this.IsConnectionOpen())
            {
                try
                {
                    int streamingMode = enable ? 1 : 0;
                    this.SimpleCommand("zi", streamingMode);

                    //Set this variable after the command finishes
                    IsStreaming = enable;
                }
                catch (Exception e)
                {
                    //empty
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        public void SetLoadcellOffset(int loadcell_idx, int new_offset)
        {
            if (new_offset >= Byte.MinValue && new_offset <= Byte.MaxValue)
            {
                if (loadcell_idx == 0)
                {
                    Loadcell_1_Offset = new_offset;
                }
                else
                {
                    Loadcell_2_Offset = new_offset;
                }

                TransmitCalibrationOffset(loadcell_idx);
            }
        }

        public void SetLoadcellGain(int gain)
        {
            Loadcell_Gain = gain;
            TransmitCalibrationGain();
        }

        public void SetLoadcellCalibrationSlope(int loadcell_idx, double new_slope)
        {
            if (loadcell_idx == 0)
            {
                Loadcell_1_Slope = new_slope;
            }
            else
            {
                Loadcell_2_Slope = new_slope;
            }

            TransmitCalibrationSlopes();
        }

        /// <summary>
        /// loadcell_idx should be 0 or 1.
        /// </summary>
        /// <param name="loadcell_idx">loadcell_idx should be 0 or 1</param>
        public void RequestLoadcellOffset(int loadcell_idx)
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("j");
                        //var b2 = Encoding.ASCII.GetBytes(device_type_board_value.ToString());
                        //var b3 = Encoding.ASCII.GetBytes(this.CurrentDeviceSet.ToString());
                        var b4 = Encoding.ASCII.GetBytes((loadcell_idx + 1).ToString());
                        //var b5 = Encoding.ASCII.GetBytes(Loadcell_Gain.ToString());

                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        //master_array.AddRange(b2);
                        //master_array.AddRange(b3);
                        master_array.AddRange(b4);
                        //master_array.AddRange(b5);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        public void RequestLoadcellGain()
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("l");
                        //var b2 = Encoding.ASCII.GetBytes(device_type_board_value.ToString());
                        //var b3 = Encoding.ASCII.GetBytes(this.CurrentDeviceSet.ToString());

                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        //master_array.AddRange(b2);
                        //master_array.AddRange(b3);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        public void RequestLoadcellCalibrationSlope()
        {
            if (IsSerialPortOpen)
            {
                try
                {
                    var device_type_board_value = ReplayDeviceTypeConverter.ConvertEnumeratedTypeToBoardValue(this.CurrentDeviceType);

                    if (device_type_board_value > 0 && this.CurrentDeviceSet > 0)
                    {
                        var b1 = Encoding.ASCII.GetBytes("f");
                        List<byte> master_array = new List<byte>();
                        master_array.AddRange(b1);
                        byte[] master_array_final = master_array.ToArray();

                        SerialPortWrite(master_array_final, 0);
                    }
                }
                catch (Exception e)
                {
                    ReOpenUsbConnection();
                }
            }
        }

        public void Close()
        {
            Disconnect();
        }

        #endregion

        #region Public properties

        private const double Loadcell_Slope_Multiplier = 100_000.0;

        public int Loadcell_Gain { get; set; } = 2;
        public int Loadcell_1_Offset { get; set; } = 0;
        public int Loadcell_2_Offset { get; set; } = 0;
        public double Loadcell_1_Slope { get; set; } = 0.0;
        public double Loadcell_2_Slope { get; set; } = 0.0;

        public DateTime LastCalibrationGainLoadTime { get; private set; } = DateTime.MinValue;
        public DateTime LastCalibrationOffset1LoadTime { get; private set; } = DateTime.MinValue;
        public DateTime LastCalibrationOffset2LoadTime { get; private set; } = DateTime.MinValue;
        public DateTime LastCalibrationSlopeLoadTime { get; set; } = DateTime.MinValue;
        public DateTime LastPingResponse { get; set; } = DateTime.MinValue;
        public DateTime LastPluggedInCheckTime { get; private set; } = DateTime.MinValue;

        public DateTime LastCurrentDeviceTypeUpdateTime { get; private set; } = DateTime.MinValue;

        public int CurrentDeviceSet { get; set; } = 0;

        public ReplayDeviceType CurrentDeviceType { get; private set; } = ReplayDeviceType.Unknown;
        
        public ControllerDeviceState CurrentDeviceState
        {
            get
            {
                return current_device_state;
            }
        }

        public bool IsDeviceConnectedToController { get; private set; } = false;

        public bool IsStreaming { get; set; } = false;

        public (List<long>, List<long>, List<long>) FetchLatestData()
        {
            List<long> l1, l2, l3;

            lock (_stream_lock)
            {
                l1 = _stream_timestamps.ToList();
                l2 = _stream_data_1.ToList();
                l3 = _stream_data_2.ToList();

                _stream_timestamps.Clear();
                _stream_data_1.Clear();
                _stream_data_2.Clear();
            }

            return (l1, l2, l3);
        }

        public (List<long>, List<long>, List<long>) FetchLatestData(bool block, int timeout_ms)
        {
            List<long> l1 = new List<long>();
            List<long> l2 = new List<long>();
            List<long> l3 = new List<long>();
            DateTime start = DateTime.Now;
            bool done = false;
            while (!done)
            {
                (l1, l2, l3) = FetchLatestData();
                if (block)
                {
                    DateTime current_time = DateTime.Now;
                    if (current_time >= (start + TimeSpan.FromMilliseconds(timeout_ms)))
                    {
                        done = true;
                    }
                    else
                    {
                        if (l1.Count > 0)
                        {
                            done = true;
                        }
                    }
                }
                else
                {
                    done = true;
                }
            }

            return (l1, l2, l3);
        }

        public bool IsConnectionOpen()
        {
            return IsSerialPortOpen;
        }

        public bool IsSetupComplete()
        {
            return (current_controller_state == ControllerState.Ready);
        }

        #endregion
    }
}
