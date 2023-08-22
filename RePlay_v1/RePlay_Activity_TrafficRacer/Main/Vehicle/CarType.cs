using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RePlay_Activity_TrafficRacer.Vehicle
{
	public class CarType
	{
		public static readonly CarType BASIC = new CarType("car1", 1.6f, 3f, 1.5f, 0, 0, new Vector3(0, 0.35f, 0));
		public static readonly CarType ROUNDY = new CarType("car2", 1.6f, 3.8f, 1.35f, 0.15f, 0.5f, new Vector3(0, 0.15f, 0));
		public static readonly CarType PICKUP = new CarType("pickup2", 1.65f, 4f, 1.4f, 0, 0, new Vector3(0, -0.8f, 0));
		public static readonly CarType SEMI = new CarType("semi", 1.7f, 5f, 2.5f, 0, 0, new Vector3(0, 1.5f, 0));
		public static readonly CarType SPORT = new CarType("sport", 1.6f, 4.35f, 1.25f, 0, 0, new Vector3(0, -0.5f, 0));

		public string Name;
		public float Width;
		public float Length;
		public float Height;
		public float RadiusTop;
		public float RadiusBottom;
		public Model Model;
		public Texture2D Texture;
		public string ModelName;
		public string TextureName;
        public Vector3 Offset;

		public CarType(string name, float width, float length, float height, float radiusTop, float radiusBottom, Vector3 offset)
		{
			Name = name;
			Width = width;
			Length = length;
			Height = height;
			RadiusTop = radiusTop;
			RadiusBottom = radiusBottom;
            Offset = offset;
		}

		public static CarType RANDOM
		{
			get
			{
				Random random = new Random();
				int i = random.Next(1, CarType.ALL.Length); //Skip the first out of all (i.e. non-sports car)

				return CarType.ALL[i];
			}
		}

		public static CarType[] ALL
		{
			get
			{
				return new CarType[]
				{
					SPORT,
					BASIC,
					ROUNDY,
					PICKUP,
					SEMI,
				};
			}
		}

		public static void LoadContent(ContentManager content)
		{
			foreach (CarType ct in CarType.ALL)
			{

                ct.TextureName = "new_cars/texture/" + ct.Name;
				ct.ModelName = "new_cars/" + ct.Name;

                ct.Texture = content.Load<Texture2D>(ct.TextureName);
				ct.Model = content.Load<Model>(ct.ModelName);
			}
		}
	}
}
