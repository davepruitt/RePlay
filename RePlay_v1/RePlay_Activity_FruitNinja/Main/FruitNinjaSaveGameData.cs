using Android.App;
using Microsoft.Xna.Framework.Input.Touch;
using RePlay_Common;
using System;
using System.IO;
using System.Text;

namespace RePlay_Activity_FruitNinja.Main
{
    public static class FruitNinjaSaveGameData
    {
        #region Private data members
        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            FinalDataPacketSection = 3
        }

        private static int fruitninja_game_data_file_version = 2;
        private static string fruitninja_file_path = "FruitNinja/GameData/";
        private static int current_frame_num = 1;

        #endregion

        #region Public methods
        public static void SaveMetaData(BinaryWriter file_stream, FruitNinjaGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write file version
                    file_stream.Write(fruitninja_game_data_file_version); //int
                    file_stream.Write(game.Duration);    //int
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCurrentGameData(BinaryWriter file_stream, FruitNinjaGame game, ProjectileManager dojo, Knife blade, bool manager_data, TouchCollection touches)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write out the number of touches
                    file_stream.Write(touches.Count);

                    //Write out information for each touch (the first touch [0] is the only one that is used for game control)
                    foreach (TouchLocation touch in touches)
                    {
                        file_stream.Write(touch.Position.X);
                        file_stream.Write(touch.Position.Y);
                        file_stream.Write(touch.Id);
                        file_stream.Write((int)touch.State);
                    }

                    //Write out the current cut velocity
                    file_stream.Write(blade.CalculatedCutVelocity);

                    file_stream.Write(game.SecondsLeft);

                    // Save all fruit data
                    dojo.SaveCurrentFruitData(file_stream, manager_data);

                    // Save all knife data
                    blade.SaveCurrentKnifeData(file_stream);

                    current_frame_num++;
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveFinalData(BinaryWriter file_stream, ProjectileManager dojo, Knife blade)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.FinalDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    file_stream.Write(dojo.TotalFruitCreated);
                    file_stream.Write(dojo.TotalFruitHit);
                    file_stream.Write(dojo.TotalBombsCreated);
                    file_stream.Write(dojo.TotalBombsHit);

                    file_stream.Write(blade.TotalSwipes);
                    file_stream.Write(blade.Score);
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