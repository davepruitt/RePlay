using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Android.Hardware.Usb;
using Android.App;
using Android.Content;
using Java.Nio;

namespace FitMiAndroid
{
    public class HIDPuckDongle : IDisposable
    {
        #region UsbReceiver subclass

        /// <summary>
        /// This class 
        /// </summary>
        public class UsbReceiver : BroadcastReceiver
        {
            public static string ACTION_USB_PERMISSION = "USB_PERMISSION";
            private HIDPuckDongle puck_dongle = null;

            public UsbReceiver(HIDPuckDongle p)
                : base()
            {
                puck_dongle = p;
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
                                puck_dongle.OnPermissionGranted(device);
                            }
                        }
                        else if (UsbManager.ActionUsbDeviceAttached.Equals(action))
                        {
                            //puck_dongle.VerifyCorrectDeviceIsAttached();
                            //puck_dongle.OnDeviceAttached(device);
                        }
                        else if (UsbManager.ActionUsbDeviceDetached.Equals(action))
                        {
                            puck_dongle.OnDeviceDetached(device);
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
                        //Close the device
                        Close();

                        //Fire some events
                        PuckDongleDetached?.Invoke(this, new EventArgs());
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
            //If we reach this function, then the device is connected and we have permission to interact with it...
            //So let's set everything up...
            if (USB_Device != null)
            {
                //Check to see that an interface exists
                if (USB_Device.InterfaceCount > 0)
                {
                    //If so, grab the interface to the device
                    DeviceInterface = USB_Device.GetInterface(0);

                    //Check to see that an input endpoint exists
                    if (DeviceInterface.EndpointCount > 0)
                    {
                        //If so, grab the input endpoint
                        InputEndpoint = DeviceInterface.GetEndpoint(0);

                        //Open a connection to the device
                        DeviceConnection = USB_Manager.OpenDevice(USB_Device);

                        //Make sure the connection to the device succeeded
                        if (DeviceConnection != null)
                        {
                            //Claim the device interface
                            DeviceConnection.ClaimInterface(DeviceInterface, true);

                            //Register a receiver to listen for "usb device detached" events in case this device gets detached
                            IntentFilter intent_filter = new IntentFilter();
                            intent_filter.AddAction(UsbManager.ActionUsbDeviceDetached);
                            USB_Receiver = new UsbReceiver(this);
                            MainActivity.ApplicationContext.RegisterReceiver(USB_Receiver, intent_filter);

                            //Set some important variables
                            IsOpen = true;
                            PlugState = true;
                            EmptyDataCount = 0;
                            ReceivingData = false;

                            CheckConnection();
                            WaitForData();

                            //Put the pucks in game mode
                            SendCommand(0, HidPuckCommands.GAMEON, 0x00, 0x01);
                            SendCommand(1, HidPuckCommands.GAMEON, 0x00, 0x01);
                            
                            if (BackgroundThread != null && !BackgroundThread.IsBusy)
                            {
                                //Start up the background thread to stream data from the device
                                BackgroundThread.RunWorkerAsync();

                                //Notify anyone listening that the puck dongle is ready to go
                                PuckDongleReady?.Invoke(this, new EventArgs());
                            }
                        }
                    }
                }
            }
        }

        protected void VerifyCorrectDeviceIsAttached()
        {
            if (USB_Device == null)
            {
                //Grab the Android USB manager and get a list of connected devices
                var attached_devices = USB_Manager.DeviceList;

                //Find the FitMi device in the list of connected devices
                foreach (var d in attached_devices.Keys)
                {
                    if (attached_devices[d].VendorId == VendorID && attached_devices[d].ProductId == ProductID)
                    {
                        USB_Device = attached_devices[d];

                        //We found the correct device, so break out of the loop
                        break;
                    }
                }
            }
        }

        #endregion

        #region Public members

        public Dictionary<string, Dictionary<string, HidPuckCommands>> Commands = new Dictionary<string, Dictionary<string, HidPuckCommands>>()
        {
            {   "red",
                new Dictionary<string, HidPuckCommands>()
                {
                    { "blink", HidPuckCommands.RBLINK },
                    { "pulse", HidPuckCommands.RPULSE }
                }
            },
            {
                "green",
                new Dictionary<string, HidPuckCommands>()
                {
                    { "blink", HidPuckCommands.GBLINK },
                    { "pulse", HidPuckCommands.GPULSE }
                }
            },
            {
                "blue",
                new Dictionary<string, HidPuckCommands>()
                {
                    { "blink", HidPuckCommands.BBLINK },
                    { "pulse", HidPuckCommands.BPULSE }
                }
            },
            {
                "motor",
                new Dictionary<string, HidPuckCommands>()
                {
                    { "blink", HidPuckCommands.MBLINK },
                    { "pulse", HidPuckCommands.MPULSE }
                }
            }
        };

