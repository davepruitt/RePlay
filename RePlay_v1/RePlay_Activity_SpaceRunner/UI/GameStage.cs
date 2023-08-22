using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_SpaceRunner.Main;
using System;
using System.IO;

namespace RePlay_Activity_SpaceRunner.UI
{
    public class GameStage
    {
        #region Background types

        private string space_texture_asset_name = "space_1";
        private string space_texture_2_asset_name = "space_2";
        private string space_rocket_asset_name = "rocket_with_astronaut";
        private string space_rocket_astro_asset_name = "rocket";

        #endregion

        #region Private data members

        private Texture2D space_texture_1;
        private Texture2D space_texture_2;
        private Texture2D rocketship;
        private Texture2D rocketship_astro;
        private Random rand = new Random();
        private TimeSpan shake_start;
        private Astronaut player;
        private SpaceRunnerGame spacerunner;

        private int space1_x;
        private int space2_x;
        private int x_velocity = 150;
        private int rocket_x;
        private int rocket_y;
        private int shake_start_angle;
        private float shake_radius;
        private bool shake_viewport = false;
        private bool enter_rocket = false;
        private bool draw_rocket = true;
        private bool astro_launched = false;
        private bool starting_animation = false;
        
        #endregion

        public Vector2 Offset = Vector2.Zero;

        #region Constructor

        public GameStage(Astronaut astro, SpaceRunnerGame game)
        {
            player = astro;
            spacerunner = game;
        }

        #endregion

        #region Methods

        public void BeginAnimation()
        {
            starting_animation = true;
            enter_rocket = true;
        }

        public void EndAnimation()
        {
            starting_animation = false;
            spacerunner.FinishedStarting();
        }

        public void LoadContent(ContentManager Content)
        {
            // Load the space textures
            space_texture_1 = Content.Load<Texture2D>(space_texture_asset_name);
            space_texture_2 = Content.Load<Texture2D>(space_texture_2_asset_name);

            // Load the rocketship
            rocketship = Content.Load<Texture2D>(space_rocket_asset_name);
            rocketship_astro = Content.Load<Texture2D>(space_rocket_astro_asset_name);

            // Starting x coords
            space1_x = SpaceRunnerGame.VirtualScreenWidth / 2;
            space2_x = space1_x + space_texture_1.Width / 2 + space_texture_2.Width / 2;
            rocket_x = -rocketship.Width * 2;
            rocket_y = -rocketship.Height * 4;
        }

        public void Update(GameTime gameTime, GameState state, bool crashed)
        {
            if (state == GameState.STARTING && starting_animation)
            {
                // Starting animation logic
                if (enter_rocket)
                {
                    int pixel_change = Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds * 0.5);
                    if (rocket_x <= SpaceRunnerGame.VirtualScreenWidth / 5)
                    {
                        rocket_x += pixel_change;
                    }

                    if (rocket_y <= SpaceRunnerGame.VirtualScreenHeight / 2)
                    {
                        rocket_y += pixel_change;
                    }

                    // Rocket has entered the screen, shake viewport
                    if (rocket_x + rocketship.Width / 2 >= 0 && rocket_y >= 0 && Offset == Vector2.Zero)
                    {
                        ShakeViewport(gameTime);
                    }
                    
                    if (rocket_x >= SpaceRunnerGame.VirtualScreenWidth / 5)
                    {
                        RocketEntered();
                    }
                }

                // Shake the viewport after the rocket arrives
                if (shake_viewport)
                {
                    Offset = new Vector2((float)(Math.Sin(shake_start_angle) * shake_radius), (float)(Math.Cos(shake_start_angle) * shake_radius));
                    shake_radius -= 0.25f;
                    shake_start_angle = (150 + rand.Next(60));
                    if (gameTime.TotalGameTime.TotalSeconds - shake_start.TotalSeconds > 5 || shake_radius <= 0)
                    {
                        shake_viewport = false;
                    }
                }
            }
            else if (state == GameState.RUNNING && !crashed)
            {
                // Move the space background so it looks like we are flying through space
                int pixels_to_move = Convert.ToInt32(x_velocity * gameTime.ElapsedGameTime.TotalSeconds);
                space1_x -= pixels_to_move;
                space2_x -= pixels_to_move;

                if (space1_x + space_texture_1.Width / 2 <= 0)
                {
                    space1_x = space2_x + space_texture_2.Width / 2 + space_texture_1.Width / 2;
                }

                if (space2_x + space_texture_2.Width / 2 <= 0)
                {
                    space2_x = space1_x + space_texture_1.Width / 2 + space_texture_2.Width / 2;
                }

                if (draw_rocket)
                {
                    rocket_x -= pixels_to_move;
                    if (rocket_x + rocketship.Width / 2 <= 0) draw_rocket = false;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(space_texture_1, new Vector2(space1_x, SpaceRunnerGame.VirtualScreenHeight / 2), null, Color.White, 0, new Vector2(space_texture_1.Width / 2, space_texture_1.Height / 2), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(space_texture_1, new Vector2(space2_x, SpaceRunnerGame.VirtualScreenHeight / 2), null, Color.White, 0, new Vector2(space_texture_2.Width / 2, space_texture_2.Height / 2), 1f, SpriteEffects.None, 0f);
            if (draw_rocket)
            {
                if (player.Flying)
                {
                    spriteBatch.Draw(rocketship_astro, new Vector2(rocket_x, rocket_y), 
                        null, Color.White, 0, new Vector2(rocketship.Width / 2, rocketship.Height / 2), 
                        1f, SpriteEffects.None, 1f);
                }
                else
                {
                    spriteBatch.Draw(rocketship, new Vector2(rocket_x, rocket_y), 
                        null, Color.White, 0, new Vector2(rocketship.Width / 2, rocketship.Height / 2), 
                        1f, SpriteEffects.None, 1f);
                }
            }
        }

        public void SaveSpaceStageData(BinaryWriter file_stream)
        {
            file_stream.Write(space_texture_1.Width);
            file_stream.Write(space_texture_1.Height);
            file_stream.Write(space1_x);

            file_stream.Write(space_texture_2.Width);
            file_stream.Write(space_texture_2.Height);
            file_stream.Write(space2_x);

            file_stream.Write(rocketship.Width);
            file_stream.Write(rocketship.Height);

            file_stream.Write(rocket_x);
            file_stream.Write(rocket_y);
        }

        #endregion

        private void ShakeViewport(GameTime time)
        {
            shake_viewport = true;
            shake_start_angle = rand.Next(180);
            shake_radius = 25f;
            shake_start = time.TotalGameTime;
        }

        private void RocketEntered()
        {
            player.LaunchAstronaut(rocket_y + 50);
            astro_launched = true;
            enter_rocket = false;
            EndAnimation();
        }
        
    }
}