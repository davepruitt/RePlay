using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_Common;

namespace RePlay_VNS_Triggering
{
    public class VNSAlgorithm_Standard : IVNSAlgorithmNumerical
    {
        #region Private data members

        VNSAlgorithmParameters parameters = new VNSAlgorithmParameters();

        private double vns_most_recent_final_value = 0;
        private bool vns_most_recent_should_we_trigger = false;
        private List<double> vns_lookback_values = new List<double>();
        private List<DateTime> vns_lookback_values_datetimes = new List<DateTime>();
        private List<double> vns_buffer = new List<double>();
        private List<DateTime> vns_buffer_datetimes = new List<DateTime>();
        private DateTime vns_most_recent_stimulation_time = DateTime.MinValue;
        private double vns_current_positive_threshold = 0;
        private double vns_current_negative_threshold = 0;

        //The following variables are exclusively for selectivity adjustment based on the desired ISI
        private List<DateTime> vns_stim_times = new List<DateTime>();
        private DateTime most_recent_selectivity_adjustment_time = DateTime.MinValue;
        private TimeSpan selectivity_adjustment_period = TimeSpan.FromSeconds(1.0);
        private TimeSpan saturation_time = TimeSpan.FromSeconds(10);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public VNSAlgorithm_Standard()
        {
            //empty
        }

        #endregion

        #region Interface implementation

        public string VNS_Algorithm_Name
        {
            get
            {
                return "TxBDC Standard VNS Algorithm";
            }
        }

        public VNSAlgorithmParameters Parameters
        {
            get
            {
                return parameters;
            }
        }

        public void Initialize_VNS_Algorithm(DateTime datetime, VNSAlgorithmParameters p)
        {
            parameters = p;

            vns_buffer.Clear();
            vns_lookback_values.Clear();
            vns_buffer_datetimes.Clear();
            vns_lookback_values_datetimes.Clear();
        }

        public bool Determine_VNS_Triggering(DateTime datetime, double signal)
        {
            return Determine_VNS_Triggering(datetime, signal, 0);
        }

        public bool Determine_VNS_Triggering(DateTime datetime, double signal, double compensation_signal)
        {
            return Determine_VNS_Triggering(datetime, signal, compensation_signal, 0);
        }

