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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RePlay_Common;

namespace RePlay_Activity_TyperShark.Main
{
    public class GameStage : NotifyPropertyChangedObject
    {
        #region Private data members
        
        protected int RandomSpeedVariationMaxValue = 75;
        protected int MinimumSharkSpeed = 50;
        protected int SharkDestinationX = 0;
        protected int WordsPerShark = 1;
        protected SpriteFont shark_font;
        protected SharkType shark_type = SharkType.Unknown;
        protected StageType stage_type = StageType.Normal;

        protected Texture2D grey_overlay;
        protected GraphicsDevice graphics_device;

        protected bool use_impaired_lanes = false;

        #endregion

        #region Constructor
        
        public GameStage (GraphicsDevice graphicsDevice, SpriteFont f, int min_letter_count, int max_letter_count, int shark_speed, int number_of_sharks, int shark_destination_x, 
            int number_of_words_per_shark, SharkType stype, StageType stg_type, bool allow_numbers)
        {
            //Create the grey overlay texture for single shark stages
            graphics_device = graphicsDevice;
            grey_overlay = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            grey_overlay.SetData<Color>(new Color[] { new Color(0x66, 0x66, 0x66, 0x66) });

            MinimumLetterCount = min_letter_count;
            MaximumLetterCount = max_letter_count;
            SharkSpeed = shark_speed;
            NumberOfSharks = number_of_sharks;
            AreNumbersAllowed = allow_numbers;
            SharkDestinationX = shark_destination_x;
            WordsPerShark = number_of_words_per_shark;
            shark_font = f;
            shark_type = stype;
            stage_type = stg_type;

            if (GameConfiguration.UseImpairedScaleFactors)
            {
                use_impaired_lanes = true;
            }

            InitializeSharks();
        }

        #endregion

        #region Private methods
        
        protected void AddShark (List<string> selected_words, Vector2 position, bool treat_as_sentence = false)
        {
            var this_shark_speed = -Math.Max(MinimumSharkSpeed, Math.Abs(SharkSpeed +
            (RandomNumberStatic.RandomNumbers.Next(0, RandomSpeedVariationMaxValue * 2) - RandomSpeedVariationMaxValue)));

            var randomly_selected_destination = RandomNumberStatic.RandomNumbers.Next(0, SharkDestinationX);
            var actual_destination_x = (GameConfiguration.VirtualScreenWidth - GameConfiguration.MarginRight) - randomly_selected_destination;

            //Create the new shark using the first word in the shuffled list
            Shark s = new Shark(shark_font, selected_words, graphics_device, treat_as_sentence)
            {
                VelocityX = this_shark_speed,
                DestinationX = actual_destination_x,
                PositionX = position.X,
                PositionY = position.Y,
                SharkType = shark_type
            };

            s.PropertyChanged += HandleSharkWordCompletedEvent;

            //Add the new shark to the list of sharks for this stage
            Sharks.Add(s);
        }

        protected void InitializeSharks ()
        {
            switch (stage_type)
            {
                case StageType.Normal:
                    InitializeSharks_NormalMode();
                    break;
                case StageType.Normal_Numbers:
                    break;
                case StageType.SingleShark_WordAtBottom:
                    InitializeSharks_SingleShark_WordAtBottom_Mode();
                    break;
                case StageType.SingleShark_Numbers:
                    InitializeSharks_SingleShark_Numbers_Mode();
                    break;
                case StageType.SingleShark_Sentences:
                    InitializeSharks_SentenceMode();
                    break;
                case StageType.OceanFloor_ShipwreckBonus:
                    break;
            }
        }

        protected void InitializeSharks_NormalMode ()
        {
            //Grab all possible word choices
            var possible_words = GameConfiguration.GameDictionary.Where(x => x.Length >= MinimumLetterCount && x.Length <= MaximumLetterCount).ToList();

            //Bin all words by the letter the word starts with
            var binned_words = new Dictionary<string, List<string>>();
            foreach (string s in GameConfiguration.GameAlphabet)
            {
                if (AreNumbersAllowed || !Int32.TryParse(s, out int y))
                {
                    binned_words[s] = possible_words.Where(x => x.StartsWith(s)).ToList().ShuffleList();
                }
            }
            
            //Grab only bins that actually have words in them
            binned_words = binned_words.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x=> x.Value);
            if (binned_words.Keys.Count < NumberOfSharks)
            {
                NumberOfSharks = binned_words.Keys.Count;
            }

