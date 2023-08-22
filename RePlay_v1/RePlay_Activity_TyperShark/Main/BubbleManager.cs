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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using RePlay_Common;

namespace RePlay_Activity_TyperShark.Main
{
    public class BubbleManager
    {
        #region Private data members
        
        private int minimum_bubble_velocity = 400;
        private int maximum_bubble_velocity = 600;

        private int bubble_x_spread = 0;
        private double bubble_frequency_mean = 0;
        private double bubble_frequency_stddev = 0;
        private double bubble_accumulator = 0;
        private double current_bubble_multiplier = 0.1;
        private int bubble_ypos_start = 0;

        private List<Bubble> bubbles = new List<Bubble>();

        #endregion

        #region Constructor

        public BubbleManager (int x_spread, double frequency_mean, double frequency_stddev, int ypos)
        {
            bubble_x_spread = x_spread;
            bubble_frequency_mean = frequency_mean;
            bubble_frequency_stddev = frequency_stddev;
            bubble_ypos_start = ypos;

            current_bubble_multiplier = RePlay_Common.RandomNumberStatic.RandomNumbers.NextGaussian(bubble_frequency_mean, bubble_frequency_stddev);
            if (current_bubble_multiplier < 0.1)
            {
                current_bubble_multiplier = 0.1;
            }
        }

        #endregion

        #region Public methods

        public int X_Spread
        {
            get
            {
                return bubble_x_spread;
            }
            set
            {
                bubble_x_spread = value;
            }
        }

        public bool IsActive { get; set; } = true;

        #endregion

        #region Methods
        
        public void Update (GameTime gameTime)
        {
            if (IsActive)
            {
                double bubbles_to_generate = current_bubble_multiplier * gameTime.ElapsedGameTime.TotalSeconds;
                bubble_accumulator += bubbles_to_generate;

                //Create some new bubbles
                if (bubble_accumulator >= 1.0)
                {
                    int num_bubbles = Convert.ToInt32(Math.Round(bubble_accumulator));
                    for (int i = 0; i < num_bubbles; i++)
                    {
                        int vel = -RePlay_Common.RandomNumberStatic.RandomNumbers.Next(minimum_bubble_velocity, maximum_bubble_velocity);
                        int xpos = Convert.ToInt32(GameConfiguration.VirtualScreenHalfWidth) +
                            (RePlay_Common.RandomNumberStatic.RandomNumbers.Next(0, bubble_x_spread * 2) - bubble_x_spread);

                        int bubble_type = RePlay_Common.RandomNumberStatic.RandomNumbers.Next(GameConfiguration.BubbleTextures.Count - 1);
                        int ypos = bubble_ypos_start + GameConfiguration.BubbleTextures[bubble_type].Height;

                        Bubble new_bubble = new Bubble(vel, new Vector2(xpos, ypos), GameConfiguration.BubbleTextures[bubble_type]);
                        bubbles.Add(new_bubble);
                    }

                    //Calculate a new bubble multiplier for the next bubble
                    current_bubble_multiplier = RePlay_Common.RandomNumberStatic.RandomNumbers.NextGaussian(bubble_frequency_mean, bubble_frequency_stddev);
                    if (current_bubble_multiplier < 0.1)
                    {
                        current_bubble_multiplier = 0.1;
                    }

                    //Reset the bubble accumulator
                    bubble_accumulator = 0;
                }
            }

            //Update all bubbles
            bubbles.ForEach(x => x.Update(gameTime));

            //Remove all bubbles that have passed beyond the top of the screen
            bubbles.RemoveAll(x => (x.BubblePosition.Y + x.BubbleTexture.Height) < 0);
        }

        public void Draw (SpriteBatch spriteBatch)
        {
            //Draw all of the bubbles in the list
            bubbles.ForEach(x => x.Draw(spriteBatch));
        }

        #endregion
    }
}