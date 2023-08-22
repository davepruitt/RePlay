using Android.App;
using RePlay_Common;
using RePlay_VNS_Triggering;
using System;

namespace RePlay.Manager
{
    public class PCMConnectionManager : NotifyPropertyChangedObject
    {
        #region Private Properties

        private Activity MainActivity;
        private PCM_Manager PCM;

        #endregion

        #region Public Properties

        public bool IsConnectedToPCM { get; set; } = false;
        public bool IsConnectedToRestore { get; set; } = false;

        #endregion

        #region Singleton Constructor

        private static PCMConnectionManager _instance = null;

        public static PCMConnectionManager Instance
        {
            get
            {
                if (_instance == null) throw new Exception("PCMConnection instance has not been instantiated");
                return _instance;
            }
        }

        public static void CreateInstance(Activity main)
        {
            _instance = new PCMConnectionManager(main);
        }

        public void RunConnectionCheck()
        {
            Console.WriteLine("Checking PCM status");
            PCM.CheckPCMStatus();
        }

        private PCMConnectionManager(Activity main)
        {
            MainActivity = main;
            PCM = new PCM_Manager(main);
            PCM.PropertyChanged += PCM_PropertyChanged;
        }

        private void PCM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsConnectedToPCM = PCM.IsConnectedToPCM;
            IsConnectedToRestore = PCM.IsConnectedToReStoreService;
            Console.WriteLine("Received response from PCM");
            NotifyPropertyChanged("IsConnected");
        }

        #endregion
    }
}