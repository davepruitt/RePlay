using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RePlay.Entity;
using RePlay_Exercises;

namespace RePlay.Manager
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class SavedPrescriptionManager
    {
        #region Private data members

        const string fileName = "savedprescriptions.json";
        private List<Prescription> saved_prescriptions = new List<Prescription>();

        #endregion

        #region Public properties

        public List<Prescription> SavedPrescriptions
        {
            get
            {
                return saved_prescriptions;
            }
            set
            {
                saved_prescriptions = value;
            }
        }

        #endregion

        #region Singleton Methods

        private static SavedPrescriptionManager instance;

        /// <summary>
        /// Private constructor
        /// </summary>
        private SavedPrescriptionManager()
        {
            //empty
        }

        public static SavedPrescriptionManager Instance 
        {
            get
            {
                if(instance == null)
                {
                    instance = new SavedPrescriptionManager();
                }
                return instance;
            }
        }
        #endregion

        #region Methods
        // load, parse, and add each prescribed exercise to the list
        public void LoadPrescription()
        {
            SavedPrescriptions.Clear();

            if (!File.Exists(FilePath))
            {
                SavePrescriptions();
            }

            using (var reader = new StreamReader(FilePath))
            {
                string file_contents = reader.ReadToEnd();
                try
                {
                    SavedPrescriptions = JsonConvert.DeserializeObject<List<Prescription>>(file_contents);
                    if (SavedPrescriptions == null)
                    {
                        SavedPrescriptions = new List<Prescription>();
                    }
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        // save the prescription list to a file for persistence
        public void SavePrescriptions()
        {
            //Create the folder if it does not exist
            new FileInfo(FilePath).Directory.Create();

            using (var writer = new StreamWriter(FilePath))
            {
                string json_string = JsonConvert.SerializeObject(SavedPrescriptions);
                writer.Write(json_string);
            }
        }

        public bool CheckIfContainsCurrentPrescription()
        {
            foreach (Prescription p in SavedPrescriptions)
            {
                if (PrescriptionManager.Instance.EqualToCurrentPrescription(p))
                {
                    return true;
                }
            }

            return false;
        }

        // return the path of the prescription file
        string FilePath
        {
            get
            {
                string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                path = Path.Combine(path, "TxBDC_NotData");
                path = Path.Combine(path, "RePlay");
                path = Path.Combine(path, "Prescriptions");
                return Path.Combine(path, fileName);
            }
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
