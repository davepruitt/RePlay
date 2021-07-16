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
    public class GameBackground
    {
        #region Background types

        public enum BackgroundType
        {
            ClearDay,
            Iceberg,
            Lighthouse,
            Shipwreck,
            Storm,
            Volcano
        }

        public Dictionary<BackgroundType, string> BackgroundTypeAssetNames = new Dictionary<BackgroundType, string>()
        {
            { BackgroundType.ClearDay, "top_background_clear" },
            { BackgroundType.Iceberg, "top_background_iceberg" },
            { BackgroundType.Lighthouse, "top_background_lighthouse" },
            { BackgroundType.Shipwreck, "top_background_shipwreck" },
            { BackgroundType.Storm, "top_background_storm" },
            { BackgroundType.Volcano, "top_background_volcano" },
        };

        private string water_texture_1_asset_name = "water";
        private string water_texture_2_asset_name = "water2";
        private string mountain_texture_1_asset_name = "mountain1";
        private string mountain_texture_2_asset_name = "mountain2";
        private string mountain_texture_3_asset_name = "mountain3";
        private string ocean_floor_asset_name = "oceanfloor";

        #endregion

        #region Private data members

        private BubbleManager bubble_manager = new BubbleManager(Convert.ToInt32(GameConfiguration.VirtualScreenHalfWidth), 0.5, 0.5, 
            Convert.ToInt32(GameConfiguration.VirtualScreenHeight));

        private Dictionary<BackgroundType, Texture2D> top_background_texture = new Dictionary<BackgroundType, Texture2D>();
        private Texture2D water_texture_1;
        private Texture2D water_texture_2;
        private Texture2D ocean_floor;
        private List<Texture2D> mountains = new List<Texture2D>();
        private BackgroundType current_top_background_type = BackgroundType.ClearDay;

        private int top_background_y = 0;
        private int water1_y = 0;
        private int water2_y = 0;
        private int y_velocity = -150;
        private bool descent_active = false;
        private bool fully_underwater = false;

        private bool place_ocean_floor_flag = false;
        private bool is_ocean_floor_placed = false;
        private int ocean_floor_y = 0;
        private int mountains_y = 0;
        private int chosen_mountain_idx = 0;


        #endregion

        #region Constructor

        public GameBackground ()
        {
            //empty
        }

        #endregion

        #region Properties

        public bool IsDescentActive
        {
            get
            {
                return descent_active;
            }
        }

        #endregion

        #region Methods

        public void PlaceOceanFloor ()
        {
            if (!is_ocean_floor_placed)
            {
                place_ocean_floor_flag = true;
                chosen_mountain_idx = RePlay_Common.RandomNumberStatic.RandomNumbers.Next(mountains.Count - 1);
            }
        }

        public void BeginDescent ()
        {
            descent_active = true;
        }

        public void StopDescent ()
        {
            descent_active = false;
        }

        public void ResetBackground (BackgroundType new_background_type = BackgroundType.ClearDay)
        {
            current_top_background_type = new_background_type;
            top_background_y = 0;
            water1_y = top_background_texture[current_top_background_type].Height;
            water2_y = water1_y + water_texture_2.Height;
            descent_active = false;
            place_ocean_floor_flag = false;
            is_ocean_floor_placed = false;
            fully_underwater = false;
        }

        public void LoadContent (ContentManager Content)
        {
            //Load each texture for the "top background"
            foreach (BackgroundType t in BackgroundTypeAssetNames.Keys)
            {
                top_background_texture[t] = Content.Load<Texture2D>(BackgroundTypeAssetNames[t]);
            }

            //Load the water textures
            water_texture_1 = Content.Load<Texture2D>(water_texture_1_asset_name);
            water_texture_2 = Content.Load<Texture2D>(water_texture_2_asset_name);

            //Load the assets for the ocean floor
            ocean_floor = Content.Load<Texture2D>(ocean_floor_asset_name);
            mountains.Add(Content.Load<Texture2D>(mountain_texture_1_asset_name));
            mountains.Add(Content.Load<Texture2D>(mountain_texture_2_asset_name));
            mountains.Add(Content.Load<Texture2D>(mountain_texture_3_asset_name));
            
            //Reset the background
            ResetBackground();
        }

        public void Update (GameTime gameTime)
        {
            if (descent_active)
            {
                int pixels_to_move = Convert.ToInt32(y_velocity * gameTime.ElapsedGameTime.TotalSeconds);
                
                if (top_background_y >= -(top_background_texture[current_top_background_type].Height * 2))
                {
                    top_background_y += pixels_to_move;
                }
                else
                {
                    fully_underwater = true;
                }

                //Move the water textures as necessary
                water1_y += pixels_to_move;
                water2_y += pixels_to_move;

                if (place_ocean_floor_flag)
                {
                    place_ocean_floor_flag = false;
                    is_ocean_floor_placed = true;
                    ocean_floor_y = Convert.ToInt32(GameConfiguration.VirtualScreenHeight + 10);
                    mountains_y = ocean_floor_y;
                }
                
                if (water1_y <= -(water_texture_1.Height))
                {
                    water1_y = water2_y + water_texture_2.Height;
                }

                if (water2_y <= -(water_texture_2.Height))
                {
                    water2_y = water1_y + water_texture_1.Height;
                }

                if (is_ocean_floor_placed)
                {
                    ocean_floor_y += pixels_to_move;
                    mountains_y += pixels_to_move;

                    if (ocean_floor_y <= (GameConfiguration.VirtualScreenHeight - ocean_floor.Height))
                    {
                        ocean_floor_y = Convert.ToInt32(GameConfiguration.VirtualScreenHeight) - ocean_floor.Height;
                        mountains_y = ocean_floor_y;
                        descent_active = false;
                    }
                }
            }

            if (fully_underwater)
            {
                bubble_manager.Update(gameTime);
            }
        }

        public void Draw (SpriteBatch spriteBatch)
        {
            if (top_background_y >= -(top_background_texture[current_top_background_type].Height * 2))
            {
                spriteBatch.Draw(top_background_texture[current_top_background_type], new Vector2(0, top_background_y), Color.White);
            }

            spriteBatch.Draw(water_texture_1, new Vector2(0, water1_y), Color.White);
            spriteBatch.Draw(water_texture_2, new Vector2(0, water2_y), Color.White);

            if (is_ocean_floor_placed)
            {
                spriteBatch.Draw(mountains[chosen_mountain_idx], new Vector2(0, mountains_y), Color.White);
                spriteBatch.Draw(ocean_floor, new Vector2(0, ocean_floor_y), Color.White);
            }

            if (fully_underwater)
            {
                bubble_manager.Draw(spriteBatch);
            }
        }

        #endregion
    }
}