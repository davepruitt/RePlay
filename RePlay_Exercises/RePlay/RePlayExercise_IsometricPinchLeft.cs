using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_DeviceCommunications;

namespace RePlay_Exercises.RePlay
{
    public class RePlayExercise_IsometricPinchLeft : RePlayExercise_IsometricPinch
    {
        #region Constructor

        public RePlayExercise_IsometricPinchLeft(ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            //empty
        }

        public RePlayExercise_IsometricPinchLeft(Activity a, double gain)
            : base(a, gain)
        {
            //empty
        }

        #endregion

        public override bool DoesDeviceMatch()
        {
            if (ReplayController != null)
            {
                return (ReplayController.CurrentDeviceType == ReplayDeviceType.Pinch_Left);
            }
            else
            {
                return false;
            }
        }
    }
}