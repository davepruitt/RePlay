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
    //Queue which will remove messages after their expiration date
    class MessageQueue : Queue<Message>
    {
        //Parameters
        const float defaultExpiration = 1;

        public void Update(GameTime gameTime)
        {
            foreach (Message m in this)
            {
                if (m.Expiration < 0) //A message will have a -1 expiration by default.
                {
                    m.Expiration = gameTime.TotalGameTime.TotalSeconds + defaultExpiration;
                }
            }

            if (Count > 0 && Peek().Expiration < gameTime.TotalGameTime.TotalSeconds)
            {
                Dequeue();
            }
        }

    }
}