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

namespace RePlay_VNS_Triggering
{
    /// <summary>
    /// A general interface that can be used to implement a variety of VNS algorithms 
    /// for use within the RePlay app environment.
    /// </summary>
    public interface IVNSAlgorithm
    {
        /// <summary>
        /// The human-readable name of the VNS algorithm being implemented
        /// </summary>
        string VNS_Algorithm_Name { get; }

        /// <summary>
        /// This method should take care of any initialization details for the VNS algorithm to
        /// properly function. We pass in the current date/time as a parameter so that it can
        /// have that information if needed.
        /// </summary>
        /// <param name="datetime">The current date and time</param>
        /// <param name="initialization_json">A JSON-formatted string containing any initialization parameters.</param>
        void Initialize_VNS_Algorithm(DateTime datetime, VNSAlgorithmParameters parameters);

        /// <summary>
        /// This method is meant to save "header" information about the VNS algorithm to a data file
        /// </summary>
        /// <param name="writer"></param>
        void Save_VNS_Algorithm_Information(BinaryWriter writer);
    }
}