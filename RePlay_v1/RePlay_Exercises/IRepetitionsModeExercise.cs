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

namespace RePlay_Exercises
{
    /// <summary>
    /// This interface describes properties used by exercises during the Repetitions Mode
    /// game (also known as Rep It Out). These properties are ONLY used for Repetitions Mode.
    /// </summary>
    public interface IRepetitionsModeExercise
    {
        /// <summary>
        /// This is effectively the "initiation threshold". The signal must cross this threshold
        /// for a trial to be initiated. Additionally, the signal must REMAIN over this threshold
        /// for AT LEAST the duration specified by MinimumTrialDuration for the trial to be counted.
        /// Otherwise, it will be considered noise. When the signal drops below this threshold,
        /// the trial is considered to be completed.
        /// </summary>
        double ReturnThreshold { get; set; }

        /// <summary>
        /// During a trial, if this threshold is crossed, then the trial be will be considered to be
        /// "successful". In Repetitions Mode, any "successful" trial is counted as a "repetition".
        /// </summary>
        double HitThreshold { get; set; }

        /// <summary>
        /// This indicates whether we expect the signal to be in the range of [0, +inf] or
        /// in the range of [-inf, +inf]. If SinglePolarity is true, then we expect the signal
        /// to be in the range of [0, +inf], and we will count trials as such. If SinglePolarity
        /// is false, then the signal could go in either direction from 0, so we must look for trials
        /// moving in the positive direction or in the negative direction.
        /// </summary>
        bool SinglePolarity { get; set; }

        /// <summary>
        /// This indicates whether Repetitions Mode should force the user to alternate between
        /// the directions in order to count trials. This property only has an effect if 
        /// "SinglePolarity" is set to FALSE. Otherwise, if "SinglePolarity" is TRUE, then
        /// "ForceAlternation" has NO EFFECT. Assuming that "SinglePolarity" is false, the
        /// following occurs: If "ForceAlternation" is true, then a user must
        /// alternate between positive and negative movements in order for trials to be counted.
        /// Otherwise, if "ForceAlternation" is false, the user can freely do either positive or 
        /// negative movements, but does not necessarily have to alternate between the two.
        /// </summary>
        bool ForceAlternation { get; set; }

        /// <summary>
        /// This is the text instruction that is displayed to the user on the screen when this
        /// exercise is being performed in Repetitions Mode.
        /// </summary>
        string Instruction { get; set; }

        /// <summary>
        /// This is the minimum amount of time the signal must exceed the ReturnThreshold for this
        /// to be considered a trial. If the signal exceeds the threshold for less time than
        /// this, then it will simply be considered noise.
        /// </summary>
        TimeSpan MinimumTrialDuration { get; set; }

        /// <summary>
        /// If ConvertSignalToVelocity is true, then RepetitionsMode will buffer the raw signal
        /// values and then calculate the derivative. In this way, we will be using the "velocity"
        /// signal rather than the raw signal while counting repetitions.
        /// </summary>
        bool ConvertSignalToVelocity { get; set; }
    }
}