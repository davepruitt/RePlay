using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExercise_MovementBidirectional : FitMiExerciseBase_Movement
    {
        #region Constructor

        public FitMiExercise_MovementBidirectional(Activity a, double gain)
            : base(a, gain)
        {
            //Repetitions Mode stuff
            ConvertSignalToVelocity = false;
            SinglePolarity = false;
            Instruction = "Move";
        }

        #endregion
    }
}