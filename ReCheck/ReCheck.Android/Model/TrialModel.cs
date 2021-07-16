using System;
using System.Collections.Generic;

namespace ReCheck.Model
{
    /// <summary>
    /// A class that represents a single repetition trial
    /// </summary>
    public class TrialModel
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public TrialModel()
        {
            //empty
        }

        #endregion

        #region Public properties

        public List<double> TrialData { get; set; } = new List<double>();

        public TrialMotionDirection MotionDirection { get; set; } = TrialMotionDirection.Unknown;

        public double TrialMaximum { get; set; } = 0;

        public DateTime TrialStartTime { get; set; } = DateTime.Now;

        public DateTime TrialEndTime { get; set; } = DateTime.Now;

        #endregion
    }
}