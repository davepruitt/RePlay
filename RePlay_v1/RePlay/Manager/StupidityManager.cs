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

namespace RePlay.Manager
{
    /// <summary>
    /// This class is created to bypass some of Android's stupidity because it does not allow
    /// direct passing of object references when starting a new Activity. Android only allows
    /// for serialization of objects using ISerializable or IParcelable. Both of these require
    /// "serializing" your object - basically converting it to a string form and then converting
    /// it back to an object on the other side. This is stupid because it wastes valuable processing
    /// time converting objects to a string form, clearing out memory, instantiating new memory, etc,
    /// while we should just simply be able to pass an object reference and be done with it.
    /// 
    /// So...this is my solution to that problem...
    /// </summary>
    public static class StupidityManager
    {
        private static Dictionary<string, object> private_dictionary = new Dictionary<string, object>();

        /// <summary>
        /// Adds an object reference to the internal dictionary using a key-value pair.
        /// </summary>
        public static void HoldThisForMe (string key, object obj)
        {
            //If "private_dictionary" is null for some reason, just instantiate a new dictionary.
            if (private_dictionary == null)
            {
                private_dictionary = new Dictionary<string, object>();
            }

            //Add this key to the dictionary
            private_dictionary[key] = obj;
        }

        /// <summary>
        /// Returns an object reference from the dictionary, given a key.
        /// </summary>
        public static object GiveMeThat (string key)
        {
            if (private_dictionary != null)
            {
                if (private_dictionary.ContainsKey(key))
                {
                    return private_dictionary[key];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the internal dictionary.
        /// </summary>
        public static void CleanTheSlatePlease ()
        {
            if (private_dictionary != null)
            {
                private_dictionary.Clear();
            }
        }
    }
}