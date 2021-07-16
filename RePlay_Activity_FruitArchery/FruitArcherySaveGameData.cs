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
using RePlay_Activity_FruitArchery.Main;
using RePlay_Common;
using RePlay_Exercises;

namespace RePlay_Activity_FruitArchery
{
    public static class FruitArcherySaveGameData
    {
        #region Private data members
        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            RebaselinePacketSection = 3,
        }

        public const int fruitarchery_game_data_file_version = 3;
        private static int current_frame_num = 1;

        #endregion

        #region Public methods

        public static void SaveRebaselineEvent(BinaryWriter file_stream, FruitArcheryGame game, List<double> baseline_values)
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

        public static void SaveMetaData(BinaryWriter file_stream, FruitArchery_Stage stage,
            int force_threshold_to_fire_arrow)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write out the file version
                    file_stream.Write(fruitarchery_game_data_file_version);

                    //Grab and write the difficulty (stage number) of the game
                    file_stream.Write((int)stage);

                    //Write out the force threshold required to shoot an arrow
                    file_stream.Write(force_threshold_to_fire_arrow);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCurrentGameData(BinaryWriter file_stream, FruitArchery_World world, ExerciseDeviceType device, double current_force_value)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Grab the arrow, bow, fruit objects for data writing
                    FruitArchery_Arrow arrow = world.GetArrow;
                    FruitArchery_Bow bow = world.GetBow;
                    FruitArchery_Fruit fruit = world.GetFruit;

                    //First we write the arrow data
                    if (arrow != null)
                    {
                        //Write a 1 if arrow exists
                        file_stream.Write(true);

                        if (arrow.IsArrowFlying)
                        {
                            file_stream.Write(true);

                            file_stream.Write(arrow.Position.X);
                            file_stream.Write(arrow.Position.Y);

                            file_stream.Write(arrow.Velocity.X);
                            file_stream.Write(arrow.Velocity.Y);

                        }
                        else { file_stream.Write(false); }

                    }
                    else { file_stream.Write(false); }

                    //Then write the bow data
                    if (bow != null)
                    {
                        file_stream.Write(true);

                        file_stream.Write(current_force_value);
                        file_stream.Write(bow.CalculatedPolarCoordinateBeforeGainApplied);

                        file_stream.Write(bow.Position.X);
                        file_stream.Write(bow.Position.Y);

                        file_stream.Write(bow.RotationRadians);
                    }
                    else { file_stream.Write(false); }

                    if (fruit != null)
                    {

                        file_stream.Write(true);

                        file_stream.Write(fruit.FruitPosition.X);
                        file_stream.Write(fruit.FruitPosition.Y);

                        file_stream.Write(fruit.FruitTextureRotation);

                        file_stream.Write(fruit.FruitTextureSize.X);
                        file_stream.Write(fruit.FruitTextureSize.Y);

                        file_stream.Write(fruit.HasBeenHitByArrow);
                    }
                    else { file_stream.Write(false); }

                    file_stream.Write(FruitArchery_GameSettings.CurrentGameScore);
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