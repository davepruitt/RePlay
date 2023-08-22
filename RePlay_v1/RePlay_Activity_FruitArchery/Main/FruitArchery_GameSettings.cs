using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Common;
using RePlay_Common;
using FitMiAndroid;
using RePlay_Exercises;

namespace RePlay_Activity_FruitArchery.Main
{
    /// <summary>
    /// A public static class that can be used to store any game settings that need to be referenced anywhere in the game
    /// </summary>
    public static class FruitArchery_GameSettings
    {
        #region Public variables

        public static ExerciseType StimulationExercise = ExerciseType.FitMi_Grip;

        public static HIDPuckDongle PuckDongle;
        public static double ExerciseGain = 1.0;

        public static bool ShowPCMConnectionStatus = false;
        public static bool IsRePlayDebugMode = false;

        public static Random RandomNumberGenerator = new Random();

        public static int VirtualScreenWidth = 2560;
        public static int VirtualScreenHeight = 1600;

        public static double TimeRemainingInSeconds = 300;
        public static int CurrentGameScore = 0;
        public static FruitArchery_Stage CurrentStage = FruitArchery_Stage.Stage_01_StaticFruit;

        public static Dictionary<FruitArchery_FruitType, List<Vertices>> FruitCollisionPolygons = new Dictionary<FruitArchery_FruitType, List<Vertices>>();
        public static Dictionary<FruitArchery_FruitType, Texture2D> FruitTextures = new Dictionary<FruitArchery_FruitType, Texture2D>();
        public static Dictionary<FruitArchery_FruitSplatColor, Texture2D> FruitSplatTextures = new Dictionary<FruitArchery_FruitSplatColor, Texture2D>();
        public static Texture2D ArrowTexture;
        public static Texture2D BowTexture;
        public static Texture2D BackgroundTexture;

        #endregion

        #region Private variables

        private static string _arrow_texture_asset_name = "weapon_arrow";
        private static string _bow_texture_asset_name = "weapon_bow_sprite";
        private static string _background_texture_asset_name = "background";
        private static string _fruit_polygons_xml_file_resource = "fruit_polygons.xml";

        private static List<FruitArchery_FruitType> _shuffled_fruit_types = new List<FruitArchery_FruitType>();
        private static int _fruit_idx = 0;

        #endregion

        #region Public Methods

        public static void LoadGameTextures (ContentManager cm)
        {
            //Load the textures for the background, the bow, and the arrow
            BackgroundTexture = cm.Load<Texture2D>(_background_texture_asset_name);
            BowTexture = cm.Load<Texture2D>(_bow_texture_asset_name);
            ArrowTexture = cm.Load<Texture2D>(_arrow_texture_asset_name);

            //Load the texture for each fruit
            FruitTextures.Clear();
            var fruit_types = Enum.GetValues(typeof(FruitArchery_FruitType));
            foreach (FruitArchery_FruitType f in fruit_types)
            {
                //Load in the main texture for this fruit
                var f_description = FruitArchery_FruitTypeConverter.ConvertFruitTypeToAssetStringDescription(f);
                Texture2D f_texture = cm.Load<Texture2D>(f_description);
                FruitTextures.Add(f, f_texture);
            }
        }

        public static void LoadFruitPolygons (FruitArchery_World w)
        {
            FruitCollisionPolygons.Clear();

            AssetManager assets = Game.Activity.Assets;
            using (StreamReader sr = new StreamReader(assets.Open(_fruit_polygons_xml_file_resource)))
            {
                var xml_content = sr.ReadToEnd();
                XmlDocument xml_doc = new XmlDocument();
                xml_doc.LoadXml(xml_content);

                LoadFruitPolygonsFromXML(w, xml_doc.DocumentElement);
            }
        }

        public static FruitArchery_FruitType GetRandomFruitType ()
        {
            if (_shuffled_fruit_types.Count == 0 || _fruit_idx >= _shuffled_fruit_types.Count)
            {
                _shuffled_fruit_types.Clear();
                _fruit_idx = 0;
                
                var all_fruits = Enum.GetValues(typeof(FruitArchery_FruitType));
                _shuffled_fruit_types.AddRange(all_fruits.OfType<FruitArchery_FruitType>());
                _shuffled_fruit_types = _shuffled_fruit_types.ShuffleList();
            }

            var selected_fruit_type =  _shuffled_fruit_types[_fruit_idx];
            _fruit_idx++;
            return selected_fruit_type;
        }
        
        #endregion

        #region Private methods
        
        private static void LoadFruitPolygonsFromXML(FruitArchery_World w, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name.Equals("bodies"))
                {
                    var all_fruit_bodies = child.ChildNodes;
                    foreach (XmlNode fruit in all_fruit_bodies)
                    {
                        string this_fruit_name = fruit.Attributes["name"].Value;
                        FruitArchery_FruitType fruit_type = FruitArchery_FruitTypeConverter.ConvertDescriptionToFruitType(this_fruit_name);
                        List<Vertices> this_fruit_bodies = new List<Vertices>();
                        LoadPolygonsFromXML(fruit, this_fruit_bodies);
                        TransformPolygonPixelCoordinatesToWorldCoordinates(w, fruit_type, this_fruit_bodies);

                        FruitCollisionPolygons[fruit_type] = this_fruit_bodies;
                    }
                }
            }
        }

        private static void LoadPolygonsFromXML(XmlNode node, List<Vertices> polygon_list)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name.Equals("polygon"))
                {
                    Vertices new_polygon = LoadPolygonVerticesFromXML(child);
                    polygon_list.Add(new_polygon);
                }
                else
                {
                    LoadPolygonsFromXML(child, polygon_list);
                }
            }
        }

        private static Vertices LoadPolygonVerticesFromXML(XmlNode node)
        {
            List<Vector2> vertices = new List<Vector2>();
            foreach (XmlNode child_vertex in node.ChildNodes)
            {
                if (child_vertex.Name.Equals("vertex"))
                {
                    try
                    {
                        var vertex_attributes = child_vertex.Attributes;
                        var x_val_str = vertex_attributes["x"].Value;
                        var y_val_str = vertex_attributes["y"].Value;
                        bool x_success = float.TryParse(x_val_str, out float x_val);
                        bool y_success = float.TryParse(y_val_str, out float y_val);

                        if (x_success && y_success)
                        {
                            Vector2 new_vertex = new Vector2(x_val, y_val);
                            vertices.Add(new_vertex);
                        }
                    }
                    catch (Exception)
                    {
                        //empty
                    }
                }
            }

            return new Vertices(vertices);
        }

        private static void TransformPolygonPixelCoordinatesToWorldCoordinates(FruitArchery_World w, FruitArchery_FruitType t, List<Vertices> v)
        {
            var texture = FruitArchery_GameSettings.FruitTextures[t];

            var half_width = -(texture.Width / 2.0f);
            var half_height = -(texture.Height / 2.0f);

            foreach (Vertices polygon in v)
            {
                polygon.Scale(w.WorldScalingFactor);
            }
        }

        #endregion
    }
}