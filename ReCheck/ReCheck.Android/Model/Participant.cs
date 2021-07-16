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
using RePlay_Common;
using RePlay_Exercises;

namespace ReCheck.Droid.Model
{
    public class Participant : NotifyPropertyChangedObject
    {
        #region Private data members

        private string id = string.Empty;
        private List<ExerciseType> completed_exercises = new List<ExerciseType>();

        #endregion

        #region Constructor

        public Participant()
        {
            //empty
        }

        #endregion

        public string ParticipantID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                NotifyPropertyChanged("ParticipantID");
            }
        }

        public List<ExerciseType> ExercisesCompleted
        {
            get
            {
                return completed_exercises;
            }
            set
            {
                completed_exercises = value;
                NotifyPropertyChanged("ExercisesCompleted");
            }
        }

        public void AddExerciseToCompletedExercisesList (ExerciseType e)
        {
            ExercisesCompleted.Add(e);
            NotifyPropertyChanged("ExercisesCompleted");
        }

        public void ClearCompletedExercises ()
        {
            ExercisesCompleted.Clear();
            NotifyPropertyChanged("ExercisesCompleted");
        }

        public static bool IsOK_ParticipantID(string pid)
        {
            if (string.IsNullOrEmpty(pid) ||
                string.IsNullOrWhiteSpace(pid) ||
                !pid.Equals(CleanParticipantID(pid)) ||
                pid.Equals("Unknown", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static string CleanParticipantID(string pid)
        {
            pid = pid.Trim();
            pid = pid.ToUpper();
            pid = RemoveSpecialCharacters(pid);
            return pid;
        }

        private static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || 
                    (c >= 'A' && c <= 'Z') || 
                    (c >= 'a' && c <= 'z') || 
                    (c == '_') || 
                    (c == '-'))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}