            //Shuffle the keys
            var shuffled_keys = binned_words.Keys.ToList().ShuffleList();

            //Shuffle the lanes
            var shuffled_lanes = Enumerable.Range(0, 
                (use_impaired_lanes ? GameConfiguration.ImpairedNumberOfLanes : GameConfiguration.NumberOfLanes)).ToList().ShuffleList();

            //Make sure the number of sharks does not exceed the number of lanes
            if (NumberOfSharks > shuffled_lanes.Count)
            {
                NumberOfSharks = shuffled_lanes.Count;
            }
            
            //Let's spawn each shark now
            List<Vector2> spawn_positions = new List<Vector2>();
            for (int i = 0; i < NumberOfSharks; i++)
            {
                //Determine the "spawn position" for each shark.
                var xpos = GameConfiguration.VirtualScreenWidth;
                var ypos = GameConfiguration.GetLaneYPosition(shuffled_lanes[i], use_impaired_lanes);
                Vector2 new_position = new Vector2(xpos, ypos);
                spawn_positions.Add(new_position);

                //Select words for this shark
                var selected_words = binned_words[shuffled_keys[i]];

                //Take the first N words in this bin for the words we will use for this shark
                selected_words = selected_words.Take(Math.Min(WordsPerShark, selected_words.Count)).ToList();

                AddShark(selected_words, spawn_positions[i]);
            }
        }

        protected void InitializeSharks_SingleShark_Numbers_Mode ()
        {
            //Define the number of sharks to be 1.
            NumberOfSharks = 1;
            
            //Shuffle the lanes. //We are only using the first 4 lanes for this stage type
            var shuffled_lanes = Enumerable.Range(0, 4).ToList().ShuffleList();

            //Let's spawn each shark now
            List<Vector2> spawn_positions = new List<Vector2>();
            for (int i = 0; i < NumberOfSharks; i++)
            {
                //Determine the "spawn position" for each shark.
                var xpos = GameConfiguration.VirtualScreenWidth;
                var ypos = GameConfiguration.GetLaneYPosition(shuffled_lanes[i]);
                Vector2 new_position = new Vector2(xpos, ypos);
                spawn_positions.Add(new_position);

                //Determine the numbers to use for this shark
                List<string> selected_words = new List<string>();
                for (int j = 0; j < WordsPerShark; j++)
                {
                    var num_digits = RePlay_Common.RandomNumberStatic.RandomNumbers.Next(MinimumLetterCount, MaximumLetterCount);
                    var max_value = Convert.ToInt32(Math.Pow(10, num_digits)) - 1;
                    var chosen_number = RePlay_Common.RandomNumberStatic.RandomNumbers.Next(0, max_value);
                    selected_words.Add(chosen_number.ToString());
                }

                AddShark(selected_words, spawn_positions[i]);
            }
        }

        protected void InitializeSharks_SingleShark_WordAtBottom_Mode()
        {
            //Define the number of sharks to be 1.
            NumberOfSharks = 1;

            //Grab all possible word choices
            var possible_words = GameConfiguration.GameDictionary.Where(x => x.Length >= MinimumLetterCount && x.Length <= MaximumLetterCount).ToList();
            possible_words = possible_words.ShuffleList();

            //Shuffle the lanes. //We are only using the first 4 lanes for this stage type
            var shuffled_lanes = Enumerable.Range(0, 4).ToList().ShuffleList();
            
            //Let's spawn each shark now
            List<Vector2> spawn_positions = new List<Vector2>();
            for (int i = 0; i < NumberOfSharks; i++)
            {
                //Determine the "spawn position" for each shark.
                var xpos = GameConfiguration.VirtualScreenWidth;
                var ypos = GameConfiguration.GetLaneYPosition(shuffled_lanes[i]);
                Vector2 new_position = new Vector2(xpos, ypos);
                spawn_positions.Add(new_position);

                //Select words for this shark
                var selected_words = possible_words;

                //Take the first N words in this bin for the words we will use for this shark
                selected_words = selected_words.Take(Math.Min(WordsPerShark, selected_words.Count)).ToList();

                AddShark(selected_words, spawn_positions[i]);
            }
        }