        public bool Determine_VNS_Triggering(DateTime datetime, double signal, double compensation_signal, double game_signal)
        {
            //Initialize the result to false
            bool should_we_trigger = false;

            //Grab the current datetime
            var latest_datetime = DateTime.Now;

            //If this is the very first value (and thus a vns stimulation has not yet happened),
            //Then set the "most recent" vns stimulation time to be the current time.
            //This effectively blocks the algorithm from stimulating within the first several seconds
            //of activity (based on what the "minimum ISI" is set to).
            if (vns_most_recent_stimulation_time == DateTime.MinValue)
            {
                vns_most_recent_stimulation_time = latest_datetime;
            }

            //Add the latest signal value to the buffer
            vns_buffer.Add(signal);
            vns_buffer_datetimes.Add(latest_datetime);

            //Remove old buffer values
            int first_idx_to_keep = vns_buffer_datetimes.FindIndex(x => x >= (latest_datetime - parameters.SmoothingWindow));
            if (vns_buffer_datetimes.Count > 0 && first_idx_to_keep > 0)
            {
                vns_buffer.RemoveRange(0, first_idx_to_keep);
                vns_buffer_datetimes.RemoveRange(0, first_idx_to_keep);
            }

            //Smooth the buffer if requested to do so
            var smoothed_buffer = vns_buffer.ToList();
            if (parameters.Stage1_Smoothing != VNSAlgorithmParameters.SmoothingOptions.None)
            {
                if (parameters.Stage1_Smoothing == VNSAlgorithmParameters.SmoothingOptions.AveragingFilter)
                {
                    smoothed_buffer = TxBDC_Math.SmoothSignal(smoothed_buffer, smoothed_buffer.Count);
                }
            }

            //Now do the "stage 1" operation: subtract the mean, or take the diff, or take the gradient
            var stage1_result = smoothed_buffer.ToList();
            switch (parameters.Stage1_Operation)
            {
                case VNSAlgorithmParameters.Stage1_Operations.SubtractMean:
                    double mean = smoothed_buffer.Average();
                    stage1_result = smoothed_buffer.Select(x => x - mean).ToList();
                    break;
                case VNSAlgorithmParameters.Stage1_Operations.Derivative:
                    stage1_result = TxBDC_Math.Diff(smoothed_buffer, false);
                    break;
                case VNSAlgorithmParameters.Stage1_Operations.Gradient:
                    stage1_result = TxBDC_Math.Gradient(smoothed_buffer);
                    break;
                default:
                    break;
            }

            //Now let's see if we need to smooth the result of the stage 1 operation
            smoothed_buffer = stage1_result.ToList();
            if (parameters.Stage2_Smoothing != VNSAlgorithmParameters.SmoothingOptions.None)
            {
                if (parameters.Stage2_Smoothing == VNSAlgorithmParameters.SmoothingOptions.AveragingFilter)
                {
                    smoothed_buffer = TxBDC_Math.SmoothSignal(smoothed_buffer, smoothed_buffer.Count);
                }
            }

            //Now let's do the stage 2 operation
            var stage2_result = smoothed_buffer.LastOrDefault();
            switch (parameters.Stage2_Operation)
            {
                case VNSAlgorithmParameters.Stage2_Operations.RMS:
                    if (smoothed_buffer.Count > 0)
                    {
                        stage2_result = Math.Sqrt(
                            smoothed_buffer.Select(x => Math.Pow(x, 2)).Sum() / Convert.ToDouble(smoothed_buffer.Count)
                        );
                    }
                    else
                    {
                        stage2_result = 0;
                    }
                    break;
                case VNSAlgorithmParameters.Stage2_Operations.SignedRMS:
                    if (smoothed_buffer.Count > 0)
                    {
                        stage2_result = Math.Sqrt(
                            smoothed_buffer.Select(x => Math.Pow(x, 2)).Sum() / Convert.ToDouble(smoothed_buffer.Count)
                        );
                        stage2_result *= Math.Sign(smoothed_buffer.Average());
                    }
                    else
                    {
                        stage2_result = 0;
                    }
                    break;
                case VNSAlgorithmParameters.Stage2_Operations.Mean:
                    if (smoothed_buffer.Count > 0)
                    {
                        stage2_result = smoothed_buffer.Average();
                    }
                    else
                    {
                        stage2_result = 0;
                    }
                    break;
                case VNSAlgorithmParameters.Stage2_Operations.Sum:
                    if (smoothed_buffer.Count > 0)
                    {
                        stage2_result = smoothed_buffer.Sum();
                    }
                    else
                    {
                        stage2_result = 0;
                    }
                    break;
                default:
                    break;
            }

            //Save this as the "most recent final value"
            vns_most_recent_final_value = stage2_result;

            //Break out of this function and return false if the new value is below the noise floor. Do not continue.
            if (Math.Abs(stage2_result) < parameters.NoiseFloor)
            {
                return false;
            }

            //Now take the result of the stage 2 operation and place it in the vns lookback buffer
            vns_lookback_values.Add(stage2_result);
            vns_lookback_values_datetimes.Add(latest_datetime);

            //Remove old values from the buffer
            if (parameters.LookbackWindowExpirationPolicy == VNSAlgorithmParameters.BufferExpirationOptions.TimeLimit)
            {
                first_idx_to_keep = vns_lookback_values_datetimes.FindIndex(x => x >= (latest_datetime - parameters.LookbackWindow));
                if (vns_lookback_values_datetimes.Count > 0 && first_idx_to_keep > 0)
                {
                    vns_lookback_values.RemoveRange(0, first_idx_to_keep);
                    vns_lookback_values_datetimes.RemoveRange(0, first_idx_to_keep);
                }
            }
            else if (parameters.LookbackWindowExpirationPolicy == VNSAlgorithmParameters.BufferExpirationOptions.TimeCapacity)
            {
                //Quickly calculate a crude measure of the inter-sampling interval
                //While this value should theoretically be constant, in practice it can be variable
                //We will calculate the median ISI based on the latest 6 samples. I chose 6 samples simply
                //because that gives us 5 intervals, and 5 intervals seems like "enough" intervals to calculate
                //a median ISI. 1 or 2 bad intervals wouldn't throw it off. And if we used more intervals it would
                //just require more CPU time.
                if (vns_lookback_values.Count >= 6)
                {
                    var latest_timestamps = vns_lookback_values_datetimes.TakeLast(6).ToList();
                    var diff_latest_timestamps = TxBDC_Math.DiffDateTime(latest_timestamps).Select(x => x.TotalMilliseconds).ToList();
                    var median_interval_ms = TxBDC_Math.Median(diff_latest_timestamps);
                    
                    //Now that we have a measure of the median ISI, let's use that to calculate how many samples our buffer should be
                    var buffer_size_limit = Convert.ToInt32(Math.Round(parameters.LookbackWindow.TotalMilliseconds / median_interval_ms));

                    //Now that we know our buffer size, let's limit the capacity of our buffer to be that size
                    vns_lookback_values.LimitTo(buffer_size_limit, true);
                    vns_lookback_values_datetimes.LimitTo(buffer_size_limit, true);
                }
            }
            else
            {
                //Limit the buffer size to the defined numeric capacity
                vns_lookback_values.LimitTo(parameters.LookbackWindowCapacity, true);
                vns_lookback_values_datetimes.LimitTo(parameters.LookbackWindowCapacity, true);
            }
            
            //Now calculate stimulation thresholds
            vns_current_positive_threshold = Math.Max(parameters.NoiseFloor, TxBDC_Math.Percentile(
                vns_lookback_values.Where(x => x >= parameters.NoiseFloor).ToArray(), parameters.Selectivity));
            vns_current_negative_threshold = Math.Min(-parameters.NoiseFloor, TxBDC_Math.Percentile(
                vns_lookback_values.Where(x => x < -parameters.NoiseFloor).ToArray(), 1 - parameters.Selectivity));

            //Now determine whether to stimulate
            if (((stage2_result >= vns_current_positive_threshold && parameters.TriggerOnPositive) ||
                (stage2_result <= vns_current_negative_threshold && parameters.TriggerOnNegative)) &&
                (latest_datetime >= (vns_most_recent_stimulation_time + parameters.Minimum_ISI)))
            {
                vns_most_recent_stimulation_time = latest_datetime;
                should_we_trigger = true;
            }

            vns_most_recent_should_we_trigger = should_we_trigger;

            if (parameters.SelectivityControlledByDesiredISI)
            {
                AdjustSelectivityBasedUponDesiredISI(latest_datetime, should_we_trigger);
            }
                
            return should_we_trigger;
        }

