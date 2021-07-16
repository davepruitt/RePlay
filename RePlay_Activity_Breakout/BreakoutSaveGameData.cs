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
using Microsoft.Xna.Framework.Graphics;
using RePlay_Common;

namespace RePlay_Activity_Breakout
{
    public static class BreakoutSaveGameData
    {
        #region Private data members
        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            CollisionPacketSection = 3,
            RebaselinePacketSection = 4,
            PowerUpCapturePacketSection = 5,
            PowerUpAppearedPacketSection = 6,
            PowerUpMissedPacketSection = 7,
            LevelFinishedPacketSection = 8,
            LevelStartPacketSection = 9,
        }

        private const int breakout_game_data_file_version = 2;
        private const string breakout_file_path = "Breakout/GameData/";
        private static int current_frame_num = 1;

        #endregion

        #region Public methods

        public static void SaveRebaselineEvent (BinaryWriter file_stream, BreakoutGame game, List<double> baseline_values)
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

        public static void SaveMetaData(BinaryWriter file_stream, BreakoutGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.MetaDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write file version
                    file_stream.Write(breakout_game_data_file_version); //int

                    //Difficulty (int)
                    file_stream.Write(game.gameDifficulty);

                    //level (int)
                    file_stream.Write(game.level);

                    //Write out the height and width of all the blocks
                    file_stream.Write(Block.BlockWidth);
                    file_stream.Write(Block.BlockHeight);

                    //Write the number of blocks that we have
                    file_stream.Write(game.blocks.Count);

                    //Write out the position, blocktype, and durability of all blocks
                    foreach (Block b in game.blocks)
                    {
                        file_stream.Write(b.position.X);
                        file_stream.Write(b.position.Y);

                        //Write out the blocktype as a char array
                        String blocktype = b.type.ToString();
                        int N = blocktype.Length;
                        file_stream.Write(N);
                        file_stream.Write(blocktype.ToCharArray());
                        file_stream.Write(b.Durability);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCurrentGameData(BinaryWriter file_stream, BreakoutGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    file_stream.Write(game.Paddle.current_data);

                    file_stream.Write(game.Paddle.position.X);
                    file_stream.Write(game.Paddle.position.Y);

                    file_stream.Write(game.Paddle.PaddleWidth);
                    file_stream.Write(game.Paddle.PaddleHeight);

                    //Write out the number of balls
                    file_stream.Write(game.balls.Count);
                    foreach (Ball b in game.balls)
                    {
                        //Write out the ball's unique ID
                        file_stream.Write(b.UniqueID.ToByteArray());

                        //Write out the ball's X position
                        file_stream.Write(b.position.X);

                        //Write out the ball's Y position
                        file_stream.Write(b.position.Y);

                        //Write out the ball's speed
                        file_stream.Write(b.Speed);

                        //Write out the ball's radius
                        file_stream.Write(b.Radius);
                    }

                    //Increment frame counter
                    current_frame_num++;
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveCollisionData(BinaryWriter file_stream, Block collidedBlock)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID indicating this packet has information about a ball-block collision
                    file_stream.Write((int)FileSave_SectionTypes.CollisionPacketSection);

                    //Write the time of the collission
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write the x and y position of the block
                    file_stream.Write(collidedBlock.position.X);
                    file_stream.Write(collidedBlock.position.Y);

                    //Write out the current durability of the block (this is the durability AFTER the ball has hit it)
                    file_stream.Write(collidedBlock.Durability);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SavePowerUpAppeared(BinaryWriter file_stream, string powerup_name)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID indicating this packet has information about the activation of a power-up
                    file_stream.Write((int)FileSave_SectionTypes.PowerUpAppearedPacketSection);

                    //Write the time of the power-up activation
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write the name of the power-up being activated
                    int N = powerup_name.Length;
                    file_stream.Write(N);
                    file_stream.Write(Encoding.ASCII.GetBytes(powerup_name));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SavePowerUpMissed(BinaryWriter file_stream, string powerup_name)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID indicating this packet has information about the activation of a power-up
                    file_stream.Write((int)FileSave_SectionTypes.PowerUpMissedPacketSection);

                    //Write the time of the power-up activation
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write the name of the power-up being activated
                    int N = powerup_name.Length;
                    file_stream.Write(N);
                    file_stream.Write(Encoding.ASCII.GetBytes(powerup_name));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SavePowerUpCapture (BinaryWriter file_stream, string powerup_name)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID indicating this packet has information about the activation of a power-up
                    file_stream.Write((int)FileSave_SectionTypes.PowerUpCapturePacketSection);

                    //Write the time of the power-up activation
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Write the name of the power-up being activated
                    int N = powerup_name.Length;
                    file_stream.Write(N);
                    file_stream.Write(Encoding.ASCII.GetBytes(powerup_name));
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveLevelStart (BinaryWriter file_stream, BreakoutGame game)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.LevelStartPacketSection);

                    //Write the date/time
                    file_stream.Write(MatlabCompatibility.ConvertDateTimeToMatlabDatenum(DateTime.Now));

                    //Difficulty (int)
                    file_stream.Write(game.gameDifficulty);

                    //level (int)
                    file_stream.Write(game.level);

                    //Write out the height and width of all the blocks
                    file_stream.Write(Block.BlockWidth);
                    file_stream.Write(Block.BlockHeight);

                    //Write the number of blocks that we have
                    file_stream.Write(game.blocks.Count);

                    //Write out the position, blocktype, and durability of all blocks
                    foreach (Block b in game.blocks)
                    {
                        file_stream.Write(b.position.X);
                        file_stream.Write(b.position.Y);

                        //Write out the blocktype as a char array
                        String blocktype = b.type.ToString();
                        int N = blocktype.Length;
                        file_stream.Write(N);
                        file_stream.Write(blocktype.ToCharArray());
                        file_stream.Write(b.Durability);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveLevelFinish (BinaryWriter file_stream)
        {
            try
            {
                if (file_stream != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID indicating this packet has information about the activation of a power-up
                    file_stream.Write((int)FileSave_SectionTypes.LevelFinishedPacketSection);

                    //Write the time of the power-up activation
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