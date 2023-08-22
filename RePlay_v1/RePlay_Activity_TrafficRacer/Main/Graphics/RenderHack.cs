using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RePlay_Activity_TrafficRacer.Graphics;

namespace RePlay_Activity_TrafficRacer.Main.Graphics
{
    public static class RenderHack
    {
        //This is a really ugly hack that must be called before each and every model draw on Android.
        //For some reason, these parameters seem to reset to default values after each model draw on Android.
        public static void RENDER_FIX(Effect effect)
        {
            effect.Parameters["View"].SetValue(Camera.main.View);
            effect.Parameters["Projection"].SetValue(Camera.main.Projection);

            effect.Parameters["AmbientColor"].SetValue(Lighting.ambientColor);

            effect.Parameters["LightPosition"].SetValue(-Lighting.Direction);
            effect.Parameters["DiffuseColor"].SetValue(Lighting.diffuseColor);

            effect.Parameters["ShadowMap"]?.SetValue(Lighting.ShadowMap);

            effect.Parameters["ChromaKeyReplace"].SetValue(new Vector4(-1, -1, -1, -1));
        }
    }
}