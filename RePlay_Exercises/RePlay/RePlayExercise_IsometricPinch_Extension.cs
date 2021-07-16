using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_DeviceCommunications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RePlay_Exercises.RePlay
{
    public class RePlayExercise_IsometricPinch_Extension : RePlayExercise_IsometricPinch
    {
        #region Constructor

        public RePlayExercise_IsometricPinch_Extension(ReplayMicrocontroller c, Activity a, double gain)
            : base(c, a, gain)
        {
            SinglePolarity = true;
            ForceAlternation = false;
        }

        public RePlayExercise_IsometricPinch_Extension(Activity a, double gain)
            : base(a, gain)
        {
            SinglePolarity = true;
            ForceAlternation = false;
        }

        #endregion

        #region Overrides

        public override void Update()
        {
            //Call the base "pinch" exercise's Update method to do most of the legwork
            base.Update();

            //Now let's transform the value for this specific exercise
            CurrentActualValue = Math.Abs(Math.Min(0, CurrentActualValue));
        }

        #endregion
    }
}