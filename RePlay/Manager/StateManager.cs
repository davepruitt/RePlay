using System;
using System.IO;

namespace RePlay.Manager
{
    // provide a singleton instance of the patient's progress through their prescription
    public class StateManager
    {
        #region Properties
        // in charge of saving state
        static StateManager instance;
        const string fileName = "state.dat";

        // saved data (which exercise they are on and at what time)
        long timestamp = 0;
        int index = 0;

        // returns the index, accounting for the user's 8hr time period to finish
        // the prescription expiring
        public int Index
        {
            get
            {
                LoadState();

                // After 8 hours, go to beginning of prescription
                long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (milliseconds - timestamp > (8 * 60 * 60 * 1000))        
                {
                    index = 0;
                }

                return index;
            }
        }

        #endregion

        #region Singleton Methods
        // private constructor
        StateManager()
        {

        }

        // returns the singleton instance of this class
        public static StateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StateManager();
                }
                return instance;
            }
        }
        #endregion

        #region Methods
        // loads the current state from the state file
        public void LoadState()
        {
            if (!File.Exists(FilePath))
            {
                SaveState();
            }

            using (var reader = new StreamReader(FilePath))
            {
                string line = reader.ReadLine();

                if(line != null)
                {
                    timestamp = long.Parse(line);
                    index = int.Parse(reader.ReadLine());
                }
            }
        }

        // writes state out to the save file
        void SaveState()
        {
            using (var writer = new StreamWriter(FilePath))
            {
                writer.WriteLine(timestamp);
                writer.WriteLine(index);
            }
        }

        // updates class properties with the new timestamp and index;
        // saves state to file
        public void UpdateState(long t, int i)
        {
            timestamp = t;
            index = i;
            SaveState();
        }

        // returns the file path of the state file
        string FilePath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(path, fileName);
            }
        }
        #endregion
    }
}
