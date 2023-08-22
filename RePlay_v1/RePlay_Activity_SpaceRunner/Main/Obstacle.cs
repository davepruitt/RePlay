using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace RePlay_Activity_SpaceRunner.Main
{
    public abstract class Obstacle
    {
        public Guid UniqueID { get; protected set; } = Guid.NewGuid();

        public int PositionX { get; set; }
        public int PositionY { get; set; }

        public enum ObstacleType
        {
            Boulder,
            Spaceship,
            LaserBeam,
            LaserGate
        }

        public abstract void Draw(SpriteBatch batch);

        public abstract void Update(GameTime time, GameState state, bool crashed);

        public abstract bool IsOffScreen();

        public abstract Rectangle GetBodyRectangle();

    }
}