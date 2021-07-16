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
using RePlay_Activity_Common;
using RePlay_Common;

namespace RePlay_Activity_TyperShark.Main
{
    public class Shark : NotifyPropertyChangedObject
    {
        #region Static private members

        /// <summary>
        /// This dictionary holds a set of properties that descibe each type of shark. The keys are each type of shark. The values
        /// are a 4-value tuple which contains the following information: default text color for this type of shark, secondary
        /// text color for this type of shark, and the x-offset and y-offset for this type of shark.
        /// </summary>
        private Dictionary<SharkType, Tuple<Color, Color, int, int>> SharkTypeProperties = new Dictionary<SharkType, Tuple<Color, Color, int, int>>()
        {
            { SharkType.Shark1, new Tuple<Color, Color, int, int>(Color.Black, Color.Yellow, 50, 10) },
            { SharkType.GhostShark, new Tuple<Color, Color, int, int>(Color.Black, Color.Red, 50, 10) },
            { SharkType.Hammerhead, new Tuple<Color, Color, int, int>(Color.Black, Color.Yellow, 50, 10) },
            { SharkType.TigerShark, new Tuple<Color, Color, int, int>(Color.Black, Color.White, 50, 10) },
            { SharkType.ToxicShark, new Tuple<Color, Color, int, int>(Color.Black, Color.White, 50, 10) },
            { SharkType.Pirahna1, new Tuple<Color, Color, int, int>(Color.Black, Color.Yellow, 15, -25) },
            { SharkType.Pirahna2, new Tuple<Color, Color, int, int>(Color.Black, Color.Yellow, 15, -25) },
            { SharkType.Jellyfish, new Tuple<Color, Color, int, int>(Color.Black, Color.Yellow, 50, 10) },
        };

        private const float NormalFishScale = 2.0f;
        private const float NormalTextScale = 1.0f;
        private const float ImpairedFishScale = 3.0f;
        private const float ImpairedTextScale = 1.5f;
        private const float ImpairedPiranhaScale = 2.25f;
        private const float ImpairedPiranhaTextScale = 1.25f;

        #endregion

        #region Private data members that should only be referenced by their associated properties and not by anything else
        
        private SharkType shark_type = SharkType.Shark1;

        #endregion

        #region Private data members

        private int inactive_velocity = -600;
        private int dead_velocity = -400;

        private SpriteFont shark_text_font = null;
        private Vector2 word_size_pixels = Vector2.Zero;
        private Vector2 word_size_pixels_half = Vector2.Zero;
        private Color default_text_color = Color.Black;
        private Color secondary_text_color = Color.Yellow;
        private int text_offset_x = 0;
        private int text_offset_y = 0;
        private float fish_scale = NormalFishScale;
        private float text_scale = NormalTextScale;

        private SharkSpriteType current_animation_sequence = SharkSpriteType.Swim;
        private int current_animation_frame = 0;

        private int ms_per_frame = 100;
        private DateTime last_frame_update = DateTime.Now;
        private DateTime last_word_completed = DateTime.Now;
        private DateTime time_of_death = DateTime.MinValue;

        private List<string> shark_words = new List<string>();
        protected SharkState shark_state = SharkState.Inactive;

        private bool is_flashing = false;
        private int flash_duration = 1000;
        private DateTime flash_start_time = DateTime.Now;

        private bool flash_frame_on = false;
        private int flash_frame_duration = 200;
        private DateTime flash_frame_start_time = DateTime.Now;
        private bool treat_as_sentence = false;

        private int current_word_index = 0;

        private GraphicsDevice graphics_device;

        private Guid shark_guid = Guid.Empty;

        #endregion

        #region Constructor

        public Shark (SpriteFont f, string word, GraphicsDevice d)
        {
            graphics_device = d;
            shark_text_font = f;
            AssignWords(new List<string>() { word });
        }

