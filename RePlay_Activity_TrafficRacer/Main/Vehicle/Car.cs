using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Common;
using Microsoft.Xna.Framework.Content;
using RePlay_Activity_TrafficRacer;
using RePlay_Activity_TrafficRacer.Graphics;
using RePlay_Activity_TrafficRacer.Environment;
using RePlay_Activity_TrafficRacer.Utility;
using System.IO;

namespace RePlay_Activity_TrafficRacer.Vehicle
{
	public class Car
	{
        //Parameters
        static readonly Color[] colors =
        {
            new Color(229, 189, 15, 255),
            new Color(229, 28, 28, 255),
            new Color(242, 139, 245, 255),
            new Color(39, 119, 238, 255),
            new Color(39, 238, 119, 255),
            new Color(238, 133, 39, 255),
            new Color(236, 247, 250, 255),
            new Color(41, 41, 41, 255)
        };

		//State
		public Guid UniqueID = Guid.NewGuid();
        public Body Body;
        CarType Type;
        public RenderedModel model;
        public Vector2 BodySize;

        float DesiredSpeed;
		float InitialSpeed;
        public int Lane;

		public List<CastedRay> Rays = new List<CastedRay>();
        World World;

        public Vector2 Position
		{
			get { return Body.Position; }
			set { Body.Position = value; }
		}

		public Vector2 Velocity
		{
			get { return Body.LinearVelocity; } //Return actual velocity
			set { //Set real X velocity but only desired Y velocity
				Body.LinearVelocity = new Vector2(value.X, Body.LinearVelocity.Y);
				DesiredSpeed = value.Y;
			}
		}

		public Car(ContentManager content, CarType type, World world, float initialSpeed)
		{
			this.World = world;
			Type = type;
			DesiredSpeed = initialSpeed;
			InitialSpeed = initialSpeed;

			model = new RenderedModel(content, type.ModelName, type.TextureName);
			model.Color = colors[new Random().Next(0, colors.Length)];        
      
			BodySize = new Vector2(type.Width, type.Length);

			float rTop = Type.RadiusTop;
			float rBot = Type.RadiusBottom;

            //Some cars rounded bumpers. This code computes the points for these curves. (see the debug view)
			Vertices verts = new Vertices();
			int steps = 4;
			float angleStep = MathHelper.Pi / steps;

			//Top curve
			for (int i = 0; i <= steps; i++)
			{
				float angle = angleStep * i;
				verts.Add(new Vector2((float)Math.Cos(angle) * BodySize.X / 2, (float)Math.Sin(angle) * rTop + (BodySize.Y/2 - rTop)));
			}

			//Bottom curve
			for (int i = 0; i <= steps; i++)
			{
				float angle = angleStep * i + MathHelper.Pi;
				verts.Add(new Vector2((float)Math.Cos(angle) * BodySize.X / 2, (float)Math.Sin(angle) * rBot - (BodySize.Y / 2 - rBot)));
			}

            //Create the physics object
			Body = world.CreateBody();
			Body.CreatePolygon(verts, 1f);
			Body.BodyType = BodyType.Dynamic;
			Body.AngularDamping = 0.5f;
			Body.LinearVelocity = new Vector2(0, initialSpeed);
		}

        //Cast a ray and return what we hit (if its dynamic)
		public Body RayCast(Vector2 p1, Vector2 p2)
		{
			List<Fixture> hits = World.RayCast(p1, p2);

			bool hit = hits.Count > 0;

			Rays.Add(new CastedRay(p1, p2, hit));

			if (hit && hits[0].Body.BodyType == BodyType.Dynamic)
			{
				return hits[0].Body;
			}
			return null;
		}

        //Cast several rays in front of the car and return what we hit
		public Body AnticipateCollision(float maxDistance)
		{
			int numRays = 4;

			Body[] bodies = new Body[numRays];

			for (int i = 0; i < numRays; i++)
			{
				Vector2 p1 = new Vector2(Position.X + i.Map(0, 3, -Type.Width / 2, Type.Width / 2), Position.Y + BodySize.Y / 2);
				Vector2 p2 = p1 + Vector2.UnitY * maxDistance;

				bodies[i] = RayCast(p1, p2);
			}

			for (int i = 0; i < numRays; i++)
			{
				if (bodies[i] != null)
				{
					return bodies[i];
				}
			}

			return null;
		}

		public void Update(GameTime gameTime)
		{
			Random random = new Random((int)gameTime.TotalGameTime.TotalMilliseconds + (int)(Velocity.Y*100) + GetHashCode());

			Body b = AnticipateCollision(3);

			if (b != null) //If we are about to crash into something
			{
                //Slow down
				Velocity = new Vector2(0, Math.Max(0, Math.Min(Velocity.Y, b.LinearVelocity.Y - 1)));
				if (Velocity.Y <= 0.1f) //Hack to prevent cars from inching forward
				{
					Velocity = new Vector2(0, 0);
				}
			}
			else
			{
				Velocity = new Vector2(0, InitialSpeed); //Speed back up to original speed
			}

            //Make real velocity approach desired velocity
			if (Math.Abs(Velocity.Y - DesiredSpeed) > 0.1)
			{
				Body.LinearVelocity = new Vector2(0, Velocity.Y * 0.9f + DesiredSpeed * 0.1f);
			}

            //Make real angle approach desired angle which is based on lateral velocity
			float desiredAngle = -Velocity.X * 0.05f;
			Body.Rotation = Body.Rotation * 0.5f + desiredAngle * 0.5f;
		}

		public void Render(GameTime gameTime, Effect effect)
		{
			model.Position = new Vector3(Position, 1.25f);
			model.Size = new Vector3(Type.Length, Type.Width, Type.Height);
			model.Rotation = Matrix.CreateFromAxisAngle(Vector3.Forward, -MathHelper.PiOver2) * Matrix.CreateFromAxisAngle(Vector3.Backward, Body.Rotation);// new Vector3(0, 0, Body.Rotation);
            model.Offset = Type.Offset;
            effect.Parameters["ChromaKeyReplace"].SetValue(model.Color.ToVector4());
			model.Render(gameTime, effect);
		}


        public static void LoadContent(ContentManager content)
        {
            CarType.LoadContent(content);
        }
	}

    //Helper class used to render raycasts in debug view
    public class CastedRay
    {
        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly bool Hit;

        public CastedRay(Vector2 p1, Vector2 p2, bool hit)
        {
            P1 = p1;
            P2 = p2;
            Hit = hit;
        }
    }
}
