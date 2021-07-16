using System;
using System.IO;
using System.Text;
using Android.App;
using RePlay_Common;
using FitMiAndroid;
using RePlay_VNS_Triggering;

namespace RePlay_Exercises
{
    /// <summary>
    /// This class handles saving of FitMi puck data to storage
    /// </summary>
    public static class Exercise_SaveData
    {
        #region Private variables and enumerations

        private static int SaveFileVersion = 11;
        private static int frame_counter = 0;
        private static int stimulation_counter = 0;

        private enum FileSave_SectionTypes
        {
            FitMiPuckPacketSection = 1,
            RePlayDevicePacketSection = 2,
            StimulationTriggerSection = 3,
            TouchScreenPacketSection = 4,
            PCMPacketSection = 5,
            RePlayPinchDevicePacketSection = 6,
            RePlayIsometricCalibrationPacketSection = 7,
            RePlayRangeOfMotionCalibrationPacketSection = 8,
            HandednessDefinitionPacketSection = 9
        }

        #endregion

        #region Public static methods
        
        public static BinaryWriter OpenFileForSaving(Activity current_activity, 
            string file_name, 
            DateTime replay_build_date, 
            string version_name, 
            string version_code,
            string tablet_id,
            string subject_id, 
            string game_name, 
            string task_or_exercise_name, 
            double standard_range, 
            double gain, 
            double actual_range,
            bool from_prescription,
            VNSAlgorithmParameters vns_algorithm_parameters)
        {
            string date_string = DateTime.Now.ToString("yyyy_MM_dd");

            string external_file_storage = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

            string replay_path = Path.Combine(external_file_storage, "TxBDC");
            replay_path = Path.Combine(replay_path, subject_id);
            replay_path = Path.Combine(replay_path, date_string);
            replay_path = Path.Combine(replay_path, game_name);

            string file_path = Path.Combine(replay_path, file_name);

            //From the passed exercise string that is passed in, grab the device type 
            ExerciseType exercise = ExerciseTypeConverter.ConvertEnumMemberStringToExerciseType(task_or_exercise_name);
            ExerciseDeviceType device = ExerciseTypeConverter.GetExerciseDeviceType(exercise);

            //Create the folder if it does not exist
            new FileInfo(file_path).Directory.Create();

            //Open a handle to be able to write to the file
            var f_stream = new FileStream(file_path, FileMode.Create);
            BinaryWriter result = new BinaryWriter(f_stream, Encoding.ASCII);

            //First, write the file version
            result.Write(SaveFileVersion);

            //Next, write out the version of RePlay being used
            var replay_build_date_matlab = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(replay_build_date);
            result.Write(replay_build_date_matlab);

            //Next write out the version name
            int N = version_name.Length;
            result.Write(N);
            result.Write(version_name.ToCharArray());

            //Next write out the version code
            N = version_code.Length;
            result.Write(N);
            result.Write(version_code.ToCharArray());

            //Next, write out the tablet ID
            N = tablet_id.Length;
            result.Write(N);
            result.Write(tablet_id.ToCharArray());

            //Next, write the subject ID
            N = subject_id.Length;
            result.Write(N);
            result.Write(subject_id.ToCharArray());

            //Next, write the name of the game they are playing / activity they are doing
            N = game_name.Length;
            result.Write(N);
            result.Write(game_name.ToCharArray());

            //Next, write the name of the task and/or exercise
            N = task_or_exercise_name.Length;
            result.Write(N);
            result.Write(task_or_exercise_name.ToCharArray());

            //Write out the exercise device type
            string temp_device = ExerciseDeviceTypeConverter.ConvertExerciseDeviceTypeToDescription(device);
            N = temp_device.Length;
            result.Write(N);
            result.Write(temp_device.ToCharArray());

            //Next, write whether this is controller data or gamedata (0: controller, 1: game)
            result.Write(file_name.Contains("gamedata"));

            //Next, write a timestamp that indicates when this game/exercise is beginning
            DateTime session_start_time = DateTime.Now;
            var matlab_version_of_start_time = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(session_start_time);
            result.Write(matlab_version_of_start_time);

            //Write the standard range for the current exercise
            result.Write(standard_range);

            //Write the gain for the current exercise
            result.Write(gain);

            //Write the actual range for the current exercise (which is the standard range divided by the gain)
            result.Write(actual_range);

            //Write out whether this game was launched from the prescription or from the games page
            result.Write(from_prescription);

            //Determine what bytes we need to write out to the file header to describe the VNS algorithm parameters
            var vns_algorithm_params_output = vns_algorithm_parameters.SaveVNSAlgorithmParameters();

            //Write out what version of the vns algorithm parameters we are about to write
            result.Write(vns_algorithm_parameters.VNS_AlgorithmParameters_SaveVersion);

            //Write out how many bytes we are about to write
            result.Write(vns_algorithm_params_output.Count);

            //Write out the VNS algorithm parameter bytes
            result.Write(vns_algorithm_params_output.ToArray());

            //Reset current frame number
            frame_counter = 0;

            //Return the file handle
            return result;
        }

        public static void CloseFile (BinaryWriter file_stream)
        {
            if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
            {
                //Write out the stimuilation counter
                file_stream.Write(stimulation_counter);

                //Write out the current frame number so we preallocation sizes for data loading
                file_stream.Write(frame_counter);

                //Close the file
                file_stream.Close();
            }
        }