        public Shark (SpriteFont f, List<string> words, GraphicsDevice d, bool sentence = false)
        {
            graphics_device = d;
            treat_as_sentence = sentence;
            shark_text_font = f;
            AssignWords(words);
        }

        #endregion

        #region Properties

        public List<string> CompletedWords { get; set; } = new List<string>();
        public int TotalWordsCompleted { get; set; } = 0;
        public int OriginalWordCount { get; set; } = 0;
        public float DestinationX { get; set; } = 0;
        public float DestinationY { get; set; } = 0;
        public float PositionX { get; set; } = 0;
        public float PositionY { get; set; } = 0;
        public float VelocityX { get; set; } = 0;
        public float VelocityY { get; set; } = 0;
        public int CurrentCharacterIndex { get; set; } = 0;
        public bool UseDestinationY { get; set; } = false;

        public Guid SharkGuid
        {
            get
            {
                return shark_guid;
            }
        }

        public string CurrentWord
        {
            get
            {
                if (treat_as_sentence)
                {
                    return (string.Join(string.Empty, shark_words));
                }
                else
                {
                    if (shark_words.Count > 0 && current_word_index < shark_words.Count)
                    {
                        return shark_words[current_word_index];
                    }
                }
                
                return string.Empty;
            }
        }
        
        public bool IsActive
        {
            get
            {
                return (shark_state == SharkState.Active);
            }
        }

        public bool IsOutOfBounds
        {
            get
            {
                return (shark_state == SharkState.Inactive_OutOfBounds);
            }
        }

        public bool IsAlive
        {
            get
            {
                return (shark_state == SharkState.Active || shark_state == SharkState.Inactive_Electrocuted);
            }
        }
        
        public SharkType SharkType
        {
            get
            {
                return shark_type;
            }
            set
            {
                shark_type = value;

                var shark_type_properties = SharkTypeProperties[shark_type];
                default_text_color = shark_type_properties.Item1;
                secondary_text_color = shark_type_properties.Item2;
                text_offset_x = shark_type_properties.Item3;
                text_offset_y = shark_type_properties.Item4;

                if (GameConfiguration.UseImpairedScaleFactors)
                {
                    if (shark_type == SharkType.Pirahna1 && shark_type == SharkType.Pirahna2)
                    {
                        fish_scale = ImpairedPiranhaScale;
                        text_scale = ImpairedPiranhaTextScale;
                    }
                    else if (shark_type == SharkType.Jellyfish)
                    {
                        fish_scale = NormalFishScale;
                        text_scale = ImpairedTextScale;
                    }
                    else
                    {
                        fish_scale = ImpairedFishScale;
                        text_scale = ImpairedTextScale;
                    }
                }
                else
                {
                    fish_scale = NormalFishScale;
                    text_scale = NormalTextScale;
                }

                MeasureWord();
            }
        }

        #endregion

        #region Private methods

        private void MeasureWord ()
        {
            if (shark_words.Count > 0 && current_word_index >= 0 && current_word_index < shark_words.Count)
            {
                string shark_text_word = shark_words[current_word_index];
                word_size_pixels = shark_text_font.MeasureString(shark_text_word);
                word_size_pixels.X = word_size_pixels.X * text_scale;
                word_size_pixels.Y = word_size_pixels.Y * text_scale;
                word_size_pixels_half = new Vector2(word_size_pixels.X / 2.0f, word_size_pixels.Y / 2.0f);
            }
        }

        private void KillShark()
        {
            shark_state = SharkState.Inactive_Dead;
            time_of_death = DateTime.Now;
        }

        #endregion

        #region Methods

