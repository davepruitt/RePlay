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
    public class GameStage_ShipwreckBonus : GameStage
    {
        #region Private class that should only be used by this game stage

        private class ShipwreckBonusShark : Shark
        {
            public ShipwreckBonusShark(SpriteFont f, string word, GraphicsDevice d)
                : base(f, word, d)
            {
                shark_state = SharkState.Active;
            }
        }

        #endregion

        #region Private data members

        private int minimum_shipwreck_bonus_word_length = 4;
        private int shipwreck_bonus_word_score = 1000;

        private List<string> words_completed = new List<string>();
        private string current_word = string.Empty;
        private int current_word_character_index = 0;
        private Vector2 current_word_size = Vector2.Zero;
        private Vector2 current_word_half_size = Vector2.Zero;
        private Vector2 current_word_position = Vector2.Zero;
        private ShipwreckBonusShark current_shark = null;

        private string shipwreck_bonus_title = "SHIPWRECK BONUS!";
        private string shipwreck_bonus_instructions = "TYPE AS MANY WORDS AS YOU CAN BEFORE TIME RUNS OUT!";

        private Vector2 shipwreck_bonus_title_size = Vector2.Zero;
        private Vector2 shipwreck_bonus_title_size_half = Vector2.Zero;
        private Vector2 shipwreck_bonus_title_position = Vector2.Zero;
        private Vector2 shipwreck_bonus_instructions_size = Vector2.Zero;
        private Vector2 shipwreck_bonus_instructions_size_half = Vector2.Zero;
        private Vector2 shipwreck_bonus_instructions_position = Vector2.Zero;

        private List<string> shuffled_possible_words = new List<string>();
        private int shuffled_possible_words_idx = -1;

        private SpriteFont shipwreck_large_font;

        private BubbleManager bubble_manager;
        private TimeSpan bubble_manager_duration = TimeSpan.FromMilliseconds(250);
        private DateTime bubble_manager_starttime = DateTime.Now;

        

        #endregion

        #region Constructor

        public GameStage_ShipwreckBonus (GraphicsDevice graphicsDevice, SpriteFont regular_font, SpriteFont large_font)
            : base(graphicsDevice, regular_font, 0, 0, 0, 0, 0, 0, SharkType.Unknown, StageType.OceanFloor_ShipwreckBonus, false)
        {
            bubble_manager = new BubbleManager(Convert.ToInt32(GameConfiguration.VirtualScreenHalfWidth / 2), 100, 0, 800);
            bubble_manager.IsActive = false;

            shipwreck_large_font = large_font;

            shipwreck_bonus_title_size = shark_font.MeasureString(shipwreck_bonus_title);
            shipwreck_bonus_title_size_half = new Vector2(shipwreck_bonus_title_size.X / 2, shipwreck_bonus_title_size.Y / 2);
            shipwreck_bonus_title_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - shipwreck_bonus_title_size_half.X, 150);

            shipwreck_bonus_instructions_size = shark_font.MeasureString(shipwreck_bonus_instructions);
            shipwreck_bonus_instructions_size_half = new Vector2(shipwreck_bonus_instructions_size.X / 2, shipwreck_bonus_instructions_size.Y / 2);
            shipwreck_bonus_instructions_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - shipwreck_bonus_instructions_size_half.X, 300);

            shuffled_possible_words = GameConfiguration.GameDictionary.Where(x => x.Length >= minimum_shipwreck_bonus_word_length).ToList().ShuffleList();
            shuffled_possible_words = shuffled_possible_words.Select(x => x.ToUpper()).ToList();

            FetchNewWord();
        }

        #endregion

        #region Private functions

        private void FetchNewWord ()
        {
            shuffled_possible_words_idx++;
            if (shuffled_possible_words.Count > 0 && shuffled_possible_words_idx < shuffled_possible_words.Count)
            {
                current_word = shuffled_possible_words[shuffled_possible_words_idx];
                current_word_size = shipwreck_large_font.MeasureString(current_word);
                current_word_half_size = new Vector2(current_word_size.X / 2, current_word_size.Y / 2);
                current_word_position = new Vector2(GameConfiguration.VirtualScreenHalfWidth - current_word_half_size.X, 800);

                current_shark = new ShipwreckBonusShark(shipwreck_large_font, current_word, graphics_device);
                current_shark.CurrentCharacterIndex = 0;

                bubble_manager.X_Spread = Convert.ToInt32(current_word_half_size.X);
            }
        }

        #endregion

        #region Methods

        public override void ResetStage()
        {
            //empty
        }

        public override void HandleKeyboardInput(Keys key, char keypress)
        {
            if (current_word_character_index < current_word.Length)
            {
                //Check to see if the key that was pressed matches the next letter in the current word
                if (current_word[current_word_character_index] == Char.ToUpper(keypress))
                {
                    //And increment the index to the next character in the string
                    current_word_character_index++;
                    current_shark.CurrentCharacterIndex = current_word_character_index;

                    //And then check to see if the user has actually completed this string
                    if (current_word_character_index >= current_word.Length)
                    {
                        //Handle the bubble manager
                        bubble_manager.IsActive = true;
                        bubble_manager_starttime = DateTime.Now;

                        //Add the new score to the user's total score
                        GameConfiguration.CurrentScore += shipwreck_bonus_word_score;

                        //Create a "floating score" object that will be displayed near the fish on the screen for a little bit
                        FloatingScore new_floating_score = new FloatingScore(shark_font, current_word_position, shipwreck_bonus_word_score);
                        FloatingScores.Add(new_floating_score);

                        //Reset the current character index to zero
                        current_word_character_index = 0;

                        //If the string has been completed, remove it from the list of words for this shark
                        words_completed.Add(current_word);

                        //Grab a new word to display to the user
                        FetchNewWord();
                    }
                }
                
            }
        }

        public override void Update(GameTime gameTime, bool zap)
        {
            bubble_manager.Update(gameTime);
            if (DateTime.Now >= (bubble_manager_starttime + bubble_manager_duration))
            {
                bubble_manager.IsActive = false;
            }

            //Remove scores that have been up for more than 2 seconds
            FloatingScores.RemoveAll(x => DateTime.Now >= (x.ScorePostedTime + TimeSpan.FromSeconds(2.0)));

            //Update the position of each floating score in the stage
            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Update(gameTime));
        }

        public override void DrawStage(SpriteBatch spriteBatch)
        {
            //Draw the bubbles
            bubble_manager.Draw(spriteBatch);

            //Draw the floating scores
            FloatingScores.Where(x => !x.IsOutOfBounds).ToList().ForEach(x => x.Draw(spriteBatch));

            //Draw the title and instructions
            spriteBatch.DrawString(shark_font, shipwreck_bonus_title, shipwreck_bonus_title_position, Color.Orange);
            spriteBatch.DrawString(shark_font, shipwreck_bonus_instructions, shipwreck_bonus_instructions_position, Color.White);

            //Draw the current word
            spriteBatch.DrawString(shipwreck_large_font, current_word, current_word_position, Color.White);

            //Now overlay the characters that have already been typed by the user
            if (current_word_character_index > 0)
            {
                string partial_word = current_word.Substring(0, current_word_character_index);
                spriteBatch.DrawString(shipwreck_large_font, partial_word, current_word_position, Color.Red, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
            }
        }

        public override void SaveStageState(BinaryWriter fid, List<Keys> released_keys)
        {
            Sharks.Clear();
            if (current_shark != null)
            {
                Sharks.Add(current_shark);
            }
            
            base.SaveStageState(fid, released_keys);
        }

        #endregion

        #region Properties

        public override bool IsStageCompleted
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}