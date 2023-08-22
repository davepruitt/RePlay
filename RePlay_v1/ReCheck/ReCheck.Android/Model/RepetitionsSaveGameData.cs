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
using RePlay_Common;
using RePlay_Exercises;

namespace ReCheck.Model
{
    public static class RepetitionsSaveGameData
    {
        #region Private variables and enumerations

        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            RepHeaderPacketSection = 3,
            RebaselinePacketSection = 4,
            EndOfAttemptPacketSection = 5,
            HandednessPacketSection = 6,
        }

        private const int repmode_game_data_file_version = 5;
        private static int current_frame_num = 1;

        #endregion

        #region Public Methods

        public static void SaveRebaselineEvent(BinaryWriter file_stream, RepetitionsModel game, List<double> baseline_values)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write out the packet ID and timestamp of the rebaseline event
                    file_stream.Write((int)FileSave_SectionTypes.RebaselinePacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write out the number of baseline values we have
                    file_stream.Write((int)baseline_values.Count);

                    //Write out each baseline value
                    for (int i = 0; i < baseline_values.Count; i++)
                    {
                        file_stream.Write(baseline_values[i]);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveMetaData(BinaryWriter file_stream, int repcount, ThresholdType threshtype, ExerciseBase Exercise)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write file version
                    file_stream.Write(repmode_game_data_file_version);

                    file_stream.Write(repcount);

                    //Threshold Type: Write both the length of enum string then the char array
                    int N = threshtype.ToString().Length;
                    file_stream.Write(N);
                    file_stream.Write(threshtype.ToString().ToCharArray());

                    file_stream.Write(Exercise.ReturnThreshold);
                    file_stream.Write(Exercise.MinimumTrialDuration.TotalSeconds);

                    //New outputs as of file version 4
                    file_stream.Write(Exercise.HitThreshold);
                    file_stream.Write(Exercise.ConvertSignalToVelocity);
                    file_stream.Write(Exercise.SinglePolarity);
                    file_stream.Write(Exercise.ForceAlternation);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveHandedness(BinaryWriter file_stream, bool is_left_handed_session)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.HandednessPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
                    file_stream.Write(is_left_handed_session);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveRepHeaderData(BinaryWriter file_stream, DateTime trialstart, ExerciseBase Exercise)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.RepHeaderPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    file_stream.Write(Exercise.HitThreshold);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveEndOfAttemptAtCurrentTime(BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.EndOfAttemptPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveGameData(BinaryWriter file_stream, ExerciseBase Exercise)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    file_stream.Write(Exercise.CurrentNormalizedValue);
                    file_stream.Write(Exercise.CurrentActualValue);

                    current_frame_num++;
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void CloseFile(BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write the final frame number in the last int in the file
                    //This allows us to pre-allocate the read structure
                    file_stream.Write(current_frame_num);

                    //Close the file
                    file_stream.Close();
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }



        #endregion 
    }
}