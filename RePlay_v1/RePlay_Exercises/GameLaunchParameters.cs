using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RePlay_Exercises
{
    public class GameLaunchParameters
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GameLaunchParameters()
        {
            //empty
        }

        #endregion

        #region Properties

        public string ContentDirectory { get; set; } = string.Empty;

        public ExerciseDeviceType Device { get; set; } = ExerciseDeviceType.Unknown;

        public ExerciseType Exercise { get; set; } = ExerciseType.Unknown;

        public double Duration { get; set; } = 2.0;

        public string TabletID { get; set; } = "UnknownTablet";

        public double Gain { get; set; } = 1.0;

        public int Difficulty { get; set; } = 1;

        public string SubjectID { get; set; } = "UnknownSubject";

        public string ProjectID { get; set; } = string.Empty;

        public string SiteID { get; set; } = string.Empty;

        public bool ShowPCMConnectionStatus { get; set; } = true;

        public bool ShowStimulationRequests { get; set; } = true;

        public bool DebugMode { get; set; } = false;

        public bool LaunchedFromPrescription { get; set; } = false;

        public VNSAlgorithmParameters VNS_AlgorithmParameters { get; set; } = null;

        public bool Continuous { get; set; } = false;

        public int VideoResourceID { get; set; } = 0;

        public List<int> RetrieveSetIDs { get; set; } = new List<int>();

        #endregion
    }
}