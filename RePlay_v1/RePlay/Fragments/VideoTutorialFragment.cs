using Android.App;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay.Activities;
using RePlay_Exercises;
using System;

namespace RePlay.Fragments
{
#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
    public class VideoTutorialFragment : DialogFragment, MediaPlayer.IOnPreparedListener, ISurfaceHolderCallback
    {
        #region Properties

        private ExerciseType Exercise;
        private Activity Prompt;
        private PromptActivity PromptActivity;
        private MediaPlayer Player;
        private int ResourceId;
        private const int PLAYER_WIDTH = 2400;
        private const int PLAYER_HEIGHT = 1350;

        #endregion

        #region Singleton Methods

        private VideoTutorialFragment(ExerciseType exercise, PromptActivity prompt, int res)
        {
            Exercise = exercise;
            ResourceId = res;
            Prompt = prompt;
            PromptActivity = prompt;
        }

        public static VideoTutorialFragment NewInstance(ExerciseType exercise, PromptActivity prompt, int res)
        {
            VideoTutorialFragment instance = new VideoTutorialFragment(exercise, prompt, res);
            
            return instance;
        }

        #endregion

        #region Create Dialog

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override void OnStart()
        {
            base.OnStart();

            if (Dialog == null) return;

            Dialog.Window.SetLayout(PLAYER_WIDTH, PLAYER_HEIGHT);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            Dialog.SetCanceledOnTouchOutside(false);
            View rootView = inflater.Inflate(Resource.Layout.VideoTutorialFragment, container, false);

            var video = rootView.FindViewById<VideoView>(Resource.Id.video_player);

            ISurfaceHolder holder = video.Holder;
            holder.AddCallback(this);
            
            Player = MediaPlayer.Create(Prompt, ResourceId);
            Player.Completion += Video_Completion;
            Player.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);
        
            var close = rootView.FindViewById<Button>(Resource.Id.close_video);
            close.Click += Close_Click;

            return rootView;
        }
        
        #endregion

        #region Event Handlers

        private void Video_Completion(object sender, EventArgs e)
        {
            Player.Stop();
            PromptActivity.VideoTutorialLaunched = false;
            Dismiss();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Player.Stop();
            PromptActivity.VideoTutorialLaunched = false;
            Dismiss();
        }

        public void OnPrepared(MediaPlayer mp)
        {
            Console.WriteLine("Prepared");
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            Console.WriteLine("SurfaceChanged");
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Console.WriteLine("SurfaceCreated");
            Player.SetDisplay(holder);
            Player.Start();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Console.WriteLine("SurfaceDestroyed");
        }

        public override void OnPause()
        {
            base.OnPause();

            if (Player != null)
            {
                Player.Stop();
            }

            PromptActivity.VideoTutorialLaunched = false;
        }

        public override void OnStop()
        {
            base.OnStop();

            if (Player != null)
            {
                Player.Stop();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (Player != null)
            {
                Player.Stop();
            }
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member
}