using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace RePlay_Activity_Breakout
{
    public class Ball : GameObject
    {
        #region Static Properties

        public const int SLOW = 400, 
            MED = 500,
            FAST = 600;

        static public float DefaultSpeed = 500;
        static public double Multiplier { get; set; } = 1.0;
        static public DateTime LastMultiplierIncrease { get; set; }
        static public DateTime LastBlockHit { get; set; }
        static public bool isPaddleBall = true;

        #endregion

        #region Properties

        private float multiBallTimer = 0f;

        public Vector2 direction = new Vector2(0, -1);

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        public Guid UniqueID { get; protected set; } = Guid.NewGuid();

        public float Speed { get; set; }

        public float Radius { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsPaddleBall
        {
            get { return isPaddleBall; }
            set { isPaddleBall = value; }
        }

        public bool IsFireBall { get; set; } = false;

        public bool IsMultiBall { get; set; } = false;

        public float FireBallTimer { get; set; } = 0f;

        #endregion

        #region Constructor

        public Ball(Game myGame) :
            base(myGame)
        {
            textureName = "ball";
            Speed = DefaultSpeed;
        }

        #endregion

        #region Update Override

        public override bool Update(float deltaTime, BreakoutGame game = null)
        {
            if (!IsMultiBall)
                position += direction * DefaultSpeed * deltaTime;
            else
                position += direction * Speed * deltaTime;  // multiball uses speed because they are temporarily (5s) slower when first spawned

            if (IsFireBall)
            {
                FireBallTimer += deltaTime;

                if (FireBallTimer > 15f)
                {
                    textureName = "ball";
                    LoadContent();
                    FireBallTimer = 0f;
                    IsFireBall = false;
                }
            }
            if (IsMultiBall)
            {
                multiBallTimer += deltaTime;

                if (multiBallTimer > 5f)
                    Speed = DefaultSpeed;
            }

            return false;
        }

        #endregion

    }
}

