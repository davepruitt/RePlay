using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_Common;
using Microsoft.Xna.Framework.Input;
using RePlay_VNS_Triggering;

namespace RePlay_Activity_TyperShark.Main
{
    public static class TyperSharkSaveGameData
    {
        #region Private data members

        private enum FileSave_SectionTypes
        {
            MetaDataPacketSection = 1,
            GameDataPacketSection = 2,
            PCMEventPacketSection = 3
        }
        
        private static int current_frame_num = 1;

        #endregion

        #region Public properties

        public static bool DidStimulationOccurFlag { get; set; } = false;

        #endregion

        #region Public methods
        
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

        public static void SaveCurrentGameData (BinaryWriter file_stream, GameLevel current_level, List<Keys> released_keys)
        {
            try
            {
                if (file_stream != null && current_level != null && file_stream.BaseStream != null && file_stream.BaseStream.CanWrite)
                {
                    //Write packet ID information and timestamp
                    file_stream.Write((int)FileSave_SectionTypes.GameDataPacketSection);

                    current_level.SaveCurrentLevelState(file_stream, released_keys);
                }
            }
            catch (ObjectDisposedException)
            {
                //empty
            }
        }

        public static void SaveMessageFromReStoreService(BinaryWriter fid, PCM_DebugModeEvent_EventArgs msg)
        {
            try
            {
                if (fid != null && fid.BaseStream != null && fid.BaseStream.CanWrite)
                {
                    //Save an field that indicates the upcoming bytes are a packet of puck data
                    int stim_trigger_section_type = (int)FileSave_SectionTypes.PCMEventPacketSection;
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
            catch (ObjectDisposedException)
            {
                //empty
            }
        }


        #endregion
    }
}