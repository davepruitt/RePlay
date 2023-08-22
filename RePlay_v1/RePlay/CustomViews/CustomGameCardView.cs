using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using RePlay.Activities;
using RePlay.Entity;
using Android.Support.V7.Widget;
using RePlay.Fragments;
using Android.OS;
using RePlay.Manager;
using Android.Content.Res;
using Android;
using Android.Support.V4.Content;
using System;

// CustomGameCardView: Create game card views
namespace RePlay.CustomViews
{
    [Activity(Label = "CustomGameCardView")]
    public class CustomGameCardView : BaseAdapter
    {
        #region Events

        public class RequestConfigureGameEventArgs : EventArgs
        {
            public string GameInternalName = string.Empty;

            public RequestConfigureGameEventArgs(string game_internal_name)
            {
                GameInternalName = game_internal_name;
            }
        }

        public event EventHandler RequestConfigureGame;

        #endregion

        #region Private data members

        private Activity caller_activity;
        private List<RePlayGame> replay_games_list;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public CustomGameCardView(Activity a, List<RePlayGame> games)
        {
            caller_activity = a;
            replay_games_list = games;
        }

        #endregion

        #region Overriden Properties

        /// <summary>
        /// Override the Count property to return the number of games available
        /// </summary>
        public override int Count
        {
            get 
            { 
                return replay_games_list.Count; 
            }
        }

        #endregion

        #region Overridden Methods

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return 0;
        }

        #endregion

        #region GetView

        /// <summary>
        /// Returns the view for a specific "card" in the "game card view".
        /// </summary>
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            //Get the view object for this game
            View view = convertView;
            if (view == null)
            {
                view = LayoutInflater.From(caller_activity).Inflate(Resource.Layout.GameCard, null, false);
            }

            //Set the image/icon that is part of the game view
            ImageView GameView = view.FindViewById<ImageView>(Resource.Id.gameslist_image);
            string drawable_name = replay_games_list[position].ImageAssetName;
            int my_img_resource_id;
            try
            {
                my_img_resource_id = (int)typeof(Resource.Drawable).GetField(drawable_name).GetValue(null);
                var my_img_resource = ContextCompat.GetDrawable(caller_activity.ApplicationContext, my_img_resource_id);
                GameView.SetImageDrawable(my_img_resource);
            }
            catch (System.Exception)
            {
                //empty
            }

            //Set the text that shows up as part of the game view
            TextView GameText = view.FindViewById<TextView>(Resource.Id.gameslist_name);
            GameText.Text = replay_games_list[position].ExternalName;
            if (!replay_games_list[position].IsAvailable)
            {
                GameText.Text += " (Coming soon!)";
            }

            //Define the click behavior for this game view button
            CardView card = view.FindViewById<CardView>(Resource.Id.gameslist_card);
            card.Click += (s, e) =>
            {
                if (replay_games_list[position].IsAvailable)
                {
                    //Get the name of the game that is being requested
                    var game_name = replay_games_list[position].InternalName;

                    //Tell the calling activity that the user wants to configure this game for launch
                    RequestConfigureGame?.Invoke(this, new RequestConfigureGameEventArgs(game_name));
                }
                else
                {
                    Toast.MakeText(caller_activity, "This game is coming soon!", ToastLength.Short).Show();
                }
            };
            
            // Return the game view to the caller
            return view;
        }

        private void Dialog_NewImmediateGameplayItemConfirmed(object sender, System.EventArgs e)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
