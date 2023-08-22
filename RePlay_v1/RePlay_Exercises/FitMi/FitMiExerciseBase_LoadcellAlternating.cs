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

namespace RePlay_Exercises.FitMi
{
    public class FitMiExerciseBase_LoadcellAlternating : FitMiExerciseBase_Loadcell
    {
        #region Constructor

        public FitMiExerciseBase_LoadcellAlternating(Activity a, double gain)
            : base(a, gain)
        {
            //RepetitionsMode stuff
            ConvertSignalToVelocity = false;
            SinglePolarity = false;
            ForceAlternation = true;
        }

        #endregion
    }
}