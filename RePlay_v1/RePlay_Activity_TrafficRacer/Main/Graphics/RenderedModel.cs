using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Main.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Graphics
{
    //Any object seen in game is a RenderedModel
	public class RenderedModel
	{
		protected Model Model;
		protected Texture2D Texture;

		public Vector3 Position;
		public Vector3 Offset;
		public Matrix Rotation = Matrix.Identity;
		public Vector3 Size = Vector3.One;
		public Vector3 Scale = Vector3.One;
		public Color Color = Color.White;

		Vector3 meshSize; //Size of bounding box (model space)

		static Dictionary<string, string> names = new Dictionary<string, string>();

		BoundingBox GetBounds()
		{
			if (Model.Tag == null)
			{
                //You MUST use the pre-processor even if you don't care about the visual effect.
                //The pre-processor computes mesh bounding boxes.
                //The bounding box cannot be computed at runtime using OpenGL ES.
				throw new Exception("Model must be processed using Jacob's Flat Processor");
			}
			return (BoundingBox)Model.Tag;
		}

		public RenderedModel(ContentManager content, string modelName, string textureName = null)
		{
			Model = content.Load<Model>(modelName);

			if (textureName != null)
			{
				Texture = content.Load<Texture2D>(textureName);
			}
			else
			{
                throw new Exception("Textures must be explicitly specified on Android!");
				if (names.ContainsKey(modelName))
				{
                    Texture = content.Load<Texture2D>(names[modelName]);
				}
				else
				{
					Texture = ((BasicEffect)Model.Meshes[0].Effects[0]).Texture;
					names.Add(modelName, Texture.Name);
				}
			}

			var bounds = GetBounds();
			meshSize = bounds.Max - bounds.Min;
		}

		public void Render(GameTime gameTime, Effect effect)
		{
			Matrix world =  Matrix.CreateScale(Size / meshSize * Scale) * Rotation * Matrix.CreateTranslation(Position + Offset);

            //Set shader parametrs
			effect.Parameters["World"].SetValue(world);
			effect.Parameters["NormalMatrix"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["LightMatrix"].SetValue(world * Lighting.View * Lighting.Projection);
			effect.Parameters["ModelTexture"].SetValue(Texture);

            //The render hack resets chroma key replace.
            //This is another hack which restores the chroma key to what it was before the hack.
            Vector4 ChromaKeyReplace = effect.Parameters["ChromaKeyReplace"].GetValueVector4();
            RenderHack.RENDER_FIX(effect);
            effect.Parameters["ChromaKeyReplace"].SetValue(ChromaKeyReplace);

            //Render the mesh
            foreach (ModelMesh mesh in Model.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = effect;

				}
				mesh.Draw();
			}
		}
	}
}
