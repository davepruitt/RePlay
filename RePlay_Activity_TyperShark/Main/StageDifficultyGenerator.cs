using System;
using System.Collections.Generic;
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
using RePlay_Exercises;

namespace RePlay_Activity_TyperShark.Main
{
    public static class StageDifficultyGenerator
    {
        #region Public static methods

        public static GameStage GenerateStage(GraphicsDevice graphicsDevice, 
            SpriteFont shark_font, 
            double difficulty, 
            bool prevent_jellyfish,
            ExerciseType exerciseType)
        {
            double rand_stage_gen = RePlay_Common.RandomNumberStatic.RandomNumbers.NextDouble();

            var difficulty_parameters = StageDifficultyGenerator.GenerateDifficultyParameters(difficulty);
            var words_per_shark = StageDifficultyGenerator.GetWordsPerShark(difficulty_parameters[0]);
            var shark_speed = StageDifficultyGenerator.GetSharkSpeed(difficulty_parameters[1]);
            var starting_location = StageDifficultyGenerator.GetStartingLocation(difficulty_parameters[2]);
            var number_of_fish = StageDifficultyGenerator.GetNumberOfFish(difficulty_parameters[3]);
            var letters_per_word = StageDifficultyGenerator.GetLettersPerWord(difficulty_parameters[4]);

            SharkType shark_type = SharkType.Pirahna1;
            if (letters_per_word <= 2)
            {
                if (words_per_shark > 1)
                {
                    shark_type = SharkType.Pirahna2;
                    words_per_shark = 2;
                }
            }
            else
            {
                switch (words_per_shark)
                {
                    case 1:
                        shark_type = SharkType.Shark1;
                        break;
                    case 2:
                        shark_type = SharkType.Hammerhead;
                        break;
                    case 3:
                        shark_type = SharkType.TigerShark;
                        break;
                    case 4:
                        shark_type = SharkType.ToxicShark;
                        break;
                    case 5:
                        shark_type = SharkType.GhostShark;
                        break;
                }
            }

            int min_letters_per_word = 1;
            int max_letters_per_word = letters_per_word;
            if (letters_per_word > 2)
            {
                min_letters_per_word = letters_per_word - 2;
                max_letters_per_word = letters_per_word + 2;
            }

            StageType stage_type = StageType.Normal;
            if (number_of_fish == 1)
            {
                stage_type = StageType.SingleShark_WordAtBottom;
            }

            if (rand_stage_gen < 0.1 && difficulty >= 0.3 && exerciseType == ExerciseType.Keyboard_Typing)
            {
                GameStage new_sentence_stage = new GameStage(graphicsDevice, shark_font, min_letters_per_word, max_letters_per_word, shark_speed, number_of_fish,
                    starting_location, words_per_shark, shark_type, StageType.SingleShark_Sentences, false);
                return new_sentence_stage;
            }
            else if (rand_stage_gen > 0.9 && !prevent_jellyfish)
            {
                GameStage_JellyfishAttack jellyfish_stage = new GameStage_JellyfishAttack(graphicsDevice, shark_font, TimeSpan.FromSeconds(15),
                    GetJellyfishFrequency(difficulty));
                return jellyfish_stage;
            }
            else
            {
                GameStage new_stage = new GameStage(graphicsDevice, shark_font, min_letters_per_word, max_letters_per_word, shark_speed, number_of_fish,
                    starting_location, words_per_shark, shark_type, stage_type, false);
                return new_stage;
            }
        }

        public static List<double> GenerateDifficultyParameters ( double difficulty )
        {
            int num_parameters = 5;
            List<double> parameters = new List<double>();
            double min = 0;
            double max = 1;
            for (int i = 0; i < num_parameters; i++)
            {
                double range = max - min;
                double new_rand = RePlay_Common.RandomNumberStatic.RandomNumbers.NextDouble();
                double new_parameter = min + (new_rand * range);
                parameters.Add(new_parameter);
                double avg = parameters.Average();
                if (avg < difficulty)
                {
                    min = difficulty;
                    max = 1;
                }
                else
                {
                    min = 0;
                    max = difficulty;
                }
            }

            return (parameters.ShuffleList());
        }

        public static int GetWordsPerShark ( double difficulty_parameter )
        {
            double max_words_per_shark = 5;
            double min_words_per_shark = 1;
            double range = max_words_per_shark - min_words_per_shark;
            double words_per_shark = min_words_per_shark + (difficulty_parameter * range);

            return Convert.ToInt32(Math.Round(words_per_shark));
        }

        public static int GetSharkSpeed ( double difficulty_parameter )
        {
            double min_speed = 100;
            double max_speed = 300;
            double range = max_speed - min_speed;
            double shark_speed = min_speed + (difficulty_parameter * range);

            return Convert.ToInt32(Math.Round(shark_speed));
        }

        public static int GetStartingLocation ( double difficulty_parameter )
        {
            double min_starting_location = 100;
            double max_starting_location = 400;
            double range = max_starting_location - min_starting_location;
            double starting_location = min_starting_location + (difficulty_parameter * range);

            return Convert.ToInt32(Math.Round(starting_location));
        }

        public static int GetNumberOfFish ( double difficulty_parameter )
        {
            double min_fish = 1;
            double max_fish = 6;
            double range = max_fish - min_fish;
            double num_fish = min_fish + (difficulty_parameter * range);

            return Convert.ToInt32(Math.Round(num_fish));
        }

        public static int GetLettersPerWord ( double difficulty_parameter )
        {
            double min_letters = 1;
            double max_letters = 7;
            double range = max_letters - min_letters;
            double num_letters = min_letters + (difficulty_parameter * range);

            return Convert.ToInt32(Math.Round(num_letters));
        }

        public static double GetJellyfishFrequency ( double difficulty_parameter )
        {
            //I have calculated the following coefficients to get the desired jellyfish frequencies.
            //These can be adjusted if needed.
            double a = 0.011;
            double b = -0.016;
            double c = 0.055;
            double d = 0.29;
            
            double x = difficulty_parameter * 10.0;
            double x_sqr = Math.Pow(x, 2);
            double x_cub = Math.Pow(x, 3);

            double y = (a * x_cub + b * x_sqr + c * x + d);
            return y;
        }

        #endregion
    }
}