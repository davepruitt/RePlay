using System;
using System.IO;
using Android.Content.Res;
using Android.Graphics;
using RePlay.Entity;

namespace RePlay.Manager
{
	// provide methods to save and load patient data
    public class PatientLoader
    {
        #region String constants

        const string fileName = "patient.dat";
        const string photoName = "profile.jpg";
        const string defaultSubjectID = "Unknown";
        const string defaultPhotoAssetName = "defaultProfile.jpg";

        #endregion

        #region Methods

        // loads the current state from the state file
        // this is distinct from other "manager" classes in that it returns
        // the loaded data rather than updating the state of the singleton
        public static Participant Load(AssetManager assets)
        {
            if (!File.Exists(FilePath))
            {
                Participant defaultPatient = new Participant
                {
                    SubjectID = defaultSubjectID,
                    Photo = BitmapFactory.DecodeStream(assets.Open(defaultPhotoAssetName))
                };

                Save(defaultPatient);

                return defaultPatient;
            }

            Participant patient = new Participant();

            using (var reader = new StreamReader(FilePath))
            {
                string subjectID = reader.ReadLine();
                patient.SubjectID = subjectID;
            }

            using (var reader = new StreamReader(PhotoPath)) {
                patient.Photo = BitmapFactory.DecodeStream(reader.BaseStream);
            }

            return patient;
        }

        // writes patient out to the patient file and photo file
        // note that this is distinct from other "manager" classes in that it
        // saves the Patient passed in rather than saving the singleton state
        public static void Save(Participant patient)
        {
            using (var writer = new StreamWriter(FilePath))
            {
                writer.WriteLine(patient.SubjectID);
            }

            var stream = new FileStream(PhotoPath, FileMode.Create);
            patient.Photo.Compress(Bitmap.CompressFormat.Jpeg, 80, stream);
            stream.Close();
        }

        // returns the file path of the patient file
        static string FilePath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return System.IO.Path.Combine(path, fileName);
            }
        }

        // returns the file path of the patient photo file
        static string PhotoPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return System.IO.Path.Combine(path, photoName);
            }
        }

        #endregion
    }
}