        protected void InitializeSharks_SentenceMode ()
        {
            if (stage_type == StageType.SingleShark_Sentences)
            {
                NumberOfSharks = 1;

                //Grab all possible sentences
                var possible_sentences = GameConfiguration.GameDictionarySentences.ToList().ShuffleList();

                //Choose a sentence
                var chosen_sentence = possible_sentences.First();
                var words_in_sentence = chosen_sentence.Split(new char[] { ' ' }).ToList();

                //Shuffle the lanes
                var shuffled_lanes = Enumerable.Range(0, 4).ToList().ShuffleList(); //We are only using the first 4 lanes for this stage type
                
                //Let's spawn each shark now
                List<Vector2> spawn_positions = new List<Vector2>();
                for (int i = 0; i < NumberOfSharks; i++)
                {
                    //Determine the "spawn position" for each shark.
                    var xpos = GameConfiguration.VirtualScreenWidth;
                    var ypos = GameConfiguration.GetLaneYPosition(shuffled_lanes[i]);
                    Vector2 new_position = new Vector2(xpos, ypos);
                    spawn_positions.Add(new_position);
                    
                    AddShark(words_in_sentence, spawn_positions[i], true);
                }
            }
        }
        
        protected void HandleSharkWordCompletedEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Handle the creation of a "floating score" when the word gets completed
            if (SelectedShark != null)
            {
                string completed_Word = SelectedShark.CompletedWords.Last();
                int new_word_score = completed_Word.Length * GameConfiguration.ScorePerLetter;
                if (SelectedShark.TotalWordsCompleted >= SelectedShark.OriginalWordCount)
                {
                    new_word_score += GameConfiguration.ScorePerWord * SelectedShark.OriginalWordCount;

                    NotifyPropertyChanged("SharkCompleted");
                }
                else
                {
                    NotifyPropertyChanged("WordCompleted");
                }

                //Add the new score to the user's total score
                GameConfiguration.CurrentScore += new_word_score;

                //Create a "floating score" object that will be displayed near the fish on the screen for a little bit
                FloatingScore new_floating_score = new FloatingScore(shark_font, new Vector2(SelectedShark.PositionX, SelectedShark.PositionY),
                    new_word_score);
                FloatingScores.Add(new_floating_score);
            }

