using System;
using System.Collections.Generic;
using System.IO;
using RePlay.Entity;
using RePlay_Exercises;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Android.App;
using System.Linq;

namespace RePlay.Manager
{
#pragma warning disable CS0618 // Type or member is obsolete
    // Provides a singleton instance of the patient's current prescription
    public class PrescriptionManager
    {
        #region Private data members

        private bool newly_swapped_prescription = false;
        private Prescription current_prescription = new Prescription();
        private const string fileName = "prescription.json";

        /// <summary>
        /// Return the path of the prescription file
        /// </summary>
        private string FilePath
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

        #region Public properties

        /// <summary>
        /// This is the currently assigned prescription
        /// </summary>
        public Prescription CurrentPrescription
        {
            get
            {
                return current_prescription;
            }
            set
            {
                current_prescription = value;
            }
        }

        public bool NewlySwappedPrescription
        {
            get
            {
                return newly_swapped_prescription;
            }
            set
            {
                newly_swapped_prescription = value;
            }
        }

        #endregion

        #region Singleton Methods

        private static PrescriptionManager instance;

        /// <summary>
        /// Constructor
        /// </summary>
        private PrescriptionManager() 
        {
            //empty
        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static PrescriptionManager Instance 
        {
            get 
            {
                if (instance == null) 
                {
                    instance = new PrescriptionManager();
                }

                return instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load, parse, and add each prescribed exercise to the list
        /// </summary>
        public void LoadPrescription() 
        {
            //Clear the list of prescription items
            CurrentPrescription = new Prescription();

            if (!File.Exists(FilePath)) 
            {
                //If no prescription file exists, then save an empty prescription file
                //and then just exit the function
                SaveCurrentPrescription();
            }
            else
            {
                //Load in the contents of the prescription file
                using (var reader = new StreamReader(FilePath))
                {
                    string file_contents = reader.ReadToEnd();

                    //First, try to read a normal prescription object
                    try
                    {
                        CurrentPrescription = JsonConvert.DeserializeObject<Prescription>(file_contents);
                    }
                    catch (Exception ex)
                    {
                        //If there was an exception, then try doing the OLD way of just reading in a list of prescription items
                        //This should provide backwards compatibility until people update their prescriptions
                        CurrentPrescription = new Prescription();
                        try
                        {
                            CurrentPrescription.PrescriptionItems = JsonConvert.DeserializeObject<List<PrescriptionItem>>(file_contents);
                            if (CurrentPrescription.PrescriptionItems == null)
                            {
                                CurrentPrescription.PrescriptionItems = new List<PrescriptionItem>();
                            }
                        }
                        catch (Exception e)
                        {
                            //empty
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save the prescription list to a file for persistence
        /// </summary>
        public void SaveCurrentPrescription() 
        {
            //Create the folder if it does not exist
            new FileInfo(FilePath).Directory.Create();

            using (var writer = new StreamWriter(FilePath))
            {
                string json_string = JsonConvert.SerializeObject(CurrentPrescription);
                writer.Write(json_string);
            }
        }

        /// <summary>
        /// This method switchs the currently assigned prescription to be the referenced prescription
        /// </summary>
        public void SwitchPrescription(Prescription newPrescription)
        {
            if (!EqualToCurrentPrescription(newPrescription))
            {
                //Copy the prescription into the current prescription
                //We do NOT want to just copy the POINTER of the object.
                var json_serialized_prescription = JsonConvert.SerializeObject(newPrescription);
                CurrentPrescription = JsonConvert.DeserializeObject<Prescription>(json_serialized_prescription);

                //Save the current prescription
                SaveCurrentPrescription();

                //Set a flag indicating that the assignment has been freshly swapped out with a new one.
                newly_swapped_prescription = true;
            }
        }

        /// <summary>
        /// This method checks to see if the referenced prescription is equal to the currently assigned prescription
        /// </summary>
        public bool EqualToCurrentPrescription(Prescription prescription_to_check)
        {
            //Some simple null checks
            if (prescription_to_check == null ||
                prescription_to_check.PrescriptionItems == null ||
                CurrentPrescription == null ||
                CurrentPrescription.PrescriptionItems == null)
            {
                return false;
            }

            if (prescription_to_check.PrescriptionItems.Count != CurrentPrescription.PrescriptionItems.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < CurrentPrescription.PrescriptionItems.Count; i++)
                {
                    if (!CurrentPrescription.PrescriptionItems[i].ComparePrescriptionItemEquality(prescription_to_check.PrescriptionItems[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
