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
using RePlay_Activity_TrafficRacer.Environment;
using RePlay_Activity_TrafficRacer.Vehicle;
using RePlay_Common;

namespace RePlay_Activity_TrafficRacer.Main
{
    public static class TrafficRacerSaveGameData
    {
        #region Private data members
        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            RebaselinePacketSection = 3,
            CrashEventPacketSection = 4,
            ReStartEventPacketSection = 5,
            CoinCaptureEventPacketSection = 6,
        }

        private const int trafficracer_game_data_file_version = 2;
        private const string trafficracer_file_path = "TrafficRacer/GameData/";
        private static int current_frame_num = 1;

        #endregion

        #region Public methods

        public static void SaveRebaselineEvent(BinaryWriter file_stream, TrafficGame game, List<double> baseline_values)
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

        public static void SaveMetaData(BinaryWriter file_stream, EnvironmentManager environment, TrafficGame trafficgame)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));


                    file_stream.Write(trafficracer_game_data_file_version); //int
                    file_stream.Write(TrafficGame.Difficulty); //float
                    file_stream.Write(trafficgame.Duration); //int
                    file_stream.Write(Road.NumLanes); //int
                    file_stream.Write(Road.LaneWidth); //float
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCurrentGameData(BinaryWriter file_stream, Player playercar, TrafficManager trafficinfo, EnvironmentManager environment, TrafficGame trafficgame, Road road, float movement)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Save the game score (int)
                    file_stream.Write(trafficgame.score);   //int

                    //Write the seconds left (double)
                    file_stream.Write(trafficgame.SecondsLeft); //double

                    //Write out the lateral movement signal 
                    file_stream.Write(movement);    //float

                    // Save all of the player's car data
                    playercar.SaveCurrentPlayerCarData(file_stream);

                    //Save all traffic data
                    trafficinfo.SaveCurrentTrafficData(file_stream);

                    //Save the coin data
                    environment.SaveCurrentCoinData(file_stream);

                    //Save which lane is highlighted
                    file_stream.Write(road.GetHighlightAtPlayerPos());

                    //Increment frame counter
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

        public static void SaveCrashEvent (BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information
                    file_stream.Write((int)FileSave_SectionTypes.CrashEventPacketSection);

                    //Write the timestamp
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveReStartEvent (BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information
                    file_stream.Write((int)FileSave_SectionTypes.ReStartEventPacketSection);

                    //Write the timestamp
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCoinCapture (BinaryWriter file_stream, Guid c)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information
                    file_stream.Write((int)FileSave_SectionTypes.CoinCaptureEventPacketSection);

                    //Write the timestamp
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write the coin's unique ID
                    file_stream.Write(c.ToByteArray());
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