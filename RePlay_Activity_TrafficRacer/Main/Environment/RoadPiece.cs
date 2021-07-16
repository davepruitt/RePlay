using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using RePlay_Activity_TrafficRacer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Environment
{
	public class RoadPiece : RenderedModel
	{
		const string modelName = "road_model";
        const string texName = "road";

		public RoadPiece(ContentManager content, float y) : base(content, modelName, texName)
		{
			this.Scale = new Vector3(1, 1.05f, 1);
			this.Position = new Vector3(0, y, 0);
			this.Rotation = Matrix.CreateFromYawPitchRoll(0, -MathHelper.Pi, 0);
			this.Size = new Vector3(Road.Size, Road.Size, 0.1f);
		}
	}
}
