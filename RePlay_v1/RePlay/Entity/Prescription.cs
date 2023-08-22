using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RePlay.Manager;
using RePlay_VNS_Triggering;
using System;
using System.Collections.Generic;

namespace RePlay.Entity
{
    public class Prescription
    {
        #region Constructors

        public Prescription()
        {
            // empty constructor
        }

        public Prescription(List<PrescriptionItem> exercises, string name)
        {
            Name = name;
            PrescriptionItems = exercises;
            Date = DateTime.Now;
        }

        public Prescription(List<PrescriptionItem> exercises, string name, string date)
        {
            Name = name;
            PrescriptionItems = exercises;
            Date = DateTime.FromBinary(long.Parse(date));
        }

        public Prescription(List<PrescriptionItem> exercises, string name, string date, VNSAlgorithmParameters vns)
        {
            Name = name;
            PrescriptionItems = exercises;
            Date = DateTime.FromBinary(long.Parse(date));
            VNS = vns;
        }

        #endregion

        #region Properties

        public List<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime Date { get; set; } = DateTime.MinValue;

        public VNSAlgorithmParameters VNS { get; set; } = new VNSAlgorithmParameters();

        #endregion
    }
}