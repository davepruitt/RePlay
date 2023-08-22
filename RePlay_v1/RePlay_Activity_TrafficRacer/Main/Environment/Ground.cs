using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Graphics;
using RePlay_Activity_TrafficRacer.Main.Graphics;
using RePlay_Activity_TrafficRacer.Utility;
using SharpNoise.Modules;

namespace RePlay_Activity_TrafficRacer.Environment
{
    //Represents one segment of ground which is the same size as a road segment
    //Each of these segments is composed of a square grid of tiles
    //Each tile is composed of 2 triangles
	public class Ground
	{
        //Parameters
        const int length = 4; //Number of tiles in y direction
        const int width = 24; //Number of tiles
        const float biomeScale = 100; //Changes how frequently the ground change colors

        //State
        public float positionY;

        float[,] heightMap; //The height of each point
        VertexPositionColorNormal[] vertices; //List of vertices, grouped by triangle

        //Noise generator for smooth terrain
        static Perlin noiseSource = new Perlin
        {
            Seed = new Random().Next()
        };

        //Helper values
        readonly Vector2 scale = new Vector2(Road.Size / length);
        readonly Vector2[] offsets = new Vector2[] //Each tile has 6 vertices. This array gives the correct position offset given a local index
        {
            new Vector2(0, 0),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        //Custom vertex definition which has only Color, Normal, Position
        struct VertexPositionColorNormal : IVertexType
        {
            public Color Color;
            public Vector3 Normal;
            public Vector3 Position;

            public static readonly VertexElement[] VertexElements = {
                new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)
            };

            VertexDeclaration IVertexType.VertexDeclaration
            {
                get { return new VertexDeclaration(VertexElements); }
            }
        }

        public Ground(ContentManager content, float Y)
        {
            heightMap = new float[width + 1, length + 1];
            vertices = new VertexPositionColorNormal[width * length * 6]; //we are intentionally duplicating vertices, hence the * 6 and lack of indexing
            positionY = Y;

            var random = new Random();

            //Generate terrain
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= length; y++)
                {
                    float value = ((float)noiseSource.GetValue(x * 0.5f, (y + positionY / scale.Y) * 0.5f, 0.5f) + 0) / 1;

                    //If this tile is under the road then make it have 0 height so it doesn't stick out
                    if (Math.Abs(x - width / 2) * scale.X < Road.Size * 0.6f)
                    {
                        value = 0;
                    }

                    heightMap[x, y] = value;
                }
            }

            //After generating the heightmap, generate the actual vertices
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    int i = (width * y + x) * 6; //Starting vertex index for this tile

                    VertexPositionColorNormal vertex;

                    //6 vertices per tile (x, y)
                    for (int j = 0; j < 6; j++)
                    {
                        vertex = new VertexPositionColorNormal();

                        //Assign position
                        float ox = x + offsets[j].X;
                        float oy = y + offsets[j].Y;
                        float height = heightMap[(int)ox, (int)oy];
                        vertex.Position = new Vector3(ox * scale.X, oy * scale.Y, height * 1.5f);

                        //Generate base color based on the following array
                        //Each index corresponds to a different "biome"
                        //The base color therefore implicitly depends on the biome (and usually explicitly depends on height)
                        Vector3[] colors =
                        {
                            new Vector3(0.2f, (heightMap[(int)ox, (int)oy]+1)/4 + 0.3f, 0.3f),
                            new Vector3(height.Map(-1, 1, 0.4f, 0.9f), 0.4f, 0.3f),
                            new Vector3(height.Map(-1, 1, 0.7f, 0.9f), height.Map(-1, 1, 0.6f, 0.8f), height.Map(-1, 1, 0.2f, 0.5f)),
                        };

                        //Determine which biome to use based on current position
                        float val = Math.Abs(y + positionY) / biomeScale % colors.Length;

                        int primary = (int)val; //Current biome color
                        int secondary = (primary + 1) % colors.Length; //Next biome color

                        //Smoothly blend between the biome colors if we are in between biomes
                        float blendAmt = 1 - (float)Math.Pow((val - primary) * 2 - 1f, 2);
                        if (val - primary < 0.8f)
                        {
                            blendAmt = 1;
                        }
                        Vector3 blended = colors[primary] * blendAmt + colors[secondary] * (1 - blendAmt);

                        //Assign color
                        Color color = new Color(blended.X, blended.Y, blended.Z);
                        vertex.Color = color;

                        vertices[i + j] = vertex; //Base tile index + local index
                    }
                }
            }

            //These calculations depend on an entire triangle, so I compute them afterwards
            for (int i = 0; i < vertices.Length; i += 3)
            {
                //Compute normal
                Vector3 u = vertices[i].Position - vertices[i + 1].Position;
                Vector3 v = vertices[i].Position - vertices[i + 2].Position;
                Vector3 normal = Vector3.Cross(v, u);
                normal.Normalize();

                //Compute average color of the 3 vertices of a triangle
                Color color = Color.White;
                color.R = Math.Max(Math.Max(vertices[i + 0].Color.R, vertices[i + 1].Color.R), vertices[i + 2].Color.R);
                color.G = Math.Max(Math.Max(vertices[i + 0].Color.G, vertices[i + 1].Color.G), vertices[i + 2].Color.G);
                color.B = Math.Max(Math.Max(vertices[i + 0].Color.B, vertices[i + 1].Color.B), vertices[i + 2].Color.B);

                //Assign the same normal and average color to all 3 vertices
                for (int j = 0; j < 3; j++)
                {
                    vertices[i + j].Normal = normal;
                    vertices[i + j].Color = color;
                }
            }
        }

        public void Render(GraphicsDevice graphics, Effect effect)
		{
			var prevTechnique = effect.CurrentTechnique; //"Push" technique which was previously being used

            //Set shader parameters
			Matrix world = Matrix.CreateTranslation(Vector3.Left * ((float)width * scale.X / 2f) + Vector3.UnitY * positionY);
			effect.CurrentTechnique = effect.Techniques["ShadowedTerrain"];
			effect.Parameters["World"].SetValue(world);
			effect.Parameters["NormalMatrix"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["LightMatrix"].SetValue(world * Lighting.View * Lighting.Projection);
            RenderHack.RENDER_FIX(effect);

            //Render the vertices
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				graphics.DrawUserPrimitives<VertexPositionColorNormal>(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
			}

			effect.CurrentTechnique = prevTechnique; //"Pop" previous technique
		}
	}
}