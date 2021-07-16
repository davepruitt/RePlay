using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Common
{
    public static class RandomNumberStatic
    {
        /// <summary>
        /// Creates a static instance of a random number generator that can be used across
        /// multiple threads, classes, and functions, so we aren't always reseeding the 
        /// random number generator.
        /// </summary>
        public static Random RandomNumbers = new Random();
    }
}