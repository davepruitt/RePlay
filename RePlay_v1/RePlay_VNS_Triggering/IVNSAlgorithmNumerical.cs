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

namespace RePlay_VNS_Triggering
{
    /// <summary>
    /// This interface is specific to VNS algorithms that are numerical in nature (I.E. almost everything
    /// except TyperShark).
    /// </summary>
    public interface IVNSAlgorithmNumerical : IVNSAlgorithm
    {
        /// <summary>
        /// Used to access the algorithm's parameters
        /// </summary>
        VNSAlgorithmParameters Parameters { get; }

        /// <summary>
        /// This method passes in new values of the signal and the compensation signal to the 
        /// VNS algorithm, and then the algorithm returns back a true/false value indicating whether
        /// stimulation should occur based upon those new signal values.
        /// </summary>
        /// <param name="datetime">The current date and time</param>
        /// <param name="signal">A new value for the primary signal</param>
        /// <returns>A true/false value indicating whether to stimulate or not</returns>
        bool Determine_VNS_Triggering(DateTime datetime, double signal);

        /// <summary>
        /// This method passes in new values of the signal and the compensation signal to the 
        /// VNS algorithm, and then the algorithm returns back a true/false value indicating whether
        /// stimulation should occur based upon those new signal values.
        /// </summary>
        /// <param name="datetime">The current date and time</param>
        /// <param name="signal">A new value for the primary signal</param>
        /// <param name="compensation_signal">A new value for the "compensation signal"</param>
        /// <returns>A true/false value indicating whether to stimulate or not</returns>
        bool Determine_VNS_Triggering(DateTime datetime, double signal, double compensation_signal);

        /// <summary>
        /// This method passes in new values of the signal and the compensation signal to the 
        /// VNS algorithm, and then the algorithm returns back a true/false value indicating whether
        /// stimulation should occur based upon those new signal values.
        /// </summary>
        /// <param name="datetime">The current date and time</param>
        /// <param name="signal">A new value for the primary signal</param>
        /// <param name="compensation_signal">A new value for the "compensation signal"</param>
        /// <param name="game_signal">A game-specific signal value</param>
        /// <returns>A true/false value indicating whether to stimulate or not</returns>
        bool Determine_VNS_Triggering(DateTime datetime, double signal, double compensation_signal, double game_signal);

        /// <summary>
        /// Flushes the VNS buffers and instates a VNS timeout equal to the duration of the minimum ISI.
        /// </summary>
        void Flush_VNS_Buffers();

        /// <summary>
        /// Returns the current VNS signal buffer for purposes of plotting on the screen
        /// </summary>
        List<double> Plotting_Get_VNS_Signal();

        /// <summary>
        /// Returns the latest calculated value
        /// </summary>
        double Plotting_Get_Latest_Calculated_Value();

        /// <summary>
        /// Returns the current VNS signal noise threshold for purposes of plotting on the screen
        /// </summary>
        double Plotting_Get_Noise_Threshold();

        /// <summary>
        /// Returns the current VNS negative threshold for purposes of plotting on the screen
        /// </summary>
        double Plotting_Get_VNS_Negative_Threshold();

        /// <summary>
        /// Returns the current VNS positive threshold for purposes of plotting on the screen
        /// </summary>
        double Plotting_Get_VNS_Positive_Threshold();
    }
}