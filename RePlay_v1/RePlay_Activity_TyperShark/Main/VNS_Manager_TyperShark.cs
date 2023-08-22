using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework.Input;

namespace RePlay_Activity_TyperShark.Main
{
    public class VNS_Manager_TyperShark
    {
        #region Private data members
        
        private List<DateTime> LookbackTimestamps = new List<DateTime>();
        private List<double> KeypressSpeedLookback = new List<double>();
        private List<double> AccuracyLookback = new List<double>();

        private List<DateTime> StimulationTimes = new List<DateTime>();
        private List<DateTime> KeypressTimes = new List<DateTime>();
        private List<double> AccuracyScores = new List<double>();

        #endregion

        #region Constructor

        public VNS_Manager_TyperShark ()
        {
            //empty
        }

        #endregion

        #region Public properties

        public bool Enabled { get; set; } = false;

        public double CurrentAccuracyScore { get; private set; } = 0;
        public double CurrentSpeedScore { get; private set; } = 0;
        public double CurrentWeightedScore { get; private set; } = 0;
        public DateTime PreviousStimulationTime { get; private set; } = DateTime.MinValue;

        public TimeSpan Minimum_Stimulation_ISI { get; set; } = TimeSpan.FromSeconds(8.0);
        public TimeSpan Desired_Median_Stimulation_ISI { get; set; } = TimeSpan.FromSeconds(15.0);
        public double StimulationSelectivity { get; set; } = 0.1;
        public double AccuracyWeight { get; set; } = 0.5;
        public double SpeedWeight { get; set; } = 0.5;
        public TimeSpan AnalysisLookbackWindow { get; set; } = TimeSpan.FromSeconds(30.0);
        public TimeSpan StimulationDeterminationWindow { get; set; } = TimeSpan.FromSeconds(2.0);
        
        #endregion

        #region Public methods

        public bool Determine_VNS_Triggering (Keys key, char input, char expected)
        {
            bool result = false;
            DateTime keypress_time = DateTime.Now;

            //Add this keypress and its accuracy score to the necessary lists
            KeypressTimes.Add(keypress_time);
            AccuracyScores.Add((input == expected) ? 1.0 : 0.0);

            //Grab the time of the last stimulation that occurred
            DateTime previous_stim = DateTime.MinValue;
            if (StimulationTimes.Count > 0)
            {
                previous_stim = StimulationTimes.Last();
            }

            PreviousStimulationTime = previous_stim;

            //Shorten the lists as necessary
            DateTime earliest_time_to_retain = keypress_time - StimulationDeterminationWindow;
            if (KeypressTimes.Count > 0)
            {
                int index_of_earliest_timestamp_to_keep = KeypressTimes.Select(x => (x >= earliest_time_to_retain) ? true : false).ToList().IndexOf(true);
                if (index_of_earliest_timestamp_to_keep >= 0)
                {
                    KeypressTimes = KeypressTimes.Skip(index_of_earliest_timestamp_to_keep).ToList();
                    AccuracyScores = AccuracyScores.Skip(index_of_earliest_timestamp_to_keep).ToList();
                }
            }

            //Grab the mean speed and accuracy in the determination window
            double accuracy_mean = AccuracyScores.Average();
            double mean_keypresses_per_second = Convert.ToDouble(KeypressTimes.Count) / StimulationDeterminationWindow.TotalSeconds;
            
            //Now add these values to the lookbacks
            LookbackTimestamps.Add(keypress_time);
            AccuracyLookback.Add(accuracy_mean);
            KeypressSpeedLookback.Add(mean_keypresses_per_second);
            
            DateTime earliest_lookback_time_to_retain = keypress_time - AnalysisLookbackWindow;
            if (LookbackTimestamps.Count > 0)
            {
                //Now reduce the size of the lookbacks as necessary
                int index_of_earliest_timestamp_to_keep = LookbackTimestamps.Select(x => (x >= earliest_lookback_time_to_retain) ? true : false).ToList().IndexOf(true);
                if (index_of_earliest_timestamp_to_keep >= 0)
                {
                    LookbackTimestamps = LookbackTimestamps.Skip(index_of_earliest_timestamp_to_keep).ToList();
                    KeypressSpeedLookback = KeypressSpeedLookback.Skip(index_of_earliest_timestamp_to_keep).ToList();
                    AccuracyLookback = AccuracyLookback.Skip(index_of_earliest_timestamp_to_keep).ToList();
                }
            }

            //Get the max speed over the lookback window
            var max_speed = KeypressSpeedLookback.Max();

            //Normalize the current typing speed to get it on a scale of 0 to 1
            var current_normalized_speed = mean_keypresses_per_second / max_speed;

            //Get the max accuracy over the lookback window
            var max_accuracy = AccuracyLookback.Max();

            //Normalize the current accuracy to the max accuracy over the lookback window
            var current_normalized_accuracy = accuracy_mean / max_accuracy;

            //Now let's do a weighted average of the accuracy and speed scores
            //var weighted_score = (SpeedWeight * current_normalized_speed) + (AccuracyWeight * current_normalized_accuracy);
            var weighted_score = (current_normalized_speed + current_normalized_accuracy) / 2.0;

            //Now determine if we should stimulate
            if ((keypress_time - previous_stim) >= Minimum_Stimulation_ISI)
            {
                if (weighted_score >= (1.0 - StimulationSelectivity))
                {
                    StimulationTimes.Add(keypress_time);
                    result = true;
                }
            }

            CurrentAccuracyScore = current_normalized_accuracy;
            CurrentSpeedScore = current_normalized_speed;
            CurrentWeightedScore = weighted_score;

            if (result) Log.Debug("VNS", "STIMULATE");

            return result;
        }

        #endregion
    }
}