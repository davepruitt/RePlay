using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using RePlay_Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RePlay_Activity_FruitNinja.Main
{
    public class ProjectileManager : NotifyPropertyChangedObject
    {
        #region Private Properties

        private FruitNinjaGame FruitNinjaGame;
        private Dictionary<string, Texture2D[]> TexturesDictionary = new Dictionary<string, Texture2D[]>();
        private Texture2D[] BombTextures = new Texture2D[3];
        private List<Fruit> FruitProjectiles = new List<Fruit>();
        private Random Randomizer;

        private int MaxWidth;
        private int MaxHeight;
        private int FruitHit = 0;
        private int FruitCreated = 0;
        private float TimeSinceFruitSpawn = -5000;
        private float TimeSinceBombSpawn = -15000;
        private int FruitSpawnInterval = 2500;
        private int BombSpawnInterval = 6000;
        private float TimeAlive = 0.12f;
        private int fruit_id = 1;

        #endregion

        #region Public Properties

        public int TotalFruitHit { get; private set; } = 0;
        public int TotalBombsHit { get; private set; } = 0;
        public int TotalFruitCreated { get; private set; } = 0;
        public int TotalBombsCreated { get; private set; } = 0;
        public int MaxFruitSpeed { get; private set; }  = 120;
        public int BombsHit { get; private set; } = 0;
        public double FruitAccuracy
        {
            get { return (double)FruitHit / (double)FruitCreated; }
        }

        #endregion

        #region Constructor

        public ProjectileManager(FruitNinjaGame game, int w, int h)
        {
            FruitNinjaGame = game;
            MaxWidth = w;
            MaxHeight = h;
            Randomizer = new Random();
        }

        #endregion

        #region Public Methods

        public void Draw(SpriteBatch batch)
        {
            foreach (Fruit f in FruitProjectiles)
            {
                f.Draw(batch);
            }
        }

        public void Update(GameTime time, Knife blade)
        {
            // some sort of algorithm to create new fruits
            // for now every 3 seconds
            TimeSinceFruitSpawn += time.ElapsedGameTime.Milliseconds;
            TimeSinceBombSpawn += time.ElapsedGameTime.Milliseconds;

            if (TimeSinceFruitSpawn >= FruitSpawnInterval)
            {
                CreateNewFruit();
                TimeSinceFruitSpawn = 0f;
            }
            if (TimeSinceBombSpawn >= BombSpawnInterval)
            {
                CreateNewFruit(true);
                TimeSinceBombSpawn = 0f;
            }

            for (int i = FruitProjectiles.Count - 1; i >= 0; i--)
            {
                Fruit curr = FruitProjectiles[i];
                curr.Update(time);

                if (curr.IsOffScreen())
                {
                    FruitProjectiles.RemoveAt(i);
                    if (curr.IsAlive && curr.Time > 0.1f && !curr.IsObstacle)
                    {
                        //blade.Score -= 50;
                        NotifyPropertyChanged("MissedFruit");
                    }
                }
            }

            if (blade.CheckForCollision)
            {
                CheckForCollisions(blade);
            }

            if (blade.CheckCombo)
            {
                HandleCombo(blade);
            }
        }

        // Load all sprites
        public void LoadContent()
        {
            var apple = FruitNinjaGame.Content.Load<Texture2D>("apple");
            var apple1 = FruitNinjaGame.Content.Load<Texture2D>("apple-1");
            var apple2 = FruitNinjaGame.Content.Load<Texture2D>("apple-2");
            var apple_splat = FruitNinjaGame.Content.Load<Texture2D>("apple-splat");
            Texture2D[] apples = { apple, apple1, apple2, apple_splat };
            TexturesDictionary.Add("apple", apples);

            var banana = FruitNinjaGame.Content.Load<Texture2D>("banana");
            var banana1 = FruitNinjaGame.Content.Load<Texture2D>("banana-1");
            var banana2 = FruitNinjaGame.Content.Load<Texture2D>("banana-2");
            var banana_splat = FruitNinjaGame.Content.Load<Texture2D>("banana-splat");
            Texture2D[] bananas = { banana, banana1, banana2, banana_splat };
            TexturesDictionary.Add("banana", bananas);

            var peach = FruitNinjaGame.Content.Load<Texture2D>("peach");
            var peach1 = FruitNinjaGame.Content.Load<Texture2D>("peach-1");
            var peach2 = FruitNinjaGame.Content.Load<Texture2D>("peach-2");
            var peach_splat = FruitNinjaGame.Content.Load<Texture2D>("peach-splat");
            Texture2D[] peaches = { peach, peach1, peach2, peach_splat };
            TexturesDictionary.Add("peach", peaches);

            var strawberry = FruitNinjaGame.Content.Load<Texture2D>("strawberry");
            var strawberry1 = FruitNinjaGame.Content.Load<Texture2D>("strawberry-1");
            var strawberry2 = FruitNinjaGame.Content.Load<Texture2D>("strawberry-2");
            var strawberry_splat = FruitNinjaGame.Content.Load<Texture2D>("strawberry-splat");
            Texture2D[] strawberries = { strawberry, strawberry1, strawberry2, strawberry_splat };
            TexturesDictionary.Add("strawberry", strawberries);

            var watermelon = FruitNinjaGame.Content.Load<Texture2D>("watermelon");
            var watermelon1 = FruitNinjaGame.Content.Load<Texture2D>("watermelon-1");
            var watermelon2 = FruitNinjaGame.Content.Load<Texture2D>("watermelon-2");
            var watermelon_splat = FruitNinjaGame.Content.Load<Texture2D>("watermelon-splat");
            Texture2D[] watermelons = { watermelon, watermelon1, watermelon2, watermelon_splat };
            TexturesDictionary.Add("watermelon", watermelons);

            var bomb = FruitNinjaGame.Content.Load<Texture2D>("bomb");
            var bomb1 = FruitNinjaGame.Content.Load<Texture2D>("bomb-1");
            var bomb2 = FruitNinjaGame.Content.Load<Texture2D>("bomb-2");
            var bomb3 = FruitNinjaGame.Content.Load<Texture2D>("bomb-3");
            Texture2D[] bomb_anim = { bomb, bomb1, bomb2, bomb3 };
            TexturesDictionary.Add("bomb", bomb_anim);
        }

        // Step up in difficulty by 10%
        public void IncreaseDifficulty()
        {
            if (FruitSpawnInterval > 1000) FruitSpawnInterval -= 400;
            if (BombSpawnInterval > 3000) BombSpawnInterval -= 500;
            if (TimeAlive < 0.16) TimeAlive += 0.04f;
            if (MaxFruitSpeed < 160) MaxFruitSpeed += 6;
            NotifyPropertyChanged("DifficultyI");
            ResetAccuracy();
        }

        // Step down in difficulty by 10%
        public void DecreaseDifficulty()
        {
            if (FruitSpawnInterval < 5000) FruitSpawnInterval += 400;
            if (BombSpawnInterval < 8000) BombSpawnInterval += 500;
            if (TimeAlive > 0.12) TimeAlive -= 0.04f;
            if (MaxFruitSpeed > 120) MaxFruitSpeed -= 6;
            NotifyPropertyChanged("DifficultyD");
            ResetAccuracy();
        }

        public void SaveCurrentFruitData(BinaryWriter fid, bool manager_data)
        {
            // Projectile manager data
            if (manager_data)
            {
                fid.Write(true);
                fid.Write(FruitHit);
                fid.Write(FruitCreated);
                fid.Write(BombsHit);
                fid.Write(MaxFruitSpeed);
                fid.Write(FruitSpawnInterval);
                fid.Write(BombSpawnInterval);
            }
            else
            {
                fid.Write(false);
            }

            // Number of fruits
            fid.Write(FruitProjectiles.Count);
            foreach (Fruit f in FruitProjectiles)
            {
                f.SaveFruitData(fid);
            }
        }

        #endregion

        #region Private Methods

        // Create new fruit/bomb
        private void CreateNewFruit(bool IsObstacle = false)
        {
            int angle = Randomizer.Next(60, 90);
            int speed = Randomizer.Next(MaxFruitSpeed - 10, MaxFruitSpeed);
            bool rightToLeft = (Randomizer.Next(0, 2) % 2 == 0);
            int startingXPos = Randomizer.Next(MaxWidth / 8, MaxWidth / 2);

            float gravity = Randomizer.Next(18, 24);
            float rotationStartingAngle = Randomizer.Next(0,360);
            float rotationIncrement = Randomizer.Next(5, 25) / 10.0f;

            if (Randomizer.Next(0, 2) % 2 == 0) rotationIncrement *= -1;

            Fruit newFruit;
            if (IsObstacle) newFruit = new Fruit(TexturesDictionary["bomb"], angle, speed, gravity, rightToLeft, rotationIncrement, rotationStartingAngle, startingXPos, FruitNinjaGame, true, fruit_id);
            else newFruit = new Fruit(GetRandomFruit(), angle, speed, gravity, rightToLeft, rotationIncrement, rotationStartingAngle, startingXPos, TimeAlive, FruitNinjaGame, fruit_id);

            FruitProjectiles.Add(newFruit);
            fruit_id++;
            if (!IsObstacle)
            {
                FruitCreated++;
                TotalFruitCreated++;
            }
            else TotalBombsCreated++;
        }
        
        // Return a random fruit
        private Texture2D[] GetRandomFruit()
        {
            int idx =   Randomizer.Next(0, TexturesDictionary.Count-1);
            var keys = new List<string>(TexturesDictionary.Keys);
            var str = keys[idx];

            return TexturesDictionary[str];
        }

        // Reset our accuracy counters
        private void ResetAccuracy()
        {
            FruitHit = 0;
            FruitCreated = 0;
            BombsHit = 0;
        }

        // Logic for checking for object collisions
        private void CheckForCollisions(Knife blade)
        {
            // Loop through all the fruits we have
            foreach(var fruit in FruitProjectiles)
            {
                if (!fruit.IsAlive)
                {
                    continue;
                }

                // Get fruit hit space
                CircleF hitSpace = fruit.HitArea;

                if (blade.TapInsteadOfSwipe)
                {
                    if (hitSpace.Contains(new Point((int)blade.LastTapLocation.X, (int)blade.LastTapLocation.Y)))
                    {
                        HandleCollision(fruit, blade);
                        break;
                    }
                }

                // Not enough swiping
                if (blade.Path.Count == 0 || blade.EuclideanDistance(blade.Path.First(), blade.Path.Last()) < 20)
                {
                    return;
                }

                // Check blade collisions
                foreach (var vector in blade.Path)
                {
                    if (hitSpace.Contains(new Point2(vector.X, vector.Y)))
                    {
                        HandleCollision(fruit, blade);
                        break;
                    }
                }
            }
        }

        // Handle a collision
        private void HandleCollision(Fruit fruit, Knife blade)
        {
            if (!fruit.IsObstacle)
            {
                fruit.Slice();
                NotifyPropertyChanged("Score");

                // Knife behavior
                blade.Score += 25;
                blade.Hits++;

                // Counters
                FruitHit++;
                TotalFruitHit++;
            }
            else
            {
                // We only explode the bomb if the player is still cutting
                if (blade.IsCutting)
                {
                    // We've hit an obstacle
                    fruit.Explode();
                    NotifyPropertyChanged("Bomb");
                    blade.Score -= 10;
                    if (blade.Score < 0)
                    {
                        blade.Score = 0;
                    }

                    // Counters
                    BombsHit++;
                    TotalBombsHit++;
                }
            }
        }

        // Handle a combo move
        private void HandleCombo(Knife blade)
        {
            if (blade.Hits > 1)
            {
                if (blade.Hits == 2)
                {
                    NotifyPropertyChanged("Combo2");
                    blade.Score += 100;
                }
                else if (blade.Hits == 3)
                {
                    NotifyPropertyChanged("Combo3");
                    blade.Score += 150;
                }
                else if (blade.Hits == 4)
                {
                    NotifyPropertyChanged("Combo4");
                    blade.Score += 200;
                }
            }

            blade.CheckCombo = false;
        }

        #endregion

    }
}