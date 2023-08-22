using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RePlay_VNS_Triggering
{
    public class VNSAlgorithmParameters
    {
        #region Enumerations specific to this class

        public enum SmoothingOptions
        {
            None,
            AveragingFilter
        }

        public enum Stage1_Operations
        {
            None,
            SubtractMean,
            Derivative,
            Gradient
        }

        public enum Stage2_Operations
        {
            RMS,
            SignedRMS,
            Mean,
            Sum
        }

        public enum BufferExpirationOptions
        {
            TimeLimit,
            TimeCapacity,
            NumericCapacity
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public VNSAlgorithmParameters ()
        {
            //empty
        }

        #endregion

        #region Properties

        public bool Enabled { get; set; } = false;

        public TimeSpan Minimum_ISI { get; set; } = TimeSpan.FromSeconds(5.0);

        public TimeSpan Desired_ISI { get; set; } = TimeSpan.FromSeconds(15.0);

        public double Selectivity { get; set; } = 0.9;

        public double CompensatorySelectivity { get; set; } = 0;

        public int TyperSharkLookbackSize { get; set; } = 5;

        public BufferExpirationOptions LookbackWindowExpirationPolicy { get; set; } = BufferExpirationOptions.NumericCapacity;

        public int LookbackWindowCapacity { get; set; } = 300;

        public TimeSpan LookbackWindow { get; set; } = TimeSpan.FromSeconds(5.0);

        public TimeSpan SmoothingWindow { get; set; } = TimeSpan.FromMilliseconds(300);

        public double NoiseFloor { get; set; } = double.NaN;

        public bool TriggerOnPositive { get; set; } = true;

        public bool TriggerOnNegative { get; set; } = true;

        public bool SelectivityControlledByDesiredISI { get; set; } = false;

        [JsonConverter(typeof(StringEnumConverter))]
        public SmoothingOptions Stage1_Smoothing { get; set; } = SmoothingOptions.AveragingFilter;

        [JsonConverter(typeof(StringEnumConverter))]
        public SmoothingOptions Stage2_Smoothing { get; set; } = SmoothingOptions.None;

        [JsonConverter(typeof(StringEnumConverter))]
        public Stage1_Operations Stage1_Operation { get; set; } = Stage1_Operations.Gradient;

        [JsonConverter(typeof(StringEnumConverter))]
        public Stage2_Operations Stage2_Operation { get; set; } = Stage2_Operations.Sum;

        #endregion

        #region Methods

        public int VNS_AlgorithmParameters_SaveVersion { get; private set; } = 3;

        public List<byte> SaveVNSAlgorithmParameters ()
        {
            //Write everything to a temporary MemoryStream object
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryMemoryWriter = new BinaryWriter(memoryStream);

            binaryMemoryWriter.Write(Enabled);
            binaryMemoryWriter.Write(Minimum_ISI.TotalMilliseconds);
            binaryMemoryWriter.Write(Desired_ISI.TotalMilliseconds);
            binaryMemoryWriter.Write(Selectivity);
            binaryMemoryWriter.Write(CompensatorySelectivity);
            binaryMemoryWriter.Write(LookbackWindow.TotalMilliseconds);
            binaryMemoryWriter.Write(SmoothingWindow.TotalMilliseconds);
            binaryMemoryWriter.Write(NoiseFloor);
            binaryMemoryWriter.Write(TriggerOnPositive);
            binaryMemoryWriter.Write(TriggerOnNegative);
            binaryMemoryWriter.Write(SelectivityControlledByDesiredISI);

            string stage1_smoothing = Stage1_Smoothing.ToString();
            string stage2_smoothing = Stage1_Smoothing.ToString();
            string stage1_operation = Stage1_Operation.ToString();
            string stage2_operation = Stage2_Operation.ToString();

            binaryMemoryWriter.Write(stage1_smoothing.Length);
            binaryMemoryWriter.Write(Encoding.ASCII.GetBytes(stage1_smoothing));
            binaryMemoryWriter.Write(stage2_smoothing.Length);
            binaryMemoryWriter.Write(Encoding.ASCII.GetBytes(stage2_smoothing));
            binaryMemoryWriter.Write(stage1_operation.Length);
            binaryMemoryWriter.Write(Encoding.ASCII.GetBytes(stage1_operation));
            binaryMemoryWriter.Write(stage2_operation.Length);
            binaryMemoryWriter.Write(Encoding.ASCII.GetBytes(stage2_operation));

            binaryMemoryWriter.Write(TyperSharkLookbackSize);

            string buffer_expiration_behavior = LookbackWindowExpirationPolicy.ToString();
            binaryMemoryWriter.Write(buffer_expiration_behavior.Length);
            binaryMemoryWriter.Write(Encoding.ASCII.GetBytes(buffer_expiration_behavior));

            binaryMemoryWriter.Write(LookbackWindowCapacity);

            //Now get the contents of the MemoryStream object
            var memory_stream_bytes = new List<byte>(memoryStream.ToArray());
            return memory_stream_bytes;
        }

        public VNSAlgorithmParameters CopyObject ()
        {
            VNSAlgorithmParameters result = this.MemberwiseClone() as VNSAlgorithmParameters;
            return result;
        }

        public void CastToMyType<T>(T hackToInferNeededType, object givenObject) where T : class
        {
            var newObject = givenObject as T;
        }

        public void ApplyJson (JToken json)
        {
            JObject json_object = json as JObject;
            if (json_object != null)
            {
                //Get the dictionary form of the json object
                IDictionary<string, JToken> dictionary = json_object;

                //Iterate over each property of the object
                PropertyInfo[] properties = typeof(VNSAlgorithmParameters).GetProperties();
                foreach (var prop in properties)
                {
                    if (dictionary.ContainsKey(prop.Name))
                    {
                        var result = Convert.ChangeType(dictionary[prop.Name], prop.PropertyType);
                        prop.SetValue(this, result);
                    }
                }
            }
        }

        #endregion
    }
}