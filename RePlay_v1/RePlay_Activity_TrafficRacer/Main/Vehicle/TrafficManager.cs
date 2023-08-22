using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D.Diagnostics;
using RePlay_Activity_TrafficRacer.Environment;
using RePlay_Activity_TrafficRacer.Utility;
using System.IO;

namespace RePlay_Activity_TrafficRacer.Vehicle
{
    public class TrafficManager
    {
        public List<Car> cars = new List<Car>();
        public int Interval;

        World World;
        long LastSpawnMillis;
        ContentManager Content;

        Random random;

        public TrafficManager(ContentManager content, World world)
        {
            World = world;
            Content = content;

            //TODO: This interval is set once. Should be set in update so that difficulty can be adjusted mid-game
            if (TrafficGame.Difficulty > 0.3f)
            {
                Interval = (int)TrafficGame.Difficulty.Map(0.3f, 1, 10000, 1000);
                LastSpawnMillis = -Interval;
            }
            else
            {
                Interval = int.MaxValue;
                LastSpawnMillis = int.MaxValue;
            }

            random = new Random();
        }

        public void Reset()
        {
            foreach (Car car in cars)
            {
                World.Remove(car.Body);
            }

            cars.Clear();
        }

        void AttemptToSpawnCar(Player player, GameState state)
        {
            CarType type;

            int lane = 0;
            float posX = 0, posY = 0;

            var foundSpawnPos = true;
            var spawnAttempts = 0;
            do
            {
                type = CarType.RANDOM;
                spawnAttempts += 1;

                lane = random.Next(0, Road.NumLanes);
                var lanePos = Road.GetCenterOfLane(lane);

                var wiggleRoom = Math.Max(0, Road.LaneWidth - type.Width - 0.1f); //How much room the car has on either side of it
                var laneOffset = (float)random.NextDouble() * wiggleRoom - wiggleRoom / 2; //Cars aren't always perfectly centered

                posX = lanePos + laneOffset;

                //If the player is going very slow or the game just started, cars should spawn behind the player
                //Otherwise they should spawn some distance ahead
                if (player.Velocity.Y < 10 || state == GameState.STARTING)
                {
                    posY = player.Position.Y - (float)(random.NextDouble()) * 20 - 10;
                }
                else
                {
                    posY = player.Position.Y + (float)(random.NextDouble()) * 20 * player.Velocity.Y.Map(10, 30, 0, 1) + 30;
                }

                foundSpawnPos = true;
                //Check to make sure this spawn position doesn't intersect any other cars
                foreach (Car c in cars)
                {
                    if (c.Lane == lane && Math.Abs(c.Position.Y - posY) < Math.Max(c.BodySize.Y, type.Length) + 1)
                    {
                        foundSpawnPos = false;
                        break;
                    }
                }
            } while (!foundSpawnPos && spawnAttempts < 5);

            //Spawn the car
            if (foundSpawnPos)
            {
                float speed = player.Velocity.Y + (Road.NumLanes - lane + 1) * 1.0f + (float)random.NextDouble().Map(0, 1, -10, 0);
                speed *= (1 - Math.Min(0.5f, TrafficGame.Difficulty));

                Car car = new Car(Content, type, World, speed);
                car.Position = new Vector2(posX, posY);
                car.Lane = lane;

                cars.Add(car);
            }
        }

        public void Update(GameTime gameTime, Player player, GameState state)
        {
            float playerY = player.Position.Y;

            //Spawn a car (maybe)
            if (gameTime.TotalGameTime.TotalMilliseconds - LastSpawnMillis >= Interval)
            {
                LastSpawnMillis = (long)gameTime.TotalGameTime.TotalMilliseconds;
                AttemptToSpawnCar(player, state);
            }

            //Despawn cars we left in the dust
            for (int i = cars.Count - 1; i >= 0; i--)
            {
                if (cars[i].Body.Position.Y < playerY - 30 || cars[i].Body.Position.Y > playerY + 50)
                {
                    World.Remove(cars[i].Body);
                    cars.Remove(cars[i]);
                }
            }

            //Update all cars
            foreach (Car car in cars)
            {
                car.Update(gameTime);
            }
        }

        public void RenderTraffic(GameTime gameTime, Effect effect)
        {
            foreach (Car car in cars)
            {
                car.Render(gameTime, effect);
            }
        }

        public void RenderDebug(DebugView debugView, Matrix view, Matrix projection)
        {
            foreach (Car car in cars)
            {
                foreach (CastedRay ray in car.Rays)
                {
                    debugView.BeginCustomDraw(projection, view);

                    if (ray.Hit)
                    {
                        debugView.DrawPoint(ray.P1, .25f, new Color(0.9f, 0.4f, 0.4f));
                        debugView.DrawSegment(ray.P2, ray.P1, new Color(0.8f, 0.4f, 0.4f));
                    }
                    else
                    {
                        debugView.DrawPoint(ray.P1, .25f, new Color(0.4f, 0.9f, 0.4f));
                        debugView.DrawSegment(ray.P2, ray.P1, new Color(0.8f, 0.8f, 0.8f));
                    }
                    debugView.EndCustomDraw();
                }

                car.Rays.Clear();
            }
        }

        public void SaveCurrentTrafficData(BinaryWriter file)
        {
            //Write the number of traffic cars
            file.Write(cars.Count);

            //Write the properities of each car
            foreach (Car car in cars)
            {
                file.Write(car.UniqueID.ToByteArray());

                file.Write(car.BodySize.X);
                file.Write(car.BodySize.Y);

                file.Write(car.Position.X);
                file.Write(car.Position.Y);

                file.Write(car.Velocity.X);
                file.Write(car.Velocity.Y);
            }
        }

    }
}