        public event EventHandler<EventArgs> PuckDongleDetached;
        public event EventHandler<EventArgs> PuckDongleReady;

        #endregion

        #region Data Members

        public int VendorID = 0x04d8;
        public int ProductID = 0x2742;
        public int Release = 0;
        public int Verbosity = 0;
        public bool IsOpen = false;
        public bool ReceivingData = false;

        public PuckPacket PuckPack0 = new PuckPacket();
        public PuckPacket PuckPack1 = new PuckPacket();

        public int RX_HardwareState = 0;
        public int RX_Channel = 0;
        public int Block0_Pipe = 0;
        public int Block1_Pipe = 1;
        public int EmptyDataCount = 0;
        public bool PlugState = false;

        public int QueueCapacity = 10;
        public Queue<List<int>> USB_OutQueue = new Queue<List<int>>();
        public Queue<Tuple<int, bool>> TouchQueue = new Queue<Tuple<int, bool>>();

        public List<DateTime> LastSent = new List<DateTime>() { DateTime.MinValue, DateTime.MinValue };
        public BackgroundWorker BackgroundThread = null;

        public object ThreadingLock = new object();

        public List<byte> Inpt = new List<byte>();

        public Activity MainActivity;
        public UsbManager USB_Manager = null;
        public UsbReceiver USB_Receiver = null;

        public UsbDevice USB_Device = null;
        public UsbDeviceConnection DeviceConnection = null;
        public UsbInterface DeviceInterface = null;
        public UsbEndpoint InputEndpoint = null;

        public string full_packet = string.Empty;

        #endregion

        #region Constructor

        public HIDPuckDongle(Activity activity)
        {
            //Instantiate an object for the background thread (but don't start the background thread yet)
            BackgroundThread = new BackgroundWorker();
            BackgroundThread.WorkerReportsProgress = true;
            BackgroundThread.WorkerSupportsCancellation = true;
            BackgroundThread.DoWork += InputChecker;
            BackgroundThread.RunWorkerCompleted += HIDPuckDongle_BackgroundWorkerCompleted;

            //Grab a copy of the activity
            MainActivity = activity;

            //Grab the USB manager from the system
            USB_Manager = MainActivity.ApplicationContext.GetSystemService(Android.Content.Context.UsbService) as Android.Hardware.Usb.UsbManager;
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

        public void Open()
        {
            VerifyCorrectDeviceIsAttached();

            if (USB_Device != null)
            {
                OnDeviceAttached(USB_Device);
            }
        }

        public void CheckConnection()
        {
            for (int i = 0; i < 200; i++)
            {
                CheckForNewPuckData();
                if (PuckPack0.Connected || PuckPack1.Connected)
                {
                    break;
                }

                Thread.Sleep(1);
            }

            SendCommand(0, HidPuckCommands.DNGLRST, 0x00, 0x00);
            Thread.Sleep(600);
        }

        public void WaitForData()
        {
            for (int i = 0; i < 200; i++)
            {
                Thread.Sleep(1);
                if (ReceivingData)
                    break;
            }
        }

        private void HIDPuckDongle_BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set a flag indicating that the puck is no longer open
            IsOpen = false;
        }

