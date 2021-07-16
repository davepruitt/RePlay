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

namespace RePlay_Activity_TyperShark.Main
{
    public class SharkZapper
    {
        #region Private data members

        Texture2D shark_zapper_ui;
        Texture2D shark_zapper_rectangle;
        Texture2D shark_zapper_ready_msg;

        int shark_zapper_start_x = 1289;
        int shark_zapper_end_x = 2505;
        int shark_zapper_start_y = 48;
        int shark_zapper_middle_y = 72;
        int shark_zapper_end_y = 97;

        int shark_zapper_full_width = 1216;
        int shark_zapper_full_height = 50;

        int shark_zapper_max_value = 100;
        int shark_zapper_value = 0;
        int shark_zapper_rectangle_width = 0;

        int shark_zapper_ypos = 0;

        int shark_zapper_ready_msg_x = 0;
        int shark_zapper_ready_msg_y = 0;

        #endregion

        #region Constructor

        public SharkZapper ()
        {
            //empty
        }

        #endregion

        #region Public properties

        public int SharkZapperValue
        {
            get
            {
                return shark_zapper_value;
            }
            set
            {
                shark_zapper_value = Math.Min(shark_zapper_max_value, value);
                shark_zapper_rectangle_width = (shark_zapper_full_width * shark_zapper_value) / shark_zapper_max_value;
            }
        }

        public bool IsSharkZapperReady
        {
            get
            {
                return (shark_zapper_value >= shark_zapper_max_value);
            }
        }

        #endregion

        #region Event handler

        private void HandleStagePropertyChangedNotifications(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("WordCompleted"))
            {
                SharkZapperValue += 3;
            }
            else if (e.PropertyName.Equals("SharkCompleted"))
            {
                SharkZapperValue += 6;
            }
        }

        #endregion

        #region Methods

        public void SubscribeToStageNotifications (GameStage s)
        {
            s.PropertyChanged -= HandleStagePropertyChangedNotifications;
            s.PropertyChanged += HandleStagePropertyChangedNotifications;
        }
        
        public void LoadContent (ContentManager Content, GraphicsDevice GraphicsDevice)
        {
            //Load the UI texture for the shark zapper
            shark_zapper_ui = Content.Load<Texture2D>("shark_zapper_ui");
            shark_zapper_ready_msg = Content.Load<Texture2D>("shark_zapper_ready_msg");

            //Create the "shark zapper" rectangle texture
            shark_zapper_rectangle = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            shark_zapper_rectangle.SetData<Color>(new Color[] { Color.CornflowerBlue });

            //Set the yposition of the filled bar
            shark_zapper_ypos = Convert.ToInt32(GameConfiguration.VirtualScreenHeight) - shark_zapper_ui.Height + shark_zapper_start_y;

            int shark_zapper_middle_x = shark_zapper_start_x + (shark_zapper_full_width / 2);
            int temp_middle_y = Convert.ToInt32(GameConfiguration.VirtualScreenHeight) - shark_zapper_ui.Height + shark_zapper_middle_y;
            shark_zapper_ready_msg_x = shark_zapper_middle_x - (shark_zapper_ready_msg.Width / 2);
            shark_zapper_ready_msg_y = temp_middle_y - (shark_zapper_ready_msg.Height / 2);
        }

        public void Draw (SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(shark_zapper_ui, new Vector2(0, GameConfiguration.VirtualScreenHeight - shark_zapper_ui.Height), Color.White);
            spriteBatch.Draw(shark_zapper_rectangle, new Rectangle(shark_zapper_start_x, shark_zapper_ypos, shark_zapper_rectangle_width,
                shark_zapper_full_height), Color.White);

            if (shark_zapper_value >= shark_zapper_max_value)
            {
                spriteBatch.Draw(shark_zapper_ready_msg, new Rectangle(shark_zapper_ready_msg_x, shark_zapper_ready_msg_y, 
                    shark_zapper_ready_msg.Width, shark_zapper_ready_msg.Height), Color.White);
            }
        }

        #endregion
    }
}