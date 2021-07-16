using Android.App;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RePlay.Helpers;
using RePlay.Manager;
using RePlay_Exercises;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RePlay.Entity
{
    // holds the data associated with a prescribed exercise
    public class PrescriptionItem : ICloneable
    {
        #region Properties and methods that exist for backwards compatibility with older prescription files

        public bool ShouldSerializeGame() { return false; }

        #endregion

        #region Properties

        // data to know about a prescribed exercise
        [JsonConverter(typeof(StringEnumConverter))]
        public ExerciseType Exercise { get; set; } = ExerciseType.Unknown;
        
        public RePlayGame Game
        {
            get
            {
                if (!string.IsNullOrEmpty(GameName))
                {
                    var idx = GameManager.Instance.Games.FindIndex(x => x.InternalName.Equals(GameName));
                    if (idx > -1)
                    {
                        return GameManager.Instance.Games[idx];
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

            //[Obsolete("The setter for the Game property is obsolete. You should set the game of this prescription item using the GameName property.")]
            set
            {
                //This setter should ONLY be called by the JSON deserializer and NEVER anyone else
                if (value != null)
                {
                    GameName = value.InternalName;
                }
            }
        }

        public string GameName { get; set; } = string.Empty;

        [JsonConverter(typeof(Json_ExerciseDeviceType_EnumStringConverter))]
        public ExerciseDeviceType Device { get; set; } = ExerciseDeviceType.Unknown;

        public int Duration { get; set; } = 0;

        public int Difficulty { get; set; } = 0;

        public List<int> RetrieveSetIDs { get; set; } = new List<int>();

        public double Gain { get; set; } = 1.0;

        public JToken VNS { get; set; } = null;

        #endregion

        #region Constructors

        public PrescriptionItem()
        {
            // empty constructor for json deserialization
        }

        // basic constructor
        public PrescriptionItem(string game_name, 
            ExerciseDeviceType device, 
            ExerciseType exercise, 
            int duration, 
            int difficulty, 
            double gain,
            JToken vns)
        {
            Exercise = exercise;
            GameName = game_name;
            Device = device;
            Duration = duration;
            Difficulty = difficulty;
            Gain = gain;
            VNS = vns;
        }

        public PrescriptionItem(string game_name, 
            ExerciseDeviceType device, 
            ExerciseType exercise, 
            int duration, 
            int difficulty, 
            List<int> ids, 
            double gain,
            JToken vns)
        {
            Exercise = exercise;
            GameName = game_name;
            Device = device;
            Duration = duration;
            Difficulty = difficulty;
            RetrieveSetIDs = ids;
            Gain = gain;
            VNS = vns;
        }

        #endregion

        #region Copy Methods

        public PrescriptionItem DeepCopy ()
        {
            PrescriptionItem new_item = new PrescriptionItem();
            new_item.Gain = this.Gain;

            if (this.RetrieveSetIDs != null)
            {
                new_item.RetrieveSetIDs = this.RetrieveSetIDs.ToList();
            }
            else
            {
                new_item.RetrieveSetIDs = new List<int>();
            }
            
            new_item.Difficulty = this.Difficulty;
            new_item.Duration = this.Duration;
            new_item.Device = this.Device;
            new_item.GameName = this.GameName;
            new_item.Exercise = this.Exercise;

            if (this.VNS != null)
            {
                new_item.VNS = this.VNS.DeepClone();
            }
            
            return new_item;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Other methods

        public bool ComparePrescriptionItemEquality (PrescriptionItem prescription_item)
        {
            if (prescription_item != null)
            {
                return (Exercise == prescription_item.Exercise &&
                        Device == prescription_item.Device &&
                        GameName.Equals(prescription_item.GameName));
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = -1748797614;
            hashCode = hashCode * -1521134295 + EqualityComparer<ExerciseType>.Default.GetHashCode(Exercise);
            hashCode = hashCode * -1521134295 + EqualityComparer<RePlayGame>.Default.GetHashCode(Game);
            hashCode = hashCode * -1521134295 + EqualityComparer<ExerciseDeviceType>.Default.GetHashCode(Device);
            hashCode = hashCode * -1521134295 + Duration.GetHashCode();
            return hashCode;
        }

        public int GetExerciseImageResourceID (Activity a)
        {
            int resource = 0;

            if (GameManager.Instance.IsRetrieve(this.Game))
            {
                var image_names = GameManager.Instance.RetrieveSetIDsToSetImages(RetrieveSetIDs, Difficulty);
                var first_image_name = image_names.FirstOrDefault();
                if (image_names.Count == 1 && !string.IsNullOrEmpty(first_image_name))
                {
                    resource = a.Resources.GetIdentifier(first_image_name, "drawable", a.PackageName);
                }
                else
                {
                    if (this.Game.HasDefinedGameSpecificExerciseImageResourceString())
                    {
                        string special_img_resource_string = this.Game.GetGameSpecificExerciseImageResourceString();
                        resource = a.Resources.GetIdentifier(special_img_resource_string, "drawable", a.PackageName);
                    }
                }
            }
            else
            {
                resource = ExerciseManager.Instance.MapNameToPic(Exercise, a);
                if (resource == 0 || Exercise == ExerciseType.Unknown)
                {
                    if (this.Game.HasDefinedGameSpecificExerciseImageResourceString())
                    {
                        string special_img_resource_string = this.Game.GetGameSpecificExerciseImageResourceString();
                        resource = a.Resources.GetIdentifier(special_img_resource_string, "drawable", a.PackageName);
                    }
                }
            }
            
            return resource;
        }

        #endregion
    }
}