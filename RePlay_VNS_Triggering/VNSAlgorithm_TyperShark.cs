using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RePlay_VNS_Triggering
{
    public class VNSAlgorithm_TyperShark : IVNSAlgorithmNumerical
    {
        #region Private data members

        VNSAlgorithmParameters parameters = new VNSAlgorithmParameters();

        private double vns_most_recent_final_value = 0;
        private bool vns_most_recent_should_we_trigger = false;
        private DateTime previous_sample = DateTime.MinValue;
        private List<double> vns_lookback_values = new List<double>();
        private List<double> vns_buffer = new List<double>();
        private DateTime vns_most_recent_stimulation_time = DateTime.MinValue;
        private double vns_current_positive_threshold = 0;
        private double vns_current_negative_threshold = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public VNSAlgorithm_TyperShark()
        {
            //empty
        }

        #endregion

        #region Interface implementation

        public string VNS_Algorithm_Name
        {
            get
            {
                return "TxBDC Standard VNS Algorithm (TyperShark Variant)";
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

            //Calculate the latest interval
            if (previous_sample == DateTime.MinValue)
            {
                //If this is the first sample, store the datetime that was passed in as a parameter.
                previous_sample = datetime;

                //Then return immediately. No further work to do on the first sample.
                return false;
            }
            else
            {
                //Calculate the interval for this sample
                signal = (datetime - previous_sample).TotalSeconds;

                //Store this datetime
                previous_sample = datetime;
            }

            //Take the inverse of the interval
            if (signal > 0)
            {
                signal = 1.0 / signal;
            }
            
            //Now take the result of the stage 2 operation and place it in the vns lookback buffer
            vns_lookback_values.Add(signal);

            //Remove old values
            int num_to_remove = vns_lookback_values.Count - parameters.TyperSharkLookbackSize;
            if (num_to_remove > 0)
            {
                vns_lookback_values.RemoveRange(0, num_to_remove);
            }

            //Now calculate stimulation thresholds
            vns_current_positive_threshold = TxBDC_Math.Percentile(vns_lookback_values.ToArray(), parameters.Selectivity);

            //Now determine whether to stimulate
            if ((signal >= vns_current_positive_threshold) &&
                latest_datetime >= (vns_most_recent_stimulation_time + parameters.Minimum_ISI))
            {
                vns_most_recent_stimulation_time = latest_datetime;
                should_we_trigger = true;
            }

            vns_most_recent_final_value = signal;
            vns_most_recent_should_we_trigger = should_we_trigger;

            return should_we_trigger;
        }

        public void Flush_VNS_Buffers()
        {
            vns_most_recent_final_value = 0;
            vns_most_recent_should_we_trigger = false;
            previous_sample = DateTime.MinValue;
            vns_lookback_values.Clear();
            vns_buffer.Clear();
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
            return 0;
        }

        /// <summary>
        /// Returns the current VNS negative threshold for purposes of plotting on the screen
        /// </summary>
        public double Plotting_Get_VNS_Negative_Threshold()
        {
            return 0;
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