using System;
using System.IO;
using System.Text;
using Android.Content.Res;
using Android.Graphics;

namespace RePlay.Entity
{
    public class Participant
    {
        #region Private static properties

        private const string participant_info_filename = "patient.dat";
        private const string participant_avatar_filename = "profile.jpg";
        private const string default_participant_id = "Unknown";
        private const string default_particiapnt_avatar_filename = "defaultProfile.jpg";

        private static string participant_info_filepath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return System.IO.Path.Combine(path, participant_info_filename);
            }
        }

        private static string participant_avatar_filepath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return System.IO.Path.Combine(path, participant_avatar_filename);
            }
        }

        #endregion

        #region Public properties

        public string SubjectID { get; set; } = string.Empty;

        public bool IsNewParticipant { get; set; } = true;

        public Bitmap Photo { get; set; } = null;

        #endregion

        #region Constructor

        public Participant()
        {
            //empty
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Verifies that the string passed as a parameter meets all requirements necessary to be a valid
        /// participant ID.
        /// </summary>
        public static bool ParticipantID_IsOK (string pid)
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

        /// <summary>
        /// Cleans all invalid characters from a participant ID string. This includes whitespace and special characters.
        /// This method also turns the entire string into uppercase characters.
        /// </summary>
        public static string CleanParticipantID(string pid)
        {
            pid = pid.Trim();
            pid = pid.ToUpper();
            pid = ParticipantID_RemoveSpecialCharacters(pid);
            return pid;
        }

        /// <summary>
        /// Loads the current participant from the participant file.
        /// </summary>
        public static Participant LoadParticipantFromFile (AssetManager assets)
        {
            if (!File.Exists(participant_info_filepath))
            {
                Participant defaultPatient = new Participant
                {
                    SubjectID = default_participant_id,
                    Photo = BitmapFactory.DecodeStream(assets.Open(default_particiapnt_avatar_filename))
                };

                SaveParticipantToFile(defaultPatient);

                return defaultPatient;
            }

            Participant patient = new Participant();

            using (var reader = new StreamReader(participant_info_filepath))
            {
                string subjectID = reader.ReadLine();
                patient.SubjectID = subjectID;
            }

            using (var reader = new StreamReader(participant_avatar_filepath))
            {
                patient.Photo = BitmapFactory.DecodeStream(reader.BaseStream);
            }

            return patient;
        }

        /// <summary>
        /// Saves the current participant to the participant file.
        /// </summary>
        public static void SaveParticipantToFile (Participant patient)
        {
            using (var writer = new StreamWriter(participant_info_filepath))
            {
                writer.WriteLine(patient.SubjectID);
            }

            var stream = new FileStream(participant_avatar_filepath, FileMode.Create);
            patient.Photo.Compress(Bitmap.CompressFormat.Jpeg, 80, stream);
            stream.Close();
        }

        #endregion

        #region Private static methods

        private static string ParticipantID_RemoveSpecialCharacters(string str)
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

        #endregion
    }
}
