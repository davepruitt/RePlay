using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Activity_TrafficRacer.Graphics
{
	public static class Lighting
	{
        //Parameters
		const float nearZ = -10;
		const float farZ = 40;
		const float width = 25;
		const float height = 50;
        const int shadowRes = 2048;

        public static readonly Vector4 ambientColor = new Vector4(0.48f, 0.54f, 0.6f, 1f);
        public static readonly Vector4 diffuseColor = new Vector4(1f, 0.8f, 0.8f, 1f);
        public static readonly Vector3 Direction = new Vector3(-0.57735f, -0.57735f, -0.57735f); //Ensure that this is normalized

        //State
        public static Vector3 Position;
		public static Matrix View;
		public static Matrix Projection;
		public static RenderTarget2D ShadowMap;

		public static void Initialize()
		{
			Projection = Matrix.CreateOrthographic(width, height, nearZ, farZ);
			ShadowMap = new RenderTarget2D(TrafficGame.Graphics, shadowRes, shadowRes, true, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PlatformContents);
		}

		public static void Update()
		{
			View = Matrix.CreateLookAt(Position, Position + Direction, Vector3.Up);
		}
	}
}
