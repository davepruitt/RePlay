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
using RePlay_Activity_SpaceRunner.Main;
using RePlay_Activity_SpaceRunner.UI;
using RePlay_Common;

namespace RePlay_Activity_SpaceRunner
{
    public static class SpaceRunnerSaveGameData
    {
        #region Private data members
        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            EndofAttemptPacketSection = 3,
            RebaselinePacketSection = 4,
            StartOfAttemptPacketSection = 5,
            CoinCapturePacketSection = 6,
        }

        private const int spacerunner_game_data_file_version = 3;
        private const string spacerunner_file_path = "SpaceRunner/GameData/";
        private static int current_frame_num = 1;

        #endregion

        #region Public methods

        public static void SaveRebaselineEvent(BinaryWriter file_stream, SpaceRunnerGame game, List<double> baseline_values)
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

        public static void SaveMetaData(BinaryWriter file_stream, SpaceRunnerGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write file version
                    file_stream.Write(spacerunner_game_data_file_version); //int

                    //Write the duration of the game
                    file_stream.Write(game.Duration);    //int
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCurrentGameData(BinaryWriter file_stream, SpaceRunnerGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write out the current normalized data from the device stream
                    file_stream.Write(game.GameInput.NormalizedExerciseData);

                    //Write out the current signal converted into a binary 1 or 0
                    file_stream.Write(game.GameInput.BinaryExerciseData);

                    game.Stage.SaveSpaceStageData(file_stream);

                    game.Road.SaveSpaceManagerData(file_stream);

                    file_stream.Write(game.Player.Score);

                    //Increment frame counter
                    current_frame_num++;
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveEndofAttemptData(BinaryWriter file_stream, SpaceRunnerGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.EndofAttemptPacketSection);

                    //Write the current date/time
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write out the final score
                    file_stream.Write(game.Scores[game.Scores.Count - 1]);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveStartOfAttemptData(BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write the packet ID
                    file_stream.Write((int)FileSave_SectionTypes.StartOfAttemptPacketSection);

                    //Write the current date/time
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCoinCapture (BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write the packet ID
                    file_stream.Write((int)FileSave_SectionTypes.CoinCapturePacketSection);

                    //Write the current date/time
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
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