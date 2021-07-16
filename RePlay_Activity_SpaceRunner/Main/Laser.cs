using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class Laser : Obstacle
    {
        #region Private Properties

        private const string laser_on_textures_folder = "laser_on/";
        private const string laser_texture = "laser";
        private int current_laser_texture = 0;
        private float LaserTimer = 0f;
        private float AnimateTimer = 0f;
        private ObstacleType Type { get; set; }
        private SpaceManager space_manager;

        #endregion

        #region Public Properties

        public bool IsActive { get; set; } = true;
        public static List<Texture2D> LaserOn;
        public static Texture2D LaserOff;

        #endregion

        #region Constructor

        public Laser(SpaceManager s, ObstacleType type, int posx, int posy, float startTime)
        {
            space_manager = s;
            Type = type;
            PositionX = posx;
            PositionY = posy;
            LaserTimer = startTime;   
        }

        public static void LoadContent(ContentManager content)
        {
            LaserOn = new List<Texture2D>();

            LaserOff = content.Load<Texture2D>(laser_texture);

            for(int i = 1; i < 16; i++)
            {
                string content_str = laser_on_textures_folder + "laser_on_" + i;
                var texture = content.Load<Texture2D>(content_str);
                LaserOn.Add(texture);
            }
        }

        #endregion

        #region Obstacles Overrides

        public override void Draw(SpriteBatch batch)
        {
            // Only draw laser if the laser is supposed to be on
            if (IsActive)
                batch.Draw(LaserOn[current_laser_texture], new Vector2(PositionX, PositionY), null, Color.White, 0f, new Vector2(LaserOn[current_laser_texture].Width / 2, LaserOn[current_laser_texture].Height / 2), 1f, SpriteEffects.None, .75f);

            // Always draw generators
            batch.Draw(LaserOff, new Vector2(PositionX, PositionY), null, Color.White, 0f, new Vector2(LaserOff.Width / 2, LaserOff.Height / 2), 1f, SpriteEffects.None, 1f);
        }

        public override void Update(GameTime time, GameState state, bool crashed)
        {
            if (state == GameState.RUNNING && !crashed)
            {
                PositionX -= space_manager.Speed;
                LaserTimer += time.ElapsedGameTime.Milliseconds;
                AnimateTimer += time.ElapsedGameTime.Milliseconds;

                // Determine if the laser is on or off
                if (IsActive && LaserTimer >= 2000)
                {
                    IsActive = false;
                    LaserTimer = 0;
                }
                else if (!IsActive && LaserTimer >= 2000)
                {
                    IsActive = true;
                    LaserTimer = 0;
                }

                // Handle flickering logic
                if (IsActive && AnimateTimer >= 25)
                {
                    current_laser_texture = (current_laser_texture == LaserOn.Count - 1) ? 0 : current_laser_texture + 1;
                    AnimateTimer = 0f;
                }
            }
        }

        public override bool IsOffScreen()
        {
            return PositionX + LaserOff.Width / 2 < 0;
        }

        public override Rectangle GetBodyRectangle()
        {
            return new Rectangle(PositionX - LaserOn[0].Width / 2, PositionY - LaserOn[0].Height / 2, LaserOn[0].Width, LaserOn[0].Height);
        }

        #endregion

    }
}