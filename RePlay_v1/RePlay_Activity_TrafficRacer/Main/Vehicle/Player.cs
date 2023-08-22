using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using tainicom.Aether.Physics2D.Collision;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Dynamics.Contacts;

namespace RePlay_Activity_TrafficRacer.Vehicle
{
    public class Dodge
	{
		public Body Body;
		public double Time;

		public Dodge(Body body, double time)
		{
			Body = body;
			Time = time;
		}
	}

	public class Player : Car
	{
        const float maxLateralSpeed = 3.25f;
        readonly Color playerColor = new Color(229, 189, 15, 255);

        private bool hitWallRight = false;
        private bool hitWallLeft = false;
        private float bounceLateralSpeed = 0f;
        private float bounceRotation = 0f;
        private float bounceTimer = 0f;

        public bool crashed = false;
		float crashTime; //Time when we hit something

		float previousTime; //Time of most recent update tick

		Body ApproachingBody; //Car that we may be about to run into

		public delegate void DodgeCompleteDelegate(Body b);
		public DodgeCompleteDelegate DodgeCompleteCallback;

        public delegate void CoinGetDelegate(Body b);
        public CoinGetDelegate CoinGetCallback;

        Queue<Dodge> Dodges = new Queue<Dodge>();

        float initialSpeed; //Speed which player should go

		public new Vector2 Velocity
		{
			get { return Body.LinearVelocity; }
			set
			{
				Body.LinearVelocity = new Vector2(value.X, Body.LinearVelocity.Y);
				initialSpeed = value.Y;
			}
		}

		public Player(ContentManager content, CarType type, World world, float initialSpeed) : base(content, type, world, initialSpeed)
		{
            this.initialSpeed = initialSpeed;
			Body.OnCollision += Collision;
			Body.Mass = 4000.0f;
			Reset();

            model.Color = playerColor;
		}

		public void Reset()
		{
			crashed = false;
			Position = Vector2.Zero;
			Body.Rotation = 0;
			Body.LinearDamping = 0.0f;
			Body.LinearVelocity = new Vector2(0, initialSpeed);
			Body.SetFriction(0);
		}

        public void ResetToCenter ()
        {
            crashed = false;
            Position = new Vector2(0, Position.Y);
            Body.Rotation = 0;
            Body.LinearDamping = 0.0f;
            Body.LinearVelocity = new Vector2(0, initialSpeed);
            Body.SetFriction(0);
        }

		public void Update(GameTime gameTime, float movement)
		{
			if (crashed)
			{
                //Normally our car would slide and spin forever
                //This code gradually makes us stop after crashing
				float dt = previousTime - crashTime;
				float damping = dt * dt * 4;
				Body.LinearDamping = damping;
				Body.AngularDamping = damping;
			}
            else
			{
				float desiredSpeed = movement * maxLateralSpeed; //Lateral speed based on controller input

				float lateralSpeed = desiredSpeed * 0.5f + Body.LinearVelocity.X * 0.5f; //Smoothly adjust real lateral speed

                float rot = MathHelper.Clamp(-lateralSpeed * 0.05f, -.25f, .25f);

                if (hitWallRight || hitWallLeft)
                {
                    bounceTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (bounceTimer >= 200)
                    {
                        hitWallRight = false;
                        hitWallLeft = false;
                        bounceTimer = 0f;
                    }
                    else
                    {
                        lateralSpeed = bounceLateralSpeed;
                        rot = bounceRotation;
                    }
                }

                Body.LinearVelocity = new Vector2(lateralSpeed, Body.LinearVelocity.Y * 0.98f + initialSpeed * 0.02f); //Smoothly accelerate
                Body.Rotation = rot; //Rotate based on lateral speed

                //Look for car in front of us
				Body b = AnticipateCollision(4.0f);

                //If we are about to hit a car and we weren't already approaching it
                //TODO: I think this logic allows a dodge to be counted multiple times and generally glitch out
				if (ApproachingBody != null && !ApproachingBody.Equals(b))
				{
					Dodges.Enqueue(new Dodge(ApproachingBody, gameTime.TotalGameTime.TotalSeconds));
					ApproachingBody = null;
				}

				if (b != null)
				{
					ApproachingBody = b;
				}

                //Once we pass a car that we were about to hit, we say we have completed a dodge
				if (Dodges.Count > 0)
				{
					Dodge d = Dodges.Peek();
					AABB aabb;
					d.Body.FixtureList[0].GetAABB(out aabb, 0);
					if (d.Body.Position.Y - aabb.Height / 2 < Position.Y)
					{
						DodgeCompleteCallback(d.Body);
						Dodges.Dequeue();
					}
				}
			}

			previousTime = (float)gameTime.TotalGameTime.TotalSeconds;
		}

        //Fired when player runs into something
		bool Collision(Fixture a, Fixture b, Contact c)
		{
            if (b.Body.BodyType == BodyType.Static) //Ran into wall, just ignore
            {
                if (hitWallLeft || hitWallRight) return true;

                if (b.Body.Position.X >= 0) hitWallRight = true;
                else hitWallLeft = true;
                
                bounceLateralSpeed = MathHelper.Clamp(Body.LinearVelocity.X * -2, -15f, 15f);
                bounceRotation = Body.Rotation * -1;

                return true;
            }

            if (b.Body.BodyType == BodyType.Kinematic) //Ran into coin
            {
                CoinGetCallback(b.Body);
                return true;
            }

            //Ran into car
			crashed = true;
			crashTime = previousTime;
			return true;
		}

        public void SaveCurrentPlayerCarData(BinaryWriter file)
        {
            //Write the car position
            file.Write(Position.X);
            file.Write(Position.Y);

			//Write the car velocity
            file.Write(Velocity.X);
            file.Write(Velocity.Y);
        }
    }
}
