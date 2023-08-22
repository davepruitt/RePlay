using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_Breakout
{

    public enum BlockType
    {
        Red = 0,
        RedShattering,
        RedFractured,
        Yellow, //3
        YellowShattering,
        YellowFractured,
        Blue, // 6
        BlueShattering,
        BlueFractured,
        Green, // 9
        GreenShattering,
        GreenFractured,
        Purple, // 12
        PurpleShattering,
        PurpleFractured,
        Orange, // 15
        OrangeShattering,
        OrangeFractured,
        Grey, // 18
        GreyShattering,
        GreyFractured,
        Rainbow // 21
    }

    public class Block
    {
        #region Properties

        public static int BlockWidth { get; set; } = 200;
        public static int BlockHeight { get; set; } = 80;

        public int Durability { get; set; } = 0; // durability determines how many hits to break the block
        public Vector2 position;
        public BlockType type;

        public Texture2D Texture { get; set; }

        #endregion

        #region Constructors

        public Block(BlockType t, Game myGame)
        {
           
            type = t;
            switch (type)
            {
                case (BlockType.Red):
                    Texture = myGame.Content.Load<Texture2D>("RedBlockFX");
                    break;
                case (BlockType.RedShattering):
                    Texture = myGame.Content.Load<Texture2D>("RedBlockShatteringFX");
                    break;
                case (BlockType.RedFractured):
                    Texture = myGame.Content.Load<Texture2D>("RedBlockFracturedFX");
                    break;

                case (BlockType.Yellow):
                    Texture = myGame.Content.Load<Texture2D>("YellowBlockFX");
                    break;
                case (BlockType.YellowShattering):
                    Texture = myGame.Content.Load<Texture2D>("YellowBlockShatteringFX");
                    break;
                case (BlockType.YellowFractured):
                    Texture = myGame.Content.Load<Texture2D>("YellowBlockFracturedFX");
                    break;

                case (BlockType.Blue):
                    Texture = myGame.Content.Load<Texture2D>("BlueBlockFX");
                    break;
                case (BlockType.BlueShattering):
                    Texture = myGame.Content.Load<Texture2D>("BlueBlockShatteringFX");
                    break;
                case (BlockType.BlueFractured):
                    Texture = myGame.Content.Load<Texture2D>("BlueBlockFracturedFX");
                    break;

                case (BlockType.Green):
                    Texture = myGame.Content.Load<Texture2D>("GreenBlockFX");
                    break;
                case (BlockType.GreenShattering):
                    Texture = myGame.Content.Load<Texture2D>("GreenBlockShatteringFX");
                    break;
                case (BlockType.GreenFractured):
                    Texture = myGame.Content.Load<Texture2D>("GreenBlockFracturedFX");
                    break;

                case (BlockType.Purple):
                    Texture = myGame.Content.Load<Texture2D>("PurpleBlockFX");
                    break;
                case (BlockType.PurpleShattering):
                    Texture = myGame.Content.Load<Texture2D>("PurpleBlockShatteringFX");
                    break;
                case (BlockType.PurpleFractured):
                    Texture = myGame.Content.Load<Texture2D>("PurpleBlockFracturedFX");
                    break;

                case (BlockType.Orange):
                    Texture = myGame.Content.Load<Texture2D>("OrangeBlockFX");
                    break;
                 case (BlockType.OrangeShattering):
                    Texture = myGame.Content.Load<Texture2D>("OrangeBlockShatteringFX");
                    break;
                case (BlockType.OrangeFractured):
                    Texture = myGame.Content.Load<Texture2D>("OrangeBlockFracturedFX");
                    break;

                case (BlockType.Grey):
                    Texture = myGame.Content.Load<Texture2D>("GrayBlockFX");
                    break;
                
                case (BlockType.GreyShattering):
                    Texture = myGame.Content.Load<Texture2D>("GrayBlockShatteringFX");
                    break;
                case (BlockType.GreyFractured):
                    Texture = myGame.Content.Load<Texture2D>("GrayBlockFracturedFX");
                    break;
                case (BlockType.Rainbow):
                    Texture = myGame.Content.Load<Texture2D>("RainbowBlockFX");
                    break;
            }
        }
        
        public Block(Texture2D t) { Texture = t; }

        #endregion
    }
}