        public static void SaveCurrentTouchData(BinaryWriter fid, float touch_position_x, float touch_position_y)
        //public static void SaveCurrentTouchData (BinaryWriter fid, TouchCollection touchdata)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save a field that indicates the upcoming bytes are a packet of puck data
                int puck_packet_section_type = (int)FileSave_SectionTypes.TouchScreenPacketSection;
                fid.Write(puck_packet_section_type);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write the touch positions
                fid.Write(touch_position_x);
                fid.Write(touch_position_y);

                frame_counter += 1;
            }
        }

        public static void SaveCurrentPuckData (BinaryWriter fid, HIDPuckDongle d)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite && d != null && d.IsOpen)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int puck_packet_section_type = (int)FileSave_SectionTypes.FitMiPuckPacketSection;
                fid.Write(puck_packet_section_type);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the blue puck data
                WriteIndividualPuckData(fid, 1, d.PuckPack0);

                //Write out the yellow puck data
                WriteIndividualPuckData(fid, 2, d.PuckPack1);

                frame_counter += 1;
            }
        }

        public static void SaveRePlayExerciseData(BinaryWriter fid, double data)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int replay_exercise_data_section = (int)FileSave_SectionTypes.RePlayDevicePacketSection;
                fid.Write(replay_exercise_data_section);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the current value of the exercise
                fid.Write(data);

                //Increment frame counter to keep track of how many 
                frame_counter += 1;
            }
        }

        public static void SaveRePlayPinchExerciseData(BinaryWriter fid, double loadcell1, double loadcell2)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int replay_exercise_data_section = (int)FileSave_SectionTypes.RePlayPinchDevicePacketSection;
                fid.Write(replay_exercise_data_section);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the current value of the exercise
                fid.Write(loadcell1);
                fid.Write(loadcell2);

                //Increment frame counter to keep track of how many 
                frame_counter += 1;
            }
        }

        public static void SaveRePlayIsometricCalibrationData(BinaryWriter fid, double b1, double b2, double s1, double s2)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int replay_exercise_data_section = (int)FileSave_SectionTypes.RePlayIsometricCalibrationPacketSection;
                fid.Write(replay_exercise_data_section);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the current value of the exercise
                fid.Write(b1);
                fid.Write(b2);
                fid.Write(s1);
                fid.Write(s2);
            }
        }

        public static void SaveRePlayRangeOfMotionCalibrationData(BinaryWriter fid, double baseline)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int replay_exercise_data_section = (int)FileSave_SectionTypes.RePlayRangeOfMotionCalibrationPacketSection;
                fid.Write(replay_exercise_data_section);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the current value of the exercise
                fid.Write(baseline);
            }
        }

        public static void SaveHandednessDefinition(BinaryWriter fid, bool is_left_handed_session)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int replay_exercise_data_section = (int)FileSave_SectionTypes.HandednessDefinitionPacketSection;
                fid.Write(replay_exercise_data_section);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                //Write out the current value of the exercise
                fid.Write(is_left_handed_session);
            }
        }

        public static void SaveStimulationTriggerAtCurrentTime (BinaryWriter fid)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int stim_trigger_section_type = (int)FileSave_SectionTypes.StimulationTriggerSection;
                fid.Write(stim_trigger_section_type);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now);
                fid.Write(t_stamp);

                stimulation_counter += 1;
            }
        }

        public static void SaveMessageFromReStoreService (BinaryWriter fid, PCM_DebugModeEvent_EventArgs msg)
        {
            if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
            {
                //Save an field that indicates the upcoming bytes are a packet of puck data
                int stim_trigger_section_type = (int)FileSave_SectionTypes.PCMPacketSection;
                fid.Write(stim_trigger_section_type);

                //Write a timestamp for this new field
                var t_stamp = MatlabCompatibility.ConvertDateTimeToMatlabDatenum(msg.MessageTimestamp);
                fid.Write(t_stamp);

                //Write the primary message text
                int N = msg.PrimaryMessage.Length;
                fid.Write(N);
                fid.Write(msg.PrimaryMessage.ToCharArray());

                //Write out key-value pairs of the secondary parts of the message, and write out how many key-value pairs
                //that there are
                var kvp_count = msg.SecondaryMessages.Count;
                fid.Write(kvp_count);

                //Iterate over each key-value pair and write it out
                foreach (var kvp in msg.SecondaryMessages)
                {
                    N = kvp.Key.Length;
                    fid.Write(N);
                    fid.Write(kvp.Key.ToCharArray());
                    N = kvp.Value.Length;
                    fid.Write(N);
                    fid.Write(kvp.Value.ToCharArray());
                }
            }
        }
        
        #endregion

        #region Private static methods

        private static void WriteIndividualPuckData (BinaryWriter fid, int puck_number, PuckPacket p)
        {
            //Write whether this is puck 1 or puck 2
            fid.Write(puck_number);

            //Write out the accelerometer data
            foreach (var a in p.Accelerometer) fid.Write(a);

            //Write out the gyrometer data
            foreach (var g in p.Gyrometer) fid.Write(g);

            //Write out the magnetometer data
            foreach (var m in p.Magnetometer) fid.Write(m);

            //Write out the quaternion data
            foreach (var q in p.Quat) fid.Write(q);
            
            //Write out the loadcell data
            fid.Write(p.Loadcell);

            //Write out the "touch" variable
            fid.Write(p.Touch);

            //Write out the battery status
            fid.Write(p.Battery);
        }

        #endregion
    }
}