using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using RePlay_Exercises;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RePlay.Helpers
{
    public class Json_ExerciseDeviceType_EnumStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ExerciseDeviceType exerciseDeviceType = (ExerciseDeviceType)value;
            writer.WriteValue(exerciseDeviceType.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //This is for backwards compatibility with older prescription files
            var enumString = (string)reader.Value;
            if (enumString.Equals("RePlay"))
            {
                enumString = "ReCheck";
            }

            return Enum.Parse(typeof(ExerciseDeviceType), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}