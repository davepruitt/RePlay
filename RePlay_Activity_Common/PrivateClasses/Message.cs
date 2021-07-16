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

namespace RePlay_Activity_Common.PrivateClasses
{
    class Message
    {
        public String Text;
        public double Expiration;
        public Vector2 Offset;

        public Message(String text)
        {
            Text = text;
            Expiration = -1;
        }
    }
}