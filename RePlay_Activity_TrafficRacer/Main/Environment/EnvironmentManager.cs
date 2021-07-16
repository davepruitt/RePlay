using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Graphics;
using RePlay_Activity_TrafficRacer.Utility;
using RePlay_Activity_TrafficRacer.Vehicle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;

namespace RePlay_Activity_TrafficRacer.Environment
{
	public class EnvironmentManager
	{
        //Game objects
		public Road road;
		List<RenderedModel> models = new List<RenderedModel>();

        //State
        Dictionary<Body, Coin> coins = new Dictionary<Body, Coin>();
        Queue<Body> coinDeleteQueue = new Queue<Body>();
        float lastBigSignSpawn = 0;
        float lastCoinSpawn = 0;

        World world;
        ContentManager content;

        public EnvironmentManager(ContentManager content, World world)
		{
			this.content = content;
            this.world = world;
			road = new Road(content, world);            
		}

        public Coin GetCoinFromBody (Body b)
        {
            try
            {
                var c = coins.Where(x => x.Key == b).Select(x => x.Value).FirstOrDefault();
                return c;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void DestroyCoin(Body b)
        {
            coinDeleteQueue.Enqueue(b);
        }

		public void Reset()
		{
			road.Reset();
		}

		public void Add(RenderedModel model)
		{
			models.Add(model);
		}

		public void Update(GameTime gameTime, Player player)
		{
			road.Update(gameTime, player.Position.Y);

            //Destroy coins which the player has passed
            foreach (KeyValuePair<Body, Coin> pair in coins)
            {
                if (pair.Value.Position.Y < player.Position.Y - 10)
                {
                    DestroyCoin(pair.Key);
                }
                pair.Value.Update(gameTime);
            }

            //Destroy models which the player has passed
            for (int i = models.Count-1; i >= 0; i--)
            {
                RenderedModel model = models[i];
                if (model.Position.Y + 20 < player.Position.Y)
                {
                    models.RemoveAt(i);
                }
            }

            //We can only delete bodies after a physics timestep, so we
            //place coins to be deleted in a queue and delete them next frame
            while (coinDeleteQueue.Count > 0)
            {
                Body b = coinDeleteQueue.Dequeue();
                world.Remove(b);
                coins.Remove(b);
            }

            //Spawn in a big orange sign
            if (player.Position.Y > lastBigSignSpawn + 200)
            {
                lastBigSignSpawn = player.Position.Y + 30;
                BigSign model = BigSign.Instantiate(content);
                model.Position = new Vector3(0, lastBigSignSpawn, 0);
                models.Add(model);
            }

            //Spawn in a coin
            if (TrafficGame.Difficulty >= 0.2f && player.Position.Y > lastCoinSpawn + 10)
            {
                Random r = new Random();
                lastCoinSpawn = player.Position.Y + r.Next(30, 40);
                Vector2 pos = new Vector2(r.NextDouble().Map(0, 1, -Road.Size/2, Road.Size/2), player.Position.Y + lastCoinSpawn);
                var coin = new Coin(content, world, pos);
                coins.Add(coin.Body, coin);
            }
		}

		public void Render(GameTime gameTime, GraphicsDevice graphics, Effect effect)
		{
			road.Render(gameTime, graphics, effect);
			foreach (RenderedModel model in models)
			{
				model.Render(gameTime, effect);
			}
            foreach (KeyValuePair<Body, Coin> pair in coins)
            {
                pair.Value.Render(gameTime, effect);
            }
        }

        public void SaveCurrentCoinData(BinaryWriter file)
        {
            file.Write(coins.Count);
            foreach (KeyValuePair<Body, Coin> pair in coins)
            {
                file.Write(pair.Value.UniqueID.ToByteArray());

                file.Write(pair.Value.Position.X);
                file.Write(pair.Value.Position.Y);

                file.Write(pair.Value.Size.X);
                file.Write(pair.Value.Size.Y);
            }
        }
        
	}
}
