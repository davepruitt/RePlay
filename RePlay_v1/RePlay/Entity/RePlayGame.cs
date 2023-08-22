using Android;
using Android.App;
using Java.Lang;
using System;
using System.Collections.Generic;

namespace RePlay.Entity
{
    // holds data pertaining to a specific game
    public class RePlayGame
    {
        #region This code exists ONLY for backwards compatibility with older prescription files.

        [Obsolete("Name is obsolete. Use ExternalName instead.", true)]
        public string Name { get { return ExternalName; } set { ExternalName = value;  } }

        [Obsolete("AssetNamespace is obsolete. Use InternalName instead.", true)]
        public string AssetNamespace {  get { return InternalName; } set { InternalName = value;  } }

        [Obsolete("IsGameAvailable is obsolete. Use IsAvailable instead.", true)]
        public bool IsGameAvailable { get { return IsAvailable; } set { IsAvailable = value;  } }

        public bool ShouldSerializeName() { return false; }
        public bool ShouldSerializeAssetNamespace() { return false; }
        public bool ShouldSerializeIsGameAvailable() { return false; }

        #endregion

        #region Public data members

        public string ExternalName = string.Empty;
        public string InternalName = string.Empty;
        public string ImageAssetName = string.Empty;
        public bool IsAvailable = false;
        public string AssemblyQualifiedName = string.Empty;
        public bool UsesDifficulty = false;
        public bool UsesGain = false;
        public bool IsExternalApplication = false;
        public bool IsMonoGameActivity = false;
        public string GameDescription = string.Empty;
        public List<string> DifficultyLevels = new List<string>();
        public Dictionary<string, object> GameSpecificInformation = new Dictionary<string, object>();
        public List<string> SupportedDevices = new List<string>();
        public List<string> SupportedExercises = new List<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public RePlayGame()
        {
            //empty
        }

        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            if (!(obj is RePlayGame game))
            {
                return false;
            }

            return (ExternalName == game.ExternalName && 
                    InternalName == game.InternalName && 
                    ImageAssetName == game.ImageAssetName && 
                    IsAvailable == game.IsAvailable && 
                    AssemblyQualifiedName == game.AssemblyQualifiedName);
        }

        public override int GetHashCode()
        {
            var hashCode = 404668865;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ExternalName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(InternalName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ImageAssetName);
            hashCode = hashCode * -1521134295 + IsAvailable.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssemblyQualifiedName);
            return hashCode;
        }

        public int GetAssetResource(Activity a)
        {
            int resource = a.Resources.GetIdentifier(ImageAssetName, "drawable", a.PackageName);
            return ((resource == 0) ? Resource.Drawable.gameunknown : resource);
        }

        public string GetGameSpecificExercise ()
        {
            string custom_key = "CustomExerciseDescription";
            if (GameSpecificInformation.ContainsKey(custom_key))
            {
                return ((string)GameSpecificInformation[custom_key]);
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetGameSpecificExerciseImageResourceString ()
        {
            string custom_key = "CustomExerciseImage";
            if (GameSpecificInformation.ContainsKey(custom_key))
            {
                return ((string)GameSpecificInformation[custom_key]);
            }
            else
            {
                return string.Empty;
            }
        }

        public bool HasDefinedGameSpecificExercise ()
        {
            string custom_key = "CustomExerciseDescription";
            return (GameSpecificInformation.ContainsKey(custom_key));
        }

        public bool HasDefinedGameSpecificExerciseImageResourceString ()
        {
            string custom_key = "CustomExerciseImage";
            return (GameSpecificInformation.ContainsKey(custom_key));
        }

        #endregion
    }
}
