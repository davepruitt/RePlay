using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RePlay_Activity_SpaceRunner.Main
{
    public class SpaceManager : NotifyPropertyChangedObject
    {
        #region Private Properties
        
        private Random Randomizer = new Random();
        private List<Obstacle> Obstacles = new List<Obstacle>();
        private Texture2D CoinTexture;
        private List<Coin> Coins = new List<Coin>();

        private float TimeSinceObstacleSpawn = -2000;
        private float TimeSinceCoinSpawn = -5000;
        private int CoinSpawnInterval = 1500;
        private int ObstacleSpawnInterval = 3000;

        #endregion
        
        #region Public Properties

        public int Speed = 10;
        public int FallingSpeed = 5;
        public int ObstaclesSpawned = 0;
        public int ObstaclesPassed = 0;

        #endregion

        #region Constructor

        public SpaceManager()
        {
            // empty
        }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager Content)
        {
            CoinTexture = Content.Load<Texture2D>("coin");
            FlyingObstacle.LoadContent(Content);
            Laser.LoadContent(Content);
        }

        public void Draw(SpriteBatch batch)
        {
            foreach (Coin coin in Coins)
            {
                coin.Draw(batch);
            }

            foreach (Obstacle obs in Obstacles)
            {
                obs.Draw(batch);
            }
        }

        public void Update(GameTime time, Astronaut player, GameState state, BinaryWriter gamedata_file_writer)
        {
            TimeSinceObstacleSpawn += time.ElapsedGameTime.Milliseconds;
            TimeSinceCoinSpawn += time.ElapsedGameTime.Milliseconds;

            if (TimeSinceObstacleSpawn >= ObstacleSpawnInterval && state == GameState.RUNNING)
            {
                CreateNewObstacle();
                TimeSinceObstacleSpawn = 0f;
            }

            if (TimeSinceCoinSpawn >= CoinSpawnInterval && state == GameState.RUNNING)
            {
                if (Randomizer.Next(10) >= 2) CreateNewCoin();
                TimeSinceCoinSpawn = 0f;
            }

            for (int i = Obstacles.Count - 1; i >= 0; i--)
            {
                var curr = Obstacles[i];
                curr.Update(time, state, player.Crashed);

                if (curr.IsOffScreen())
                {
                    ObstaclesPassed++;
                    Obstacles.RemoveAt(i);
                }
            }

            for (int i = Coins.Count - 1; i >= 0; i--)
            {
                Coin curr = Coins[i];
                curr.Update(time, state, player.Crashed);

                if (curr.IsOffScreen() || !curr.IsAlive)
                {
                    Coins.RemoveAt(i);
                }
            }

            if (!player.Crashed)
            {
                CheckForCollisions(player, gamedata_file_writer);
            }
        }

        // Reset spaceroad
        public void Reset()
        {
            Obstacles.Clear();
            Coins.Clear();
            TimeSinceObstacleSpawn = -2000;
            TimeSinceCoinSpawn = -5000;
            CoinSpawnInterval = 4000;
            ObstacleSpawnInterval = 1500;
            Speed = 15;
        }

        // Increase difficulty
        public void IncreaseDifficulty()
        {
            if (Speed < 25) Speed++;
            if (ObstacleSpawnInterval > 1500) ObstacleSpawnInterval -= 100;
        }

        public void SaveSpaceManagerData(BinaryWriter file_stream)
        {
            file_stream.Write(Speed);

            file_stream.Write(Coins.Count);
            foreach (Coin coin in Coins)
            {
                file_stream.Write(coin.UniqueID.ToByteArray());
                file_stream.Write(coin.BodyRectangle.X);
                file_stream.Write(coin.BodyRectangle.Y);
                file_stream.Write(coin.BodyRectangle.Width);
                file_stream.Write(coin.BodyRectangle.Height);
            }

            file_stream.Write(Obstacles.Count);
            foreach (Obstacle obs in Obstacles)
            {
                file_stream.Write(obs.UniqueID.ToByteArray());
                file_stream.Write(obs.GetBodyRectangle().X);
                file_stream.Write(obs.GetBodyRectangle().Y);
                file_stream.Write(obs.GetBodyRectangle().Width);
                file_stream.Write(obs.GetBodyRectangle().Height);
            }
        }

        #endregion

        #region Private Methods

        // Create new obstacles on road
        private void CreateNewObstacle()
        {
            int randomType = Randomizer.Next(0, 4);
            Obstacle.ObstacleType type = (Obstacle.ObstacleType)randomType;

            switch (type)
            {
                case Obstacle.ObstacleType.Boulder:
                case Obstacle.ObstacleType.Spaceship:
                    var idx = (type == Obstacle.ObstacleType.Boulder) ? 0 : 1;
                    bool spawnTop = (Randomizer.Next(0, 2) == 0);

                    int ypos;
                    if (spawnTop)
                    {
                        ypos = FlyingObstacle.Textures[idx].Height / 2;
                    }
                    else
                    {
                        ypos = SpaceRunnerGame.VirtualScreenHeight - (FlyingObstacle.Textures[idx].Height / 2);
                    }

                    Obstacles.Add(new FlyingObstacle(this, type, SpaceRunnerGame.VirtualScreenWidth + 50, ypos, spawnTop));
                    break;
                case Obstacle.ObstacleType.LaserGate:
                case Obstacle.ObstacleType.LaserBeam:
                    int doubleObstacleRandomizer = Randomizer.Next(0, 10);
                    int yVal = Randomizer.Next(
                        (Laser.LaserOff.Height / 2), 
                        ((SpaceRunnerGame.VirtualScreenHeight / 2) - (Laser.LaserOff.Height / 2))
                        );
                    float startTimer = Randomizer.Next(0, 2000);
                    bool doubleObstacle = false;

                    if (doubleObstacle)
                    {
                        Obstacles.Add(new Laser(this, type, SpaceRunnerGame.VirtualScreenWidth + 50, Laser.LaserOff.Height / 2, startTimer));
                        Obstacles.Add(new Laser(this, type, SpaceRunnerGame.VirtualScreenWidth + 50, SpaceRunnerGame.VirtualScreenHeight - Laser.LaserOff.Height / 2, startTimer));
                        ObstaclesSpawned += 2;
                    }
                    else
                    {
                        if (ObstaclesSpawned % 2 == 0) yVal = SpaceRunnerGame.VirtualScreenHeight - yVal;
                        Obstacles.Add(new Laser(this, type, SpaceRunnerGame.VirtualScreenWidth + 50, yVal, startTimer));
                        ObstaclesSpawned++;
                    }

                    break;
            }
        }

        // Spawn 5 coins at once
        private void CreateNewCoin()
        {
            int xPos = Randomizer.Next(SpaceRunnerGame.VirtualScreenWidth + 50, SpaceRunnerGame.VirtualScreenWidth + 150);
            int yPos = Randomizer.Next(CoinTexture.Height / 2, SpaceRunnerGame.VirtualScreenHeight - (CoinTexture.Height / 2));
            
            Coins.Add(new Coin(this, CoinTexture, xPos, yPos));
            Coins.Add(new Coin(this, CoinTexture, xPos + CoinTexture.Width + 5, yPos));
            Coins.Add(new Coin(this, CoinTexture, xPos + 2 * CoinTexture.Width + 5, yPos));
            Coins.Add(new Coin(this, CoinTexture, xPos + 3 * CoinTexture.Width + 5, yPos));
            Coins.Add(new Coin(this, CoinTexture, xPos + 4 * CoinTexture.Width + 5, yPos));
        }

        // Check for astronaut collision
        private void CheckForCollisions(Astronaut player, BinaryWriter gamedata_file_writer)
        {
            foreach (var obstacle in Obstacles)
            {
                if (obstacle.GetBodyRectangle().Intersects(player.BodyRectangle))
                {
                    if(obstacle is Laser)
                    {
                        var laser = obstacle as Laser;
                        if (laser.IsActive)
                        {
                            NotifyPropertyChanged("HitLaser");
                            player.Crashed = true;
                            break;
                        }
                        else
                        {
                            NotifyPropertyChanged("AvoidedLaser");
                            break;
                        }
                    }
                    else
                    {
                        NotifyPropertyChanged("HitObstacle");
                        player.Crashed = true;
                        break;
                    }
                }
            }

            foreach (var coin in Coins)
            {
                if (!coin.IsAlive) continue;

                if (player.BodyRectangle.Intersects(coin.BodyRectangle))
                {
                    coin.IsAlive = false;
                    player.Score += 10;
                    SpaceRunnerSaveGameData.SaveCoinCapture(gamedata_file_writer);
                    break;
                }
            }

            if (player.HitFloor())
            {
                player.Crashed = true;
                NotifyPropertyChanged("HitFloor");
            }
        }

        #endregion

    }
}