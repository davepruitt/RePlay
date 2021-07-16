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
using FitMiAndroid;

namespace RePlay_Exercises.FitMi
{
    public class FitMiExerciseBase_Movement : FitMiExerciseBase
    {
        #region Private data members

        protected List<double> baseline_quaternion = new List<double>() { 0, 0, 0, 0 };
        protected bool reset_exercise_flag = false;

        protected List<double> bg_reference_quaternion = new List<double>() { 0, 0, 0, 0 };

        protected enum bg_generic_movement_bidirectional_states
        {
            wait,
            discovery_belowthresh,
            discovery_abovethresh,
            operational
        }

        protected bg_generic_movement_bidirectional_states bg_movement_bidirectional_state = bg_generic_movement_bidirectional_states.wait;
        protected double bg_movement_bidirectional_distance_thresh = 0.3;
        protected double bg_movement_bidirectional_maxdistance = 0;


        #endregion

        #region Constructor

        public FitMiExerciseBase_Movement(Activity a, double gain)
            : base(a, gain)
        {
            //Repetitions Mode stuff
            ConvertSignalToVelocity = false;
            SinglePolarity = true;
            Instruction = "Move";
        }

        #endregion

        #region Methods

        public override List<double> RetrieveBaselineData()
        {
            return baseline_quaternion;
        }

        public override void EnableBaselineDataCollection(bool enable)
        {
            //empty
        }

        public override bool ResetExercise(bool long_reset = false)
        {
            reset_exercise_flag = true;
            return true;
        }

        public override void Update()
        {
            base.Update();

            List<double> qn = FitMi_Controller.PuckPack0.Quat.ToList();
            if (reset_exercise_flag)
            {
                baseline_quaternion = qn.ToList();
                reset_exercise_flag = false;
                bg_movement_bidirectional_state = bg_generic_movement_bidirectional_states.discovery_belowthresh;
                bg_movement_bidirectional_maxdistance = 0;
            }

            double quat_distance = Quaternion.quaternion_distance(qn, baseline_quaternion);

            if (!SinglePolarity)
            {
                if (bg_movement_bidirectional_state == bg_generic_movement_bidirectional_states.wait)
                {
                    //empty
                }
                else if (bg_movement_bidirectional_state == bg_generic_movement_bidirectional_states.discovery_belowthresh)
                {
                    if (quat_distance >= bg_movement_bidirectional_distance_thresh)
                    {
                        bg_movement_bidirectional_state = bg_generic_movement_bidirectional_states.discovery_abovethresh;
                    }
                }
                else if (bg_movement_bidirectional_state == bg_generic_movement_bidirectional_states.discovery_abovethresh)
                {
                    if (quat_distance > bg_movement_bidirectional_maxdistance)
                    {
                        bg_movement_bidirectional_maxdistance = quat_distance;

                        bg_reference_quaternion = Quaternion.q_mult(qn,
                            Quaternion.q_conjugate(baseline_quaternion));
                        if (bg_reference_quaternion[0] < 0)
                        {
                            bg_reference_quaternion[0] *= -1.0;
                            bg_reference_quaternion[1] *= -1.0;
                            bg_reference_quaternion[2] *= -1.0;
                            bg_reference_quaternion[3] *= -1.0;
                        }
                    }

                    if (quat_distance < bg_movement_bidirectional_distance_thresh)
                    {
                        bg_movement_bidirectional_state = bg_generic_movement_bidirectional_states.operational;
                    }
                }
                else if (bg_movement_bidirectional_state == bg_generic_movement_bidirectional_states.operational)
                {
                    var pn = Quaternion.q_mult(qn, Quaternion.q_conjugate(baseline_quaternion));
                    if (pn[0] < 0)
                    {
                        pn[0] *= -1.0;
                        pn[1] *= -1.0;
                        pn[2] *= -1.0;
                        pn[3] *= -1.0;
                    }

                    var dp = Quaternion.quaternion_dot_product_no_w(pn, bg_reference_quaternion);
                    if (dp < 0)
                    {
                        quat_distance *= -1.0;
                    }
                }
            }

            //Calculate the value normalized to the range (and the range is sensitivity dependent)
            CurrentActualValue = (quat_distance * 100.0);
        }

        #endregion        
    }
}