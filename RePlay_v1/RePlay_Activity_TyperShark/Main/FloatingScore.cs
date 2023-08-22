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
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_TyperShark.Main
{
    public class FloatingScore
    {
        #region Constant private data members

        private int y_velocity = -200;

        #endregion

        #region Private data members

        private Color score_color = Color.Yellow;
        private SpriteFont score_font;
        private Vector2 score_dimensions;
        private Vector2 score_position;
        private string score_text;
        private DateTime score_start_time = DateTime.MinValue;

        #endregion

        #region Constructor

        public FloatingScore(SpriteFont f, Vector2 position, int score)
        {
            score_font = f;
            score_position = position;
            score_text = "+" + score.ToString();
            score_dimensions = f.MeasureString(score_text);
            score_start_time = DateTime.Now;
        }

        #endregion

        #region Public properties

        public bool IsOutOfBounds
        {
            get
            {
                return ((score_position.Y + (score_dimensions.Y * 2)) < 0);
            }
        }

        public DateTime ScorePostedTime
        {
            get
            {
                return score_start_time;
            }
        }

        #endregion

        #region Methods

        public void Update (GameTime t)
        {
            //Update the y-position
            double num_pixels_moved_y = y_velocity * t.ElapsedGameTime.TotalSeconds;
            num_pixels_moved_y = Math.Max(Int32.MinValue, Math.Min(Int32.MaxValue, Math.Round(num_pixels_moved_y)));
            score_position.Y += Convert.ToSingle(num_pixels_moved_y);

            //Update the color
            if (DateTime.Now >= (score_start_time + TimeSpan.FromSeconds(1.0)))
            {
                score_color = Color.Red;
            }
        }

        public void Draw (SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(score_font, score_text, score_position, score_color, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        }

        #endregion
    }
}