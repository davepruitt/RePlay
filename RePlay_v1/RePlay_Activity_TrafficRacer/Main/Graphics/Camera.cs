using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Graphics
{
	public enum CameraMode
	{
		PERSPECTIVE, FLAT, FIXED
	}

	public class Camera
	{
        //Parameters
		const float orthoNearZ = -1000;
		const float orthoFarZ = 1000;
		const float perspNearZ = 0.1f;
		const float perspFarZ = 100;
		const float fov = MathHelper.PiOver2 * 0.8f;

        //State
		public Vector2 Target;
		public Vector2 Offset = Vector2.Zero;
		public Vector2 Revolution;

		public Matrix View;
		public Matrix Projection;

		public int Width;
		public int Height;
		public CameraMode Mode = CameraMode.PERSPECTIVE;
		public float Zoom = 1;
		public float Scale = 40;

        private static Camera instance;
        public static Camera main
        {
            get
            {
                if (instance == null)
                {
                    instance = new Camera(TrafficGame.Graphics.Viewport.Width, TrafficGame.Graphics.Viewport.Height);
                }
                return instance;
            }
        }

		Camera(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public void Update()
		{
			Vector3 position;
			Vector3 target = new Vector3(0 * Zoom + Target.X * (1 - Zoom), Target.Y + 5 * Zoom, 0);
			Vector3 up = Vector3.Up;

			if (Mode == CameraMode.PERSPECTIVE) //Default
			{
				position = target + new Vector3(0, -6 * Zoom, 11 * Zoom); //Camera position is above and behind target
				Projection = Matrix.CreatePerspectiveFieldOfView(fov, (float)Width / (float)Height, perspNearZ, perspFarZ);
			}
			else if (Mode == CameraMode.FLAT) //Debug
			{
				position = target + new Vector3(0, 0, 10); //Camera position is above target
				Projection = Matrix.CreateOrthographic(Width / Scale, Height / Scale, orthoNearZ, orthoFarZ);
			}
			else //I honestly can't remember what this is for
			{
				target = new Vector3(Target, 0);
				position = new Vector3(Offset + Target, 2);
				up = Vector3.UnitZ;
				Projection = Matrix.CreatePerspectiveFieldOfView(fov, (float)Width / (float)Height, perspNearZ, perspFarZ);
			}
			position = Vector3.Transform(position - target, Matrix.CreateFromYawPitchRoll(Revolution.X, Revolution.Y, 0)) + target;

			View = Matrix.CreateLookAt(position, target, up);

			if (Mode == CameraMode.FIXED)
			{
				View *= Matrix.CreateTranslation(Vector3.UnitY * -1);
			}
		}
	}
}