        private void AdjustSelectivityBasedUponDesiredISI(DateTime current_datetime, bool should_we_trigger)
        {
            if (vns_stim_times.Count == 0)
            {
                //If there are currently no timestamps in the vns_stim_times list, let's seed it with the current
                //time. This effectively makes it so that the first datetime in the list is the time at which
                //the session began.
                vns_stim_times.Add(current_datetime);
            }
            else if (should_we_trigger)
            {
                //If the algorithm decided to trigger, store that information in our list of trigger times.
                vns_stim_times.Add(current_datetime);
            }

            //Now check to see if it is time to make an adjustment to the selectivity
            if (current_datetime >= (most_recent_selectivity_adjustment_time + selectivity_adjustment_period))
            {
                //If so...
                double current_isi = 0;

                //Calculate the current mean ISI
                if (vns_stim_times.Count > 1)
                {
                    var intervals = TxBDC_Math.DiffDateTime(vns_stim_times);
                    current_isi = intervals.Select(x => x.TotalMilliseconds).Average();
                }
                else if (vns_stim_times.Count == 1)
                {
                    current_isi = (current_datetime - vns_stim_times[0]).TotalMilliseconds;

                    //If no real stimulations have happened yet, and the current ISI is less than 
                    //the desired ISI, then just return from this function and don't do anything else.
                    //We will start calculating things once the first ISI exceeds the desired ISI, or once
                    //we have a real stimulation to work off of.
                    if (current_isi < parameters.Desired_ISI.TotalMilliseconds)
                    {
                        return;
                    }
                }

                //Calculate the difference between current projected isi and the desired isi
                var isi_diff = parameters.Desired_ISI.TotalMilliseconds - current_isi;

                //Calculate the learning rate
                double learning_rate = 0;
                if (isi_diff < -saturation_time.TotalMilliseconds)
                {
                    learning_rate = -1.0;
                }
                else if (isi_diff > saturation_time.TotalMilliseconds)
                {
                    learning_rate = 1.0;
                }
                else
                {
                    learning_rate = isi_diff / saturation_time.TotalMilliseconds;
                }

                //Calculate the new selectivity
                double new_selectivity = parameters.Selectivity + (learning_rate / 100.0);

                //Assign the new selectivity to the parameters structure
                parameters.Selectivity = new_selectivity;
            }
        }

        public void Flush_VNS_Buffers()
        {
            vns_most_recent_final_value = 0;
            vns_most_recent_should_we_trigger = false;
            vns_lookback_values.Clear();
            vns_lookback_values_datetimes.Clear();
            vns_buffer.Clear();
            vns_buffer_datetimes.Clear();
            vns_most_recent_stimulation_time = DateTime.Now;
        }

        public void Save_VNS_Algorithm_Information(BinaryWriter writer)
        {
            List<byte> vns_algorithm_parameters_to_save = parameters.SaveVNSAlgorithmParameters();

            writer.Write(vns_algorithm_parameters_to_save.Count);
            writer.Write(vns_algorithm_parameters_to_save.ToArray());
            writer.Flush();
        }

        /// <summary>
        /// Returns the current VNS signal buffer for purposes of plotting on the screen
        /// </summary>
        public List<double> Plotting_Get_VNS_Signal()
        {
            return vns_lookback_values;
        }

        /// <summary>
        /// Returns the latest calculated value
        /// </summary>
        public double Plotting_Get_Latest_Calculated_Value()
        {
            return vns_most_recent_final_value;
        }

        /// <summary>
        /// Returns the current VNS signal noise threshold for purposes of plotting on the screen
        /// </summary>
        public double Plotting_Get_Noise_Threshold()
        {
            return parameters.NoiseFloor;
        }

        /// <summary>
        /// Returns the current VNS negative threshold for purposes of plotting on the screen
        /// </summary>
        public double Plotting_Get_VNS_Negative_Threshold()
        {
            return vns_current_negative_threshold;
        }

        /// <summary>
        /// Returns the current VNS positive threshold for purposes of plotting on the screen
        /// </summary>
        public double Plotting_Get_VNS_Positive_Threshold()
        {
            return vns_current_positive_threshold;
        }

        #endregion
    }
}