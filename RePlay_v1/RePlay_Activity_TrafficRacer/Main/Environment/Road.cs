using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Utility;
using tainicom.Aether.Physics2D.Dynamics;

namespace RePlay_Activity_TrafficRacer.Environment
{
    public class Road
    {
        //Parameters
        public const int NumLanes = 4;
        public const float LaneWidth = 5f;
        public const float Size = NumLanes * LaneWidth;
        public const double ScalingConstant = 5.0;
        const int highlightChangeInterval = 10;
        
        //State
        LinkedList<RoadSegment> Segments = new LinkedList<RoadSegment>();
        public int PrevHighlight = 0;
        public int Highlight = new Random().Next(4);
        public double HighlightChangeTime = -10;

        ContentManager content;
        World world;

        public Road(ContentManager content, World world)
        {
            PrevHighlight = Highlight;

            this.content = content;
            this.world = world;
            Reset();
        }

        //Destroy the road and initially generate 10 pieces at the beginning
        public void Reset()
        {
            Segments.Clear();
            for (int i = 0; i < 10; i++)
            {
                var piece = new RoadSegment(content, world, Size * (i - 1), Highlight);
                Segments.AddLast(piece);
            }
        }

        //Get the lane whose center is at a given x-coordinate
        public static int GetLane(float x, float tolerance = 0.1f)
        {
            float f = x.Map(-LaneWidth * NumLanes / 2, LaneWidth * NumLanes / 2, 0, NumLanes);
            int i = (int)f;

            if (Math.Abs(x - GetCenterOfLane(i)) > tolerance)
            {
                return -100; //This value must be < 0 but must not == -1. Ugly hack.
            }

            return i;
        }

        //Used to make a given lane have gold arrows
        public void SetHighlightStatus(int lane)
        {
            foreach (RoadSegment segment in Segments)
            {
                segment.SetHighlightStatus(lane);
            }
        }

        //Returns a world-space X coordinate of a given lane center
        public static float GetCenterOfLane(int lane)
        {
            return LaneWidth * (lane - NumLanes / 2) + LaneWidth / 2;
        }

        //Get which lane is highlighted at the player-end of the road
        //(segments further ahead of the player may have a different highlighted lane)
        public int GetHighlightAtPlayerPos()
        {
            int result = -1;
            try
            {
                result = Segments.First.Next.Value.HighlightedLane;
            }
            catch (Exception)
            {
                //empty
            }

            return result;
        }

        public void Update(GameTime gameTime, float playerY)
        {
            //Destroy road segments which the player has passed,
            //and create a new one far ahead of the player
            if (playerY - Segments.First.Value.Y > Size * 2)
            {
                Segments.First.Value.Destroy();
                Segments.RemoveFirst();
                var piece = new RoadSegment(content, world, Segments.Last.Value.Y + Size, Highlight);
                Segments.AddLast(piece);
            }

            //Change the highlated lane
            double d = gameTime.TotalGameTime.TotalSeconds - HighlightChangeTime;
            if (d > highlightChangeInterval)
            {
                HighlightChangeTime = gameTime.TotalGameTime.TotalSeconds;

                //Decide which lane to highlight next:

                //First, generate a list of numbers from 1 to 4. This list represents a number ID for each lane
                var possible_lane_list = Enumerable.Range(0, 4).ToList();

                //Now remove the number ID for the lane which is currently highlighted. We don't want to continue highlighting the lane which is already highlighted.
                possible_lane_list.Remove(Highlight);
                possible_lane_list.Remove(PrevHighlight);
                
                //Now randomly shuffle the remaining lane IDs in the list
                var shuffled_list = RePlay_Common.ListExtensionMethods.ShuffleList(possible_lane_list);

                //Now pick the first lane ID in the randomly shuffled list, and it is the next lane we will highlight
                PrevHighlight = Highlight;
                Highlight = shuffled_list.First();
            }
        }

        public void Render(GameTime gameTime, GraphicsDevice graphics, Effect effect)
        {
            foreach (RoadSegment p in Segments)
            {
                p.Render(gameTime, graphics, effect);
            }
        }
    }
}

