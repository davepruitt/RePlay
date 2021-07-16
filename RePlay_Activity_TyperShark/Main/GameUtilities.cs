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
using Microsoft.Xna.Framework.Input;

namespace RePlay_Activity_TyperShark.Main
{
    public static class GameUtilities
    {
        private static Dictionary<Keys, char> SupportedKeyConversions = new Dictionary<Keys, char>()
        {
            { Keys.A, 'a' },
            { Keys.B, 'b' },
            { Keys.C, 'c' },
            { Keys.D, 'd' },
            { Keys.E, 'e' },
            { Keys.F, 'f' },
            { Keys.G, 'g' },
            { Keys.H, 'h' },
            { Keys.I, 'i' },
            { Keys.J, 'j' },
            { Keys.K, 'k' },
            { Keys.L, 'l' },
            { Keys.M, 'm' },
            { Keys.N, 'n' },
            { Keys.O, 'o' },
            { Keys.P, 'p' },
            { Keys.Q, 'q' },
            { Keys.R, 'r' },
            { Keys.S, 's' },
            { Keys.T, 't' },
            { Keys.U, 'u' },
            { Keys.V, 'v' },
            { Keys.W, 'w' },
            { Keys.X, 'x' },
            { Keys.Y, 'y' },
            { Keys.Z, 'z' },
            { Keys.D0, '0' },
            { Keys.D1, '1' },
            { Keys.D2, '2' },
            { Keys.D3, '3' },
            { Keys.D4, '4' },
            { Keys.D5, '5' },
            { Keys.D6, '6' },
            { Keys.D7, '7' },
            { Keys.D8, '8' },
            { Keys.D9, '9' },
            { Keys.NumPad0, '0' },
            { Keys.NumPad1, '1' },
            { Keys.NumPad2, '2' },
            { Keys.NumPad3, '3' },
            { Keys.NumPad4, '4' },
            { Keys.NumPad5, '5' },
            { Keys.NumPad6, '6' },
            { Keys.NumPad7, '7' },
            { Keys.NumPad8, '8' },
            { Keys.NumPad9, '9' },
            { Keys.Space, ' ' },
            { Keys.OemComma, ',' },
            { Keys.OemQuotes, '\'' },
            { Keys.OemQuestion, '?' },
            { Keys.OemPeriod, '.' }

        };

        public static bool IsKeyAlphanumeric (Keys key)
        {
            return (IsKeyAlpha(key) || IsKeyNumeric(key));
        }

        public static bool IsKeyAlpha (Keys key)
        {
            return (key >= Keys.A && key <= Keys.Z);
        }

        public static bool IsKeyNumeric (Keys key)
        {
            return (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        public static char ConvertKeyToChar (Keys key)
        {
            if (SupportedKeyConversions.ContainsKey(key))
            {
                return SupportedKeyConversions[key];
            }
            else return Convert.ToChar(0);
        }
    }
}