            //This function gets called when a word has been completed by the user. In this case, the shark is no longer "selected".
            //So we set "SelectedShark" to be null.
            SelectedShark = null;
        }

        #endregion

        #region Public methods

        public virtual void SaveStageState (BinaryWriter fid, List<Keys> released_keys)
        {
            //Save the stage type
            string stage_type_string = StageType.ToString();
            fid.Write(stage_type_string.Length);
            fid.Write(Encoding.ASCII.GetBytes(stage_type_string));
            
            //Save the number of sharks in this stage
            fid.Write(Sharks.Count);

            //Save each shark that is currently alive
            var alive_sharks = Sharks.Where(x => x.IsAlive).ToList();
            fid.Write(alive_sharks.Count);
            alive_sharks.ForEach(x => x.SaveSharkState(fid));
            
            //Save which shark is the "selected" shark at the current time
            if (SelectedShark == null)
            {
                fid.Write((byte)0);
            }
            else
            {
                fid.Write((byte)1);
                fid.Write(SelectedShark.SharkGuid.ToByteArray());
            }

            //Save how many keys were released
            fid.Write(released_keys.Count);

            //Save a list of all keys that were released during this most recent frame of activity
            for (int i = 0; i < released_keys.Count; i++)
            {
                //Save the individual keypress
                string keypress_str = released_keys[i].ToString();
                fid.Write(keypress_str.Length);
                fid.Write(Encoding.ASCII.GetBytes(keypress_str));
            }
        }

        public virtual void ResetStage ()
        {
            InitializeSharks();
        }

        public virtual void HandleKeyboardInput (Keys key, char keypress)
        {
            //Change the keypress to be in string format
            string keypress_str = new string(keypress, 1);

            //Check to see if the keypress matches the first letter of any shark in this stage
            if (SelectedShark == null)
            {
                SelectedShark = Sharks.Where(x => x.IsActive && x.CurrentWord.StartsWith(keypress_str, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            }

            //Now let's handle the keypress assuming a shark has been selected
            if (SelectedShark != null)
            {
                //Pass the keypress into the shark for it to handle it.
                SelectedShark.HandleTextInput(key, keypress);
            }
        }

        public virtual void Update (GameTime gameTime, bool zap)
        {
            //Zap the sharks if necessary
            if (zap)
            {
                //Update the score upon zapping
                var active_sharks_list = Sharks.Where(x => x.IsAlive).ToList();
                int score_per_shark = GameConfiguration.ScorePerWord;
                var total_score_change = score_per_shark * active_sharks_list.Count;
                foreach (Shark s in active_sharks_list)
                {
                    //Create a "floating score" object that will be displayed near the fish on the screen for a little bit
                    FloatingScore new_floating_score = new FloatingScore(shark_font, new Vector2(s.PositionX, s.PositionY), score_per_shark);
                    FloatingScores.Add(new_floating_score);
                }

                GameConfiguration.CurrentScore += total_score_change;

                //Zap each shark
                active_sharks_list.ForEach(x => x.ZapShark());
            }

            //Unselect the selected shark if it has gone out of bounds
            if (SelectedShark != null)
            {
                if (SelectedShark.IsOutOfBounds)
                {
                    SelectedShark = null;
                }
            }

            //Remove scores that have been up for more than 2 seconds
            FloatingScores.RemoveAll(x => DateTime.Now >= (x.ScorePostedTime + TimeSpan.FromSeconds(2.0)));

            //Update the position of each floating score in the stage
            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Update(gameTime));
            
            //Update each shark in the stage
            Sharks.ForEach(x => x.UpdateShark(gameTime));
        }

        public virtual void DrawStage (SpriteBatch spriteBatch)
        {
            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Draw(spriteBatch));
            Sharks.ForEach(x => x.DrawShark(spriteBatch, stage_type));

            if (stage_type == StageType.SingleShark_WordAtBottom || stage_type == StageType.SingleShark_Sentences ||
                stage_type == StageType.SingleShark_Numbers)
            {
                //Draw the transparent grey overlay
                spriteBatch.Draw(grey_overlay, 
                    new Rectangle(0, 
                                  Convert.ToInt32(GameConfiguration.VirtualScreenHeight) - 375, 
                                  Convert.ToInt32(GameConfiguration.VirtualScreenWidth), 
                                  375), 
                    Color.White);

                //Select the text to display
                string text = "Type the word below:";
                if (stage_type == StageType.SingleShark_Sentences)
                {
                    text = "Type the words below:";
                }

                //Determine the position of the text
                var word_size_pixels = shark_font.MeasureString(text);
                Vector2 word_size_pixels_half = new Vector2(word_size_pixels.X / 2, word_size_pixels.Y / 2);
                Vector2 text_pos = new Vector2(GameConfiguration.VirtualScreenHalfWidth - word_size_pixels_half.X,
                    GameConfiguration.VirtualScreenHeight - 350);
                spriteBatch.DrawString(shark_font, text, text_pos, new Color(0x69, 0xBE, 0x28), 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
            }
        }

        #endregion

        #region Public properties

        public int MinimumLetterCount { get; set; } = 3;
        public int MaximumLetterCount { get; set; } = 3;
        public int SharkSpeed { get; set; } = 1;
        public int NumberOfSharks { get; set; } = 1;
        public bool AreNumbersAllowed { get; set; } = false;

        public List<FloatingScore> FloatingScores = new List<FloatingScore>();
        public List<Shark> Sharks = new List<Shark>();
        public Shark SelectedShark { get; set; } = null;

        public virtual bool IsStageCompleted
        {
            get
            {
                return Sharks.All(x => x.IsOutOfBounds);
            }
        }

        public StageType StageType
        {
            get
            {
                return stage_type;
            }
        }

        #endregion
    }
}