        public void SaveSharkState (BinaryWriter fid)
        {
            if (shark_guid == Guid.Empty)
            {
                //Generate a guid for this shark
                shark_guid = Guid.NewGuid();

                //Save the shark guid
                fid.Write(shark_guid.ToByteArray());

                //Save the type of shark
                string shark_type_string = shark_type.ToString();
                fid.Write(shark_type_string.Length);
                fid.Write(Encoding.ASCII.GetBytes(shark_type_string));

                //Save the list of words for this shark
                int word_count = shark_words.Count;
                fid.Write(word_count);

                for (int i = 0; i < word_count; i++)
                {
                    int word_length = shark_words[i].Length;
                    fid.Write(word_length);
                    fid.Write(Encoding.ASCII.GetBytes(shark_words[i]));
                }
            }
            else
            {
                //Save the shark guid
                fid.Write(shark_guid.ToByteArray());
            }
            
            //Save the current word index (which word the user is currently on)
            fid.Write(current_word_index);

            //Save the current character index (which character of the current word)
            fid.Write(CurrentCharacterIndex);

            //Save the current position of the shark
            fid.Write(PositionX);
            fid.Write(PositionY);

            //Save the current velocity of the shark
            fid.Write(VelocityX);
            fid.Write(VelocityY);
        }

        public void ZapShark ()
        {
            CurrentCharacterIndex = 0;
            last_word_completed = DateTime.Now;
            current_word_index = shark_words.Count;
            shark_state = SharkState.Inactive_Electrocuted;
        }

        public void AssignWords (List<string> words)
        {
            if (treat_as_sentence)
            {
                for (int i = 1; i < words.Count; i++)
                {
                    words[i] = " " + words[i];
                }
            }

            words = words.Select(x => x.ToUpper()).ToList();
            OriginalWordCount = words.Count;
            shark_words = words;
            MeasureWord();
        }
        
        public void HandleTextInput (Keys key, char c)
        {
            if (shark_words.Count > 0 && current_word_index < shark_words.Count)
            {
                //Check to see if the key that was pressed matches the next letter in the current word
                if (Char.ToUpper(shark_words[current_word_index][CurrentCharacterIndex]) == Char.ToUpper(c))
                {
                    if (PositionX <= GameConfiguration.VirtualScreenWidth - GameConfiguration.PenaltyFreeZone)
                    {
                        //If so, then push the shark back a bit as a penalty to the shark
                        PositionX += GameConfiguration.PenaltyOffsetDistance;
                    }
                    
                    //And increment the index to the next character in the string
                    CurrentCharacterIndex++;

                    //And then check to see if the user has actually completed this string
                    if (CurrentCharacterIndex >= shark_words[current_word_index].Length)
                    {
                        //Reset the current character index to zero
                        CurrentCharacterIndex = 0;
                        
                        //If the string has been completed, remove it from the list of words for this shark
                        CompletedWords.Add(shark_words[current_word_index]);
                            
                        //Set the state of this shark to be "electrocuted"
                        shark_state = SharkState.Inactive_Electrocuted;

                        //Set the timestamp for completing this latest word
                        last_word_completed = DateTime.Now;

                        //Increment the counter on this shark indicating how many words have been completed
                        TotalWordsCompleted++;

                        //Increment the current word index
                        current_word_index++;

                        //Assign the next word
                        MeasureWord();

                        if ((treat_as_sentence && current_word_index >= shark_words.Count) || !treat_as_sentence)
                        {
                            //Notify the GameStage object that the current word has been completed
                            NotifyPropertyChanged("WordCompleted");
                        }
                    }
                }
                else
                {
                    flash_start_time = DateTime.Now;
                    is_flashing = true;

                    flash_frame_start_time = DateTime.Now;
                    flash_frame_on = true;
                }
            }
        }

        public void UpdateShark (GameTime t)
        {
            if (is_flashing)
            {
                if (DateTime.Now > (flash_start_time + TimeSpan.FromMilliseconds(flash_duration)))
                {
                    is_flashing = false;
                    flash_frame_on = false;
                }
                else
                {
                    if (DateTime.Now > (flash_frame_start_time + TimeSpan.FromMilliseconds(flash_frame_duration)))
                    {
                        flash_frame_on = !flash_frame_on;
                        flash_frame_start_time = DateTime.Now;
                    }
                }
            }

            Texture2D shark_texture = GameConfiguration.SharkSprites[SharkType][current_animation_sequence][current_animation_frame].Texture;
            double num_pixels_moved_x = 0;
            double num_pixels_moved_y = 0;

            switch (shark_state)
            {
                case SharkState.Inactive:

                    current_animation_sequence = SharkSpriteType.Swim;

                    if (!UseDestinationY)
                    {
                        //Update the position of the shark on the screen
                        num_pixels_moved_x = inactive_velocity * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_x = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_x)));
                        PositionX += Convert.ToInt32(num_pixels_moved_x);

                        //If this shark has reached its destination, make it active
                        if (PositionX <= DestinationX)
                        {
                            shark_state = SharkState.Active;
                        }

                    }
                    else
                    {
                        num_pixels_moved_y = inactive_velocity * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_y = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_y)));
                        PositionY += Convert.ToInt32(num_pixels_moved_y);

                        //If this shark has reached its destination, make it active
                        if (PositionY <= DestinationY)
                        {
                            shark_state = SharkState.Active;
                        }
                    }
                    
                    break;
                case SharkState.Active:

                    current_animation_sequence = SharkSpriteType.Swim;

                    if (!UseDestinationY)
                    {
                        //Update the position of the shark on the screen
                        num_pixels_moved_x = VelocityX * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_x = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_x)));
                        PositionX += Convert.ToInt32(num_pixels_moved_x);

                        //Check to see if the shark has reached the left side of the screen
                        if (PositionX <= GameConfiguration.MarginLeft)
                        {
                            shark_state = SharkState.Inactive_Victorious;
                        }
                    }
                    else
                    {
                        //Update the position of the shark on the screen
                        num_pixels_moved_y = VelocityY * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_y = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_y)));
                        PositionY += Convert.ToInt32(num_pixels_moved_y);

                        //Check to see if the shark has reached the left side of the screen
                        if (PositionY <= GameConfiguration.MarginTop)
                        {
                            shark_state = SharkState.Inactive_Victorious;
                        }
                    }
                    
                    break;
                case SharkState.Inactive_Electrocuted:

                    if (GameConfiguration.SharkSprites[SharkType][SharkSpriteType.Shock].Count == 0 ||
                        (DateTime.Now - last_word_completed).TotalMilliseconds >= 500)
                    {
                        //The shock animation is finished. Now let's move on to the next state for this shark...
                        if (current_word_index >= shark_words.Count)
                        {
                            //If all the words for this shark have been completed, then the shark is now dead.
                            KillShark();
                        }
                        else
                        {
                            //And then set the shark state to active
                            shark_state = SharkState.Active;
                        }
                    }
                    else
                    {
                        //If the shock animation has not yet finished...
                        current_animation_frame = 0;

                        if (DateTime.Now >= (last_frame_update + TimeSpan.FromMilliseconds(100)))
                        {
                            last_frame_update = DateTime.Now;

                            if (current_animation_sequence == SharkSpriteType.Shock)
                            {
                                current_animation_sequence = SharkSpriteType.Swim;
                            }
                            else
                            {
                                current_animation_sequence = SharkSpriteType.Shock;
                            }
                        }
                    }

                    break;

                case SharkState.Inactive_Dead:

                    num_pixels_moved_y = dead_velocity * t.ElapsedGameTime.TotalSeconds;
                    num_pixels_moved_y = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_y)));
                    PositionY += Convert.ToInt32(num_pixels_moved_y);

                    //Check to see if the shark has ascended above the top of the screen
                    if (PositionY + (shark_texture.Height * 2) < 0)
                    {
                        shark_state = SharkState.Inactive_OutOfBounds;
                    }

                    break;
                case SharkState.Inactive_Victorious:

                    current_animation_sequence = SharkSpriteType.Swim;

                    if (!UseDestinationY)
                    {
                        //Update the position of the shark on the screen
                        num_pixels_moved_x = inactive_velocity * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_x = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_x)));
                        PositionX += Convert.ToInt32(num_pixels_moved_x);

                        //Check to see if the shark has gone beyond the left side of the screen
                        if (PositionX + (shark_texture.Width * 2) < 0)
                        {
                            shark_state = SharkState.Inactive_OutOfBounds;
                        }
                    }
                    else
                    {
                        num_pixels_moved_y = inactive_velocity * t.ElapsedGameTime.TotalSeconds;
                        num_pixels_moved_y = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_y)));
                        PositionY += Convert.ToInt32(num_pixels_moved_y);

                        //If this shark has reached its destination, make it active
                        if (PositionY + (shark_texture.Height * 2) < 0)
                        {
                            shark_state = SharkState.Inactive_OutOfBounds;
                        }
                    }

                    break;
                case SharkState.Inactive_OutOfBounds:
                    break;
            }
            
            //Update the animation frame for a shark that is alive (but not electrocuted)
            if (shark_state != SharkState.Inactive_Dead && shark_state != SharkState.Inactive_Electrocuted)
            {
                //If the shark is alive...
                if (DateTime.Now >= (last_frame_update + TimeSpan.FromMilliseconds(ms_per_frame)))
                {
                    last_frame_update = DateTime.Now;
                    current_animation_frame++;

                    int num_animation_frames = GameConfiguration.SharkSprites[SharkType][current_animation_sequence].Count;
                    if (current_animation_frame >= num_animation_frames)
                    {
                        current_animation_frame = 0;
                    }
                }
            }
            else if (shark_state == SharkState.Inactive_Dead)
            {
                //Update the animation frame for a shark that is dead
                if (current_animation_sequence != SharkSpriteType.Death)
                {
                    current_animation_sequence = SharkSpriteType.Death;
                    current_animation_frame = 0;
                }

                if (DateTime.Now >= (last_frame_update + TimeSpan.FromMilliseconds(ms_per_frame)))
                {
                    last_frame_update = DateTime.Now;
                    current_animation_frame++;

                    int num_animation_frames = GameConfiguration.SharkSprites[SharkType][current_animation_sequence].Count;
                    if (current_animation_frame >= num_animation_frames)
                    {
                        current_animation_frame = num_animation_frames - 1;
                    }
                }
            }
        }

        public void DrawShark (SpriteBatch s, StageType stage_type = StageType.Normal)
        {
            //Draw the shark itself
            Texture2D shark_texture = GameConfiguration.SharkSprites[SharkType][current_animation_sequence][current_animation_frame].Texture;
            Vector2 shark_center = GameConfiguration.SharkSprites[SharkType][current_animation_sequence][current_animation_frame].RotationCenter;
            s.Draw(shark_texture, new Vector2(PositionX, PositionY), null, Color.White, 0, shark_center, fish_scale, SpriteEffects.None, 0);
            
            if (flash_frame_on)
            {
                Texture2D shark_highlighted_texture = GameConfiguration.HighlightedSharkSprites[SharkType][current_animation_sequence][current_animation_frame].Texture;
                s.Draw(shark_highlighted_texture, new Vector2(PositionX, PositionY), null, Color.White, 0, shark_center, fish_scale, SpriteEffects.None, 0);
            }
            
            //If the shark is alive and active, draw the word on the shark
            if (shark_state == SharkState.Active || shark_state == SharkState.Inactive_Electrocuted)
            {
                if (shark_words.Count > 0 && current_word_index < shark_words.Count)
                {
                    if (shark_state == SharkState.Active && stage_type == StageType.Normal)
                    {
                        //Determine the position of the text
                        Vector2 text_pos = new Vector2(PositionX - word_size_pixels_half.X + text_offset_x, PositionY - word_size_pixels_half.Y + text_offset_y);

                        //Draw the word
                        s.DrawString(shark_text_font, shark_words[current_word_index], text_pos, default_text_color, 0, Vector2.Zero, text_scale, SpriteEffects.None, 0);

                        //Now overlay the characters that have already been typed by the user
                        if (CurrentCharacterIndex > 0)
                        {
                            string partial_word = shark_words[current_word_index].Substring(0, CurrentCharacterIndex);
                            s.DrawString(shark_text_font, partial_word, text_pos, secondary_text_color, 0, Vector2.Zero, text_scale, SpriteEffects.None, 0);
                        }
                    }
                    else if (stage_type == StageType.SingleShark_WordAtBottom || stage_type == StageType.SingleShark_Numbers)
                    {
                        //Determine the position of the text
                        Vector2 text_pos = new Vector2(GameConfiguration.VirtualScreenHalfWidth - word_size_pixels_half.X,
                            GameConfiguration.VirtualScreenHeight - 250);
                        s.DrawString(shark_text_font, shark_words[current_word_index], text_pos, Color.White, 0, Vector2.Zero, text_scale, SpriteEffects.None, 0);

                        //Now overlay the characters that have already been typed by the user
                        if (CurrentCharacterIndex > 0)
                        {
                            string partial_word = shark_words[current_word_index].Substring(0, CurrentCharacterIndex);
                            s.DrawString(shark_text_font, partial_word, text_pos, Color.Red, 0, Vector2.Zero, text_scale, SpriteEffects.None, 0);
                        }
                    }
                    else if (stage_type == StageType.SingleShark_Sentences)
                    {
                        //Get the full sentence and partial sentence
                        string full_sentence = string.Join(string.Empty, shark_words);
                        string partial_sentence = string.Empty;
                        for (int i = 0; i <= current_word_index; i++)
                        {
                            if (i == current_word_index)
                            {
                                partial_sentence += shark_words[i].Substring(0, CurrentCharacterIndex);
                            }
                            else
                            {
                                partial_sentence += shark_words[i];
                            }
                        }

                        word_size_pixels = shark_text_font.MeasureString(full_sentence);
                        word_size_pixels.X = word_size_pixels.X * text_scale;
                        word_size_pixels.Y = word_size_pixels.Y * text_scale;
                        word_size_pixels_half = new Vector2(word_size_pixels.X / 2.0f, word_size_pixels.Y / 2.0f);

                        //Determine the position of the text
                        Vector2 text_pos = new Vector2(GameConfiguration.VirtualScreenHalfWidth - word_size_pixels_half.X,
                            GameConfiguration.VirtualScreenHeight - 250);
                        s.DrawString(shark_text_font, full_sentence, text_pos, Color.White, 0, Vector2.Zero, text_scale, SpriteEffects.None, 0);

                        //Now overlay the characters that have already been typed by the user
                        if (!string.IsNullOrEmpty(partial_sentence))
                        {
                            //Measure the partial sentence
                            var partial_sentence_size_pixels = shark_text_font.MeasureString(partial_sentence);
                            partial_sentence_size_pixels.X = partial_sentence_size_pixels.X * text_scale;
                            partial_sentence_size_pixels.Y = partial_sentence_size_pixels.Y * text_scale;

                            //Create the red overlay texture
                            Texture2D red_overlay = new Texture2D(graphics_device, 1, 1, false, SurfaceFormat.Color);
                            red_overlay.SetData<Color>(new Color[] { new Color(0xFF, 0x00, 0x00, 0x33) });
                            
                            s.Draw(red_overlay,
                                new Rectangle(Convert.ToInt32(text_pos.X),
                                              Convert.ToInt32(text_pos.Y),
                                              Convert.ToInt32(partial_sentence_size_pixels.X),
                                              Convert.ToInt32(partial_sentence_size_pixels.Y)),
                                Color.White);
                        }
                    }
                }
            }
        }

        #endregion
    }
}