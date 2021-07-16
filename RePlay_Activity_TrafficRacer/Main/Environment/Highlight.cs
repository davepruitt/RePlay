using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Environment
{
	public class Highlight : RenderedModel
	{
        private Vector3 off_color = Color.LimeGreen.ToVector3();
        private Vector3 on_color = new Vector3(0.8f, 1.0f, 0.8f);
        private Vector3 color_difference;
        private Vector3 current_color;

        private DateTime flash_start = DateTime.Now;
        private TimeSpan flash_duration = TimeSpan.FromSeconds(0.5);
        private bool current_state = false;

		const string modelName = "highlight";
        const string texName = "HighlightTex";

        int lane = -1;

        public int HighlightOn = -100;

		public Highlight(ContentManager content, int lane) : base(content, modelName, texName)
		{
            this.Size = new Vector3(Road.LaneWidth * 0.8f, Road.LaneWidth * 0.8f, 0.05f);
			this.Offset = new Vector3(-Road.LaneWidth/2 + 0.05f, 0, 0.05f);
			this.Rotation = Matrix.CreateFromYawPitchRoll(0, 0.02f, 0);
            this.lane = lane;

            color_difference = on_color - off_color;
            current_color = off_color;
        }

		public void Update(GameTime gameTime)
		{
            //empty
        }

        public new void Render(GameTime gameTime, Effect effect)
        {
            if (HighlightOn == lane)
            {
                /*float timeStep = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds / flash_duration.TotalSeconds);
                var vector_color_change = new Vector3(color_difference.X * timeStep, color_difference.Y * timeStep, color_difference.Z * timeStep);
                if (current_state)
                {
                    vector_color_change = -vector_color_change;
                }

                current_color += vector_color_change;

                if (current_color.X > on_color.X && current_color.Y > on_color.Y && current_color.Z > on_color.Z)
                {
                    current_state = true;
                }
                else if (current_color.X < off_color.X && current_color.Y < off_color.Y && current_color.Z < off_color.Z)
                {
                    current_state = false;
                }

                effect.Parameters["ChromaKeyReplace"].SetValue(current_color);*/
                effect.Parameters["ChromaKeyReplace"].SetValue(on_color);
            }
            else
            {
                effect.Parameters["ChromaKeyReplace"].SetValue(off_color);
            }
            base.Render(gameTime, effect);
        }
	}
}