        private void InputChecker(object sender, DoWorkEventArgs e)
        {
            ReceivingData = true;
            int read_fail_count = 0;
            int too_many_fails = 70;

            Dictionary<string, bool> touch_history = new Dictionary<string, bool>()
            {
                { "puck0", false },
                { "puck1", false }
            };

            int bufferSize = 62;
            ByteBuffer buffer = ByteBuffer.Allocate(bufferSize);
            buffer.Order(ByteOrder.LittleEndian);

            UsbRequest request = new UsbRequest();
            bool status = request.Initialize(DeviceConnection, InputEndpoint);

            while (IsOpen)
            {
                try
                {
                    if (DeviceConnection != null)
                    {
                        if (status)
                        {
                            byte[] inpt = new byte[bufferSize];

                            request.Queue(buffer, bufferSize);
                            if (DeviceConnection.RequestWait().Equals(request))
                            {
                                buffer.Position(0);

                                //Process the buffer
                                for (int i = 0; i < bufferSize; i++)
                                {
                                    inpt[i] = (byte)buffer.Get();
                                }

                                full_packet = BitConverter.ToString(inpt);

                                lock (ThreadingLock)
                                {
                                    Inpt = inpt.ToList();
                                }
                            }
                        }

                        if (Inpt == null || Inpt.Count == 0)
                        {
                            read_fail_count++;
                            if (read_fail_count > too_many_fails)
                            {
                                ReceivingData = false;
                            }
                        }
                        else
                        {
                            read_fail_count = 0;
                            ReceivingData = true;
                            CheckForTouch(Inpt.ToArray(), touch_history, 0);
                            CheckForTouch(Inpt.ToArray(), touch_history, 1);
                        }

                        if (USB_OutQueue.Count > 0)
                        {
                            var output_msg = USB_OutQueue.Dequeue().Select(x => (byte)x).ToArray();
                            var response = DeviceConnection.ControlTransfer((UsbAddressing)0x21, 0x9, 0x300, 0,
                                output_msg, output_msg.Length, 5000);
                        }
                    }

                }
                catch (Exception)
                {
                    full_packet = "ERROR ENCOUNTERED IN BACKGROUND LOOP";
                    IsOpen = false;
                    break;
                }

                Thread.Sleep(new TimeSpan(100));
            }

            //Empty the USB out queue
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    if (USB_OutQueue.Count > 0)
                    {
                        if (DeviceConnection != null)
                        {
                            var output_msg = USB_OutQueue.Dequeue().Select(x => (byte)x).ToArray();
                            //DeviceConnection.BulkTransfer(OutputEndpoint, output_msg, output_msg.Length, 30);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //empty
            }

            request.Cancel();

            if (DeviceConnection != null)
            {
                DeviceConnection.Close();
            }
        }

        public void CheckForTouch(byte[] inpt, Dictionary<string, bool> touch_history, int puck_num)
        {
            int index = 29;
            if (puck_num == 1)
            {
                index = 59;
            }

            var status = inpt[index];
            var touch = (status & 0b0000_0100) >> 2;

            if (puck_num == 0)
            {
                if (touch > 0 && !touch_history["puck0"])
                {
                    if (TouchQueue.Count < QueueCapacity)
                    {
                        TouchQueue.Enqueue(new Tuple<int, bool>(0, true));
                    }
                }
                else if (touch == 0 && touch_history["puck0"])
                {
                    if (TouchQueue.Count < QueueCapacity)
                    {
                        TouchQueue.Enqueue(new Tuple<int, bool>(0, false));
                    }
                }

                touch_history["puck0"] = touch > 0 ? true : false;
            }
        }

        public void CheckForNewPuckData()
        {
            if (ReceivingData)
            {
                try
                {
                    List<byte> this_thread_input = new List<byte>();
                    lock (ThreadingLock)
                    {
                        this_thread_input = Inpt.ToList();
                    }

                    Parse_RX_Data(this_thread_input.GetRange(60, 2).ToArray());
                    PuckPack0.Parse(this_thread_input.GetRange(0, 30).ToArray());
                    PuckPack1.Parse(this_thread_input.GetRange(30, 30).ToArray());

                    while (TouchQueue.Count > 0)
                    {
                        var t = TouchQueue.Dequeue();
                        var puck_num = t.Item1;
                        var state = t.Item2;

                        if (puck_num == 0)
                        {
                            PuckPack0.Touch = state;
                        }
                        else if (puck_num == 1)
                        {
                            PuckPack1.Touch = state;
                        }
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        public void Parse_RX_Data(byte[] byte_array_rx_data)
        {
            var result = BitConverter.ToInt16(byte_array_rx_data, 0);
            RX_HardwareState = result >> 13;
            RX_Channel = (result & 0b0001_1111_1100_0000) >> 6;
            Block0_Pipe = (result & 0b111000) >> 3;
            Block1_Pipe = (result & 0b111);
        }

        public void Parse_RX_Data(string rxdata)
        {
            byte[] string_bytes = Encoding.ASCII.GetBytes(rxdata);
            Parse_RX_Data(string_bytes);
        }

        public void SendCommand(int puck_number, HidPuckCommands cmd, int msb, int lsb)
        {
            var command = (0b1110_0000 & (puck_number << 5)) | (byte)cmd;
            if (IsPlugged())
            {
                if (this.USB_OutQueue.Count < QueueCapacity)
                {
                    //this.USB_OutQueue.Enqueue(new List<int>() { 0x00, command, msb, lsb });
                    this.USB_OutQueue.Enqueue(new List<int>() { command, msb, lsb });
                }
            }
        }

        public void NoteSending(string value)
        {
            //empty
        }

        public void Actuate(int puck_number, int duration, int amp, string atype = "blink", string actuator = "motor")
        {
            var puck_0_time_since_last_sent = (DateTime.Now - LastSent[0]).TotalSeconds;
            var puck_1_time_since_last_sent = (DateTime.Now - LastSent[1]).TotalSeconds;

            if (puck_number == 0 && puck_0_time_since_last_sent < 0.2)
                return;
            if (puck_number == 1 && puck_1_time_since_last_sent < 0.2)
                return;

            LastSent[puck_number] = DateTime.Now;
            var durbyte = Convert.ToInt32(Math.Min((duration * 255.0) / 1500.0, 255.0));
            amp = Math.Min(amp, 100);

            try
            {
                var cmd = Commands[actuator][atype];
                SendCommand(puck_number, cmd, durbyte, amp);
            }
            catch (Exception)
            {
                //empty
            }
        }

        public void SetTouchBuzz(int puck_number, int value)
        {
            SendCommand(puck_number, HidPuckCommands.TOUCHBUZ, 0, value);
        }

        public void ChangeRXFrequency(int new_frequency)
        {
            SendCommand(0, HidPuckCommands.RXCHANGEFREQ, 0, new_frequency);
        }

        public void SetUSBPipes(int pack0_pipe = 0, int pack1_pipe = 1)
        {
            pack0_pipe = Math.Min(pack0_pipe, 5);
            pack1_pipe = Math.Min(pack1_pipe, 5);
            SendCommand(0, HidPuckCommands.SETUSBPIPES, pack0_pipe, pack1_pipe);
        }

        public void StartSpy(int spy_channel = 12, int duration = 100)
        {
            if (duration > 255)
            {
                duration = 255;
            }

            SendCommand(0, HidPuckCommands.CHANSPY, spy_channel, duration);
        }

        public void Stop()
        {
            IsOpen = false;
        }

        public void Close()
        {
            //Unregister the receiver that we initially registered when this object was constructed
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
            
            //Send a couple closing commands to the pucks
            if (IsPlugged() && IsOpened())
            {
                SetTouchBuzz(0, 1);
                SetTouchBuzz(1, 1);
            }

            //Set the boolean flag indicating that the connection is closed
            IsOpen = false;

            //Cancel the background thread
            if (BackgroundThread != null && BackgroundThread.IsBusy)
            {
                BackgroundThread.CancelAsync();
            }

            //Clean up any objects that need to be cleaned up
            if (USB_Device != null)
            {
                //Close the device connection
                if (DeviceConnection != null)
                {
                    try
                    {
                        DeviceConnection.Close();
                    }
                    catch (Exception)
                    {
                        //empty
                    }
                }
                
                //Set the objects to be null
                DeviceConnection = null;
                USB_Device = null;
                DeviceInterface = null;
                InputEndpoint = null;
            }
        }

        public bool IsPlugged()
        {
            //If we don't yet have an instance of the USB manager object, then get one.
            if (USB_Manager == null)
            {
                USB_Manager = MainActivity.ApplicationContext.GetSystemService(Android.Content.Context.UsbService) as Android.Hardware.Usb.UsbManager;
            }

            //Get a list of attached devices
            var attached_devices = USB_Manager.DeviceList;

            //Find the FitMi device in the list of connected devices
            foreach (var d in attached_devices.Keys)
            {
                if (attached_devices[d].VendorId == VendorID && attached_devices[d].ProductId == ProductID)
                {
                    //We found the device, therefore it is plugged in. Return the value of true.
                    return true;
                }
            }

            //If we reach this code, the device is not plugged in, so return false.
            return false;
        }

        public bool IsOpened()
        {
            return this.IsOpen;
        }

        public bool IsPluggedFast()
        {
            return this.ReceivingData;
        }

        public UsbDevice GetDeviceInfo()
        {
            return USB_Device;
        }
    }
}
