using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Android.Content.Res;
using RePlay.Entity;
using RePlay_Exercises;
using Android.App;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RePlay.Manager
{
    /// <summary>
    /// Singleton class that manages the collection of games that exist in RePlay.
    /// </summary>
    public class GameManager
    {
        #region Private data members

        private static GameManager instance;
        private const string assetName = "games.txt";
        
        private Activity parent_activity = null;

        private const string REPETITIONS_INTERNAL_NAME = "Repetitions";
        private const string REPETITIONS_EXTERNAL_NAME = "Rep it out";
        private const string RETRIEVE_INTERNAL_NAME = "Retrieve";

        private List<RePlayGame> list_of_games = new List<RePlayGame>();

        #endregion

        #region Singleton Methods

        /// <summary>
        /// Private constructor
        /// </summary>
        private GameManager()
        {
            //empty
        }

        /// <summary>
        /// The singleton instance of the game manager
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameManager();
                }

                return instance;
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The list of all games in RePlay
        /// </summary>
        public List<RePlayGame> Games
        {
            get
            {
                return list_of_games;
            }
            private set
            {
                list_of_games = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the parent activity of the game manager object
        /// </summary>
        public void SetParentActivity (Activity pa)
        {
            parent_activity = pa;
        }

        /// <summary>
        /// Returns a RePlayGame object based upon the game's "internal name".
        /// </summary>
        public RePlayGame GetGameByInternalName(string game_internal_name)
        {
            foreach (RePlayGame game in Games)
            {
                if (game.InternalName.Equals(game_internal_name))
                {
                    return game;
                }
            }
            return null;
        }

        /// <summary>
        /// This method parses the "games.json" file to populate the list of games that exist
        /// in RePlay.
        /// </summary>
        public void LoadGames(AssetManager assets)
        {
            //Clear the list of games
            Games.Clear();

            //Read in the "games.json" file to populate the list of games
            using (var reader = new StreamReader(assets.Open("games.json")))
            {
                string file_contents = reader.ReadToEnd();
                try
                {
                    Games = JsonConvert.DeserializeObject<List<RePlayGame>>(file_contents);
                }
                catch (Exception e)
                {
                    //empty
                }
            }
        }

        #endregion

        #region Methods that are game-specific

        /// <summary>
        /// This method tests whether the RePlayGame object is the Repetitions Mode game.
        /// </summary>
        public bool IsRepetitionsMode(RePlayGame game)
        {
            if (game.InternalName == REPETITIONS_INTERNAL_NAME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method tests whether the external game name string is the same as the string
        /// associated with Repetitions Mode
        /// </summary>
        public bool IsRepetitionsModeExternalName(string game_external_name)
        {
            if (game_external_name == REPETITIONS_EXTERNAL_NAME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method tests whether the RePlayGame object is the ReTrieve game.
        /// </summary>
        public bool IsRetrieve(RePlayGame game)
        {
            if (game.InternalName == RETRIEVE_INTERNAL_NAME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method is specific to ReTrieve.
        /// Given a list of set names, this method returns a list of numeric set ids.
        /// </summary>
        public List<int> RetrieveSetsToSetIDs(List<string> sets)
        {
            List<int> setids = new List<int>();

            foreach(string s in sets)
            {
                setids.Add(RetrieveSetToSetID(s));
            }

            return setids;
        }

        /// <summary>
        /// This method is specific to ReTrieve.
        /// Given a list of set ids and a difficulty level, this method will return a list
        /// of image resources that should be used to display information about the sets.
        /// </summary>
        public List<string> RetrieveSetIDsToSetImages(List<int> set_ids, int difficulty_level)
        {
            List<string> result = new List<string>();

            var set_names = RetrieveSetIDstoSet(set_ids);
            set_names = set_names.Select(x => x.ToLower()).ToList();
            foreach (var set_name in set_names)
            {
                //Initialize a variable that we will use to track whether this is an intro set
                bool is_intro_set = false;

                //Establish the base
                string resource_file_name = "retrieve_";

                //Establish the next piece
                if (set_name.Equals("Shape", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "shape";
                }
                else if (set_name.Equals("Weight", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "weight";
                }
                else if (set_name.Equals("Texture", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "texture";
                }
                else if (set_name.Equals("Length", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "length";
                }
                else if (set_name.Equals("Polygon", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "polygons";
                }
                else if (set_name.Equals("Intro: Find", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "intro_find";
                    is_intro_set = true;
                }
                else if (set_name.Equals("Intro: Discriminate", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "intro_discriminate";
                    is_intro_set = true;
                }
                else if (set_name.Equals("Texture 2", StringComparison.OrdinalIgnoreCase))
                {
                    resource_file_name += "texture2";
                }
                
                //If this is not an intro set, continue to form the last part of the resource file name
                if (!is_intro_set)
                {
                    resource_file_name += "_";

                    //Establish the next piece
                    if (difficulty_level == 0)
                    {
                        resource_file_name += "single";
                    }
                    else if (difficulty_level == 1)
                    {
                        resource_file_name += "easy";
                    }
                    else if (difficulty_level == 2)
                    {
                        resource_file_name += "medium";
                    }
                    else
                    {
                        resource_file_name += "hard";
                    }
                }
                
                //Add the resource file name to the result
                result.Add(resource_file_name);
            }

            return result;
        }

        /// <summary>
        /// This method is specific to ReTrieve.
        /// Given a list of set ids, this method will return a list of set names.
        /// </summary>
        public List<string> RetrieveSetIDstoSet(List<int> setIds)
        {
            List<string> sets = new List<string>();

            RePlayGame retrieve_game = Games.Where(x => x.InternalName.Equals(RETRIEVE_INTERNAL_NAME)).FirstOrDefault();
            if (retrieve_game != null)
            {
                var retrieve_sets = retrieve_game.GameSpecificInformation["Sets"] as JArray;
                if (retrieve_sets != null)
                {
                    foreach (var set_id in setIds)
                    {
                        var matching_set = retrieve_sets.Where(x => ((int)x["Id"]) == set_id).FirstOrDefault();
                        if (matching_set != null)
                        {
                            sets.Add((string)matching_set["Name"]);
                        }
                        else
                        {
                            sets.Add(string.Empty);
                        }
                    }
                }
            }

            return sets;
        }

        /// <summary>
        /// This method is specific to ReTrieve.
        /// Given a set name, this method will return the associated set id.
        /// </summary>
        private int RetrieveSetToSetID(string set)
        {
            RePlayGame retrieve_game = Games.Where(x => x.InternalName.Equals(RETRIEVE_INTERNAL_NAME)).FirstOrDefault();
            if (retrieve_game != null)
            {
                var retrieve_sets = retrieve_game.GameSpecificInformation["Sets"] as JArray;
                if (retrieve_sets != null)
                {
                    var matching_set = retrieve_sets.Where(x => ((string)x["Name"]).Equals(set)).FirstOrDefault();
                    if (matching_set != null)
                    {
                        return ((int)matching_set["Id"]);
                    }
                }
            }

            return 0;
        }

        public List<string> GetAllRetrieveSetNames (RePlayGame retrieve_game)
        {
            if (retrieve_game != null && IsRetrieve(retrieve_game))
            {
                var retrieve_sets = retrieve_game.GameSpecificInformation["Sets"] as JArray;
                var retrieve_set_names = retrieve_sets.Select(x => (string)x["Name"]).ToList();
                return retrieve_set_names;
            }
            else
            {
                return new List<string>();
            }
        }

        public List<string> GetRetrieveSetsThatMatchDifficultyLevel (RePlayGame retrieve_game, int difficulty_level)
        {
            List<string> result = new List<string>();

            if (retrieve_game != null && IsRetrieve(retrieve_game))
            {
                var retrieve_sets = retrieve_game.GameSpecificInformation["Sets"] as JArray;
                if (retrieve_sets != null)
                {
                    foreach (var set in retrieve_sets)
                    {
                        var this_set_difficulty_levels = set["Difficulty"].ToObject<int[]>().ToList();
                        if (this_set_difficulty_levels.Contains(difficulty_level))
                        {
                            result.Add((string)set["Name"]);
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
