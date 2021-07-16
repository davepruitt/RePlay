using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using RePlay_Activity_TrafficRacer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;

namespace RePlay_Activity_TrafficRacer.Environment
{
	public class Rail : RenderedModel
	{
		const string modelName = "rail2";
        const string texName = "RailTex";

        World world;
        Body body;

		public Rail(ContentManager content, World world, float x, float y) : base(content, modelName, texName)
		{
			this.Offset = new Vector3(0.25f, -5, -0.25f);

			this.Position = new Vector3(x, y, 0);
			this.Size = new Vector3(0.5f, Road.Size, 1f);

            this.world = world;
            body = world.CreateRectangle(Size.X, Size.Y, 1, new Vector2(Position.X, Position.Y));
            body.SetFriction(0);
            body.SetCollisionGroup(5);
		}

        public void Destroy()
        {
            world.Remove(body);
            body = null;
        }
	}
}
