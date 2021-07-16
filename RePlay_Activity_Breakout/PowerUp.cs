using Microsoft.Xna.Framework;

namespace RePlay_Activity_Breakout
{
    public enum PowerUpType
    {
        MultiBall = 0,
        PaddleSizeIncrease,
        FireBall
    }

    public class PowerUp : GameObject
    {
        #region Public Properties

        public PowerUpType type;
        public static float speed = 150;
        public bool shouldRemove = false,
                    isActive = false;

        #endregion

        #region Constructor

        public PowerUp(PowerUpType myType, Game myGame) :
            base(myGame)
        {
            type = myType;

            switch (type)
            {
                case (PowerUpType.MultiBall):
                    textureName = "multi_ball_powerUp";
                    break;
                case (PowerUpType.PaddleSizeIncrease):
                    textureName = "wide_paddle_powerUp";
                    break;
                case (PowerUpType.FireBall):
                    textureName = "fireball_powerUp";
                    break;
            }
        }

        #endregion

        #region Update Override

        public override bool Update(float deltaTime, BreakoutGame game = null)
        {
            base.Update(deltaTime);

            if (game != null)
            {
                int ScreenHeight = BreakoutGame.VirtualScreenHeight;

                position.Y += speed * deltaTime;
                if (position.Y > (ScreenHeight + Height / 2))
                {
                    shouldRemove = true;
                }
            }

            return false;
        }

        #endregion

    }
}
