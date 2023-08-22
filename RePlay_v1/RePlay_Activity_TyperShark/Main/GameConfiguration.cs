using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using RePlay_Activity_Common;
using RePlay_Exercises;
using RePlay_VNS_Triggering;

namespace RePlay_Activity_TyperShark.Main
{
    public static class GameConfiguration
    {
        #region Private data members

        private static string dictionary_file_name_left_handed = "typershark_dictionary_left.txt";
        private static string dictionary_file_name_right_handed = "typershark_dictionary_right.txt";
        private static string dictionary_file_name = "typershark_dictionary.txt";
        private static string sentence_dictionary_file_name = "typershark_dictionary_sentences.txt";
        private static string predators_json_file_name = "predators.json";
        private static string predators_asset_path = "Predators/";

        #endregion

        #region Public data members

        public static bool UseImpairedScaleFactors;
        public static bool AllowJellyfish = true;

        public static int CurrentScore = 0;

        public static int ScorePerLetter = 10;
        public static int ScorePerWord = 10;
        public static float VirtualScreenWidth = 2560.0f;
        public static float VirtualScreenHeight = 1600.0f;
        public static float VirtualScreenHalfWidth = 1280.0f;
        public static float VirtualScreenHalfHeight = 800.0f;
        public static int MarginTop = 250;
        public static int MarginBottom = 120;
        public static int MarginLeft = 50;
        public static int MarginRight = 50;
        public static int NumberOfLanes = 7;
        public static int ImpairedNumberOfLanes = 5;
        public static int PenaltyFreeZone = 300;
        public static int PenaltyOffsetDistance = 20;

        public static bool IsRePlayDebugMode = false;

        public static VNSAlgorithm_TyperShark VNS_Manager;
        public static PCM_Manager PCM_Manager;
        public static RePlay_Game_GameplayUI GameplayUI;

        public static List<string> GameDictionarySentences = new List<string>();
        public static List<string> GameDictionary = new List<string>();
        public static List<string> GameAlphabet = new List<string>();
        
        public static Dictionary<SharkType, Dictionary<SharkSpriteType, List<SpriteFrame>>> SharkSprites = new Dictionary<SharkType, Dictionary<SharkSpriteType, List<SpriteFrame>>>();
        public static Dictionary<SharkType, Dictionary<SharkSpriteType, List<SpriteFrame>>> HighlightedSharkSprites = new Dictionary<SharkType, Dictionary<SharkSpriteType, List<SpriteFrame>>>();
        
        private static string bubble_texture_1_asset_name = "bubble1";
        private static string bubble_texture_2_asset_name = "bubble2";
        private static string bubble_texture_3_asset_name = "bubble3";
        public static List<Texture2D> BubbleTextures = new List<Texture2D>();
        
        #endregion

        #region Public methods

        public static void InitializeStatics ()
        {
            IsRePlayDebugMode = false;
            UseImpairedScaleFactors = true;
            AllowJellyfish = true;

            CurrentScore = 0;
            ScorePerLetter = 10;
            ScorePerWord = 10;
            VirtualScreenWidth = 2560.0f;
            VirtualScreenHeight = 1600.0f;
            VirtualScreenHalfWidth = 1280.0f;
            VirtualScreenHalfHeight = 800.0f;
            MarginTop = 250;
            MarginBottom = 120;
            MarginLeft = 50;
            MarginRight = 50;
            NumberOfLanes = 7;
            ImpairedNumberOfLanes = 5;
            PenaltyFreeZone = 300;
            PenaltyOffsetDistance = 20;
            bubble_texture_1_asset_name = "bubble1";
            bubble_texture_2_asset_name = "bubble2";
            bubble_texture_3_asset_name = "bubble3";
            dictionary_file_name = "typershark_dictionary.txt";
            sentence_dictionary_file_name = "typershark_dictionary_sentences.txt";
            dictionary_file_name_left_handed = "typershark_dictionary_left.txt";
            dictionary_file_name_right_handed = "typershark_dictionary_right.txt";
            predators_json_file_name = "predators.json";
            predators_asset_path = "Predators/";
        }
        
        public static int GetLaneYPosition ( int lane_number, bool use_impaired_calculations = false )
        {
            int num_lanes = NumberOfLanes;
            if (use_impaired_calculations)
            {
                num_lanes = ImpairedNumberOfLanes;
            }

            var active_vertical_pixels = VirtualScreenHeight - MarginTop - MarginBottom;
            var pixels_per_lane = Convert.ToInt32(active_vertical_pixels / num_lanes);

            return (MarginTop + (pixels_per_lane * lane_number));
        }

        public static bool IsHardwareKeyboardAvailable (Activity current_activity)
        {
            return (current_activity.Resources.Configuration.Keyboard == Android.Content.Res.KeyboardType.Qwerty);
        }

        public static void LoadGameDictionary (Activity a, ExerciseType exerciseType = ExerciseType.Keyboard_Typing)
        {
            //Clear the dictionaries
            GameDictionary.Clear();
            GameDictionarySentences.Clear();
            GameAlphabet.Clear();

            //Check to see which dictionary we should load
            string dictionary_file_to_load = dictionary_file_name;
            switch (exerciseType)
            {
                case ExerciseType.Keyboard_Typing_LeftHanded:
                    dictionary_file_to_load = dictionary_file_name_left_handed;
                    break;
                case ExerciseType.Keyboard_Typing_RightHanded:
                    dictionary_file_to_load = dictionary_file_name_right_handed;
                    break;
            }

            //Load the dictionary of words
            if (GameDictionary.Count == 0)
            {
                using (StreamReader reader = new StreamReader(a.Assets.Open(dictionary_file_to_load)))
                {
                    while (!reader.EndOfStream)
                    {
                        string word = reader.ReadLine().Trim();
                        GameDictionary.Add(word);
                    }

                    reader.Close();
                }
            }

            //Also load the dictionary of sentences
            if (GameDictionarySentences.Count == 0)
            {
                using (StreamReader reader = new StreamReader(a.Assets.Open(sentence_dictionary_file_name)))
                {
                    while (!reader.EndOfStream)
                    {
                        string word = reader.ReadLine().Trim();
                        GameDictionarySentences.Add(word);
                    }

                    reader.Close();
                }
            }

            //Define the game alphabet (this is primarily used to define the set of characters that a word or sentence can BEGIN with)
            if (GameAlphabet.Count == 0)
            {
                GameAlphabet = new List<string>() {
                    "a", "b", "c", "d", "e", "f", "g", "h", "i",
                    "j", "k", "l", "m", "n", "o", "p", "q", "r",
                    "s", "t", "u", "v", "w", "x", "y", "z",
                    "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
                };
            }
        }

        public static void LoadBubbles (ContentManager Content)
        {
            //Clear the bubble assets
            BubbleTextures.Clear();

            //Load the bubble assets
            BubbleTextures.Add(Content.Load<Texture2D>(bubble_texture_1_asset_name));
            BubbleTextures.Add(Content.Load<Texture2D>(bubble_texture_2_asset_name));
            BubbleTextures.Add(Content.Load<Texture2D>(bubble_texture_3_asset_name));
        }

        public static void LoadPredators (ContentManager Content)
        {
            //Clear the dictionary if it has something in it for some reason
            SharkSprites.Clear();
            HighlightedSharkSprites.Clear();

            //Iterate over each shark type and sprite type to create the necessary keys
            foreach (SharkType s_type in Enum.GetValues(typeof(SharkType)))
            {
                SharkSprites[s_type] = new Dictionary<SharkSpriteType, List<SpriteFrame>>();
                HighlightedSharkSprites[s_type] = new Dictionary<SharkSpriteType, List<SpriteFrame>>();
                foreach (SharkSpriteType sp_type in Enum.GetValues(typeof(SharkSpriteType)))
                {
                    SharkSprites[s_type][sp_type] = new List<SpriteFrame>();
                    HighlightedSharkSprites[s_type][sp_type] = new List<SpriteFrame>();
                }
            }

            //Load the JSON file that contains the necessary asset names
            AssetManager assets = TyperSharkGame.Activity.Assets;
            using (StreamReader sr = new StreamReader(assets.Open(predators_json_file_name)))
            {
                var json_content = sr.ReadToEnd();
                JObject json_root = JObject.Parse(json_content);

                var costumes = json_root["costumes"];
                foreach (var costume in costumes)
                {
                    string sprite_name = (string)costume["name"];
                    string asset_name = (string)costume["assetId"];
                    int rot_x = (int)costume["rotationCenterX"];
                    int rot_y = (int)costume["rotationCenterY"];
                    
                    int index_of_last_space_in_name = sprite_name.LastIndexOf(' ');
                    int second_part_index_start = index_of_last_space_in_name + 1;
                    int second_part_length = sprite_name.Length - second_part_index_start;
                    string shark_type_string = sprite_name.Substring(0, index_of_last_space_in_name).Trim();
                    string shark_sprite_type_string = sprite_name.Substring(index_of_last_space_in_name + 1, second_part_length).Trim();
                    List<string> shark_sprite_type_parts = shark_sprite_type_string.Split(new char[] { '-' }).ToList();
                    
                    SharkType? shark_type_nullable = (SharkType?) EnumerationDescriptionConverter.ConvertStringDescriptionToEnumeratedValue(typeof(SharkType), shark_type_string);
                    if (shark_type_nullable.HasValue)
                    {
                        SharkType shark_type = shark_type_nullable.Value;
                        SharkSpriteType shark_sprite_type = SharkSpriteTypeConverter.ConvertStringDescriptionToSharkSpriteType(shark_sprite_type_parts[0].Trim());

                        int shark_sprite_idx = 0;
                        if (shark_sprite_type_parts.Count > 1)
                        {
                            shark_sprite_idx = Int32.Parse(shark_sprite_type_parts[1]);
                        }

                        Texture2D shark_sprite_frame_asset = Content.Load<Texture2D>(predators_asset_path + asset_name);
                        SpriteFrame sprite_frame = new SpriteFrame(shark_sprite_frame_asset, new Microsoft.Xna.Framework.Vector2(rot_x, rot_y));
                        
                        Color[] original_pixels = new Color[shark_sprite_frame_asset.Width * shark_sprite_frame_asset.Height];
                        Color[] new_pixels = new Color[shark_sprite_frame_asset.Width * shark_sprite_frame_asset.Height];
                        shark_sprite_frame_asset.GetData<Color>(original_pixels);
                        for (int i = 0; i < original_pixels.Length; i++)
                        {
                            if (original_pixels[i].A == 255)
                            {
                                new_pixels[i] = new Color(0x69, 0xBE, 0x28, 0x67);
                            }
                            else
                            {
                                new_pixels[i] = Color.Transparent;
                            }
                        }

                        var highlighted_shark_sprite_frame_asset = new Texture2D(shark_sprite_frame_asset.GraphicsDevice, 
                            shark_sprite_frame_asset.Width, shark_sprite_frame_asset.Height);
                        highlighted_shark_sprite_frame_asset.SetData<Color>(new_pixels);
                        
                        SpriteFrame highlighted_sprite_frame = new SpriteFrame(highlighted_shark_sprite_frame_asset, new Microsoft.Xna.Framework.Vector2(rot_x, rot_y));
                        
                        SharkSprites[shark_type][shark_sprite_type].Add(sprite_frame);
                        HighlightedSharkSprites[shark_type][shark_sprite_type].Add(highlighted_sprite_frame);

                        //Some of the "shock" sprites are shared across different types of sharks. In the following if-statement,
                        //we will check for this and make sure each shark-type that doesn't have a "shock" sprite gets one.
                        if (shark_sprite_type == SharkSpriteType.Shock)
                        {
                            if (shark_type == SharkType.Pirahna1)
                            {
                                SharkSprites[SharkType.Pirahna2][shark_sprite_type].Add(sprite_frame);
                                HighlightedSharkSprites[SharkType.Pirahna2][shark_sprite_type].Add(highlighted_sprite_frame);
                            }
                            else if (shark_type == SharkType.TigerShark)
                            {
                                SharkSprites[SharkType.ToxicShark][shark_sprite_type].Add(sprite_frame);
                                HighlightedSharkSprites[SharkType.ToxicShark][shark_sprite_type].Add(highlighted_sprite_frame);
                            }
                            else if (shark_type == SharkType.Shark1)
                            {
                                SharkSprites[SharkType.GhostShark][shark_sprite_type].Add(sprite_frame);
                                HighlightedSharkSprites[SharkType.GhostShark][shark_sprite_type].Add(highlighted_sprite_frame);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}