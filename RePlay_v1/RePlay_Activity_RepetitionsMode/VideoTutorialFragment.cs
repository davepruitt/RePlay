using Android.App;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RePlay_Exercises;
using System;

namespace RePlay_Activity_RepetitionsMode
{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0672 // Member overrides obsolete member
    public class VideoTutorialFragment : DialogFragment, MediaPlayer.IOnPreparedListener, ISurfaceHolderCallback
    {
        #region Properties
        
        public event EventHandler VideoFinishedEvent;
        
        private Activity Prompt;
        private Activity ParentActivity;
        private MediaPlayer Player;
        private int ResourceId;
        private const int PLAYER_WIDTH = 2400;
        private const int PLAYER_HEIGHT = 1350;

        #endregion

        #region Singleton Methods

        private VideoTutorialFragment(Activity prompt, int res)
        {
            ResourceId = res;
            Prompt = prompt;
            ParentActivity = prompt;
        }

        public static VideoTutorialFragment NewInstance(Activity prompt, int res)
        {
            VideoTutorialFragment instance = new VideoTutorialFragment(prompt, res);

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
            View rootView = inflater.Inflate(Resources.GetIdentifier("video_tutorial_fragment_reps_mode", "layout", Application.Context.PackageName), container, false);
            
            var video = rootView.FindViewById<VideoView>(Resources.GetIdentifier("video_player_reps_mode", "id", Application.Context.PackageName));

            ISurfaceHolder holder = video.Holder;
            holder.AddCallback(this);

            Player = MediaPlayer.Create(Prompt, ResourceId);
            Player.Completion += Video_Completion;
            Player.SetVideoScalingMode(VideoScalingMode.ScaleToFitWithCropping);

            var close = rootView.FindViewById<Button>(Resources.GetIdentifier("close_video_reps_mode", "id", Application.Context.PackageName));
            close.Click += Close_Click;

            return rootView;
        }

        #endregion

        #region Event Handlers

        private void Video_Completion(object sender, EventArgs e)
        {
            Player.Stop();
            VideoFinishedEvent?.Invoke(this, new EventArgs());
            Dismiss();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Player.Stop();
            VideoFinishedEvent?.Invoke(this, new EventArgs());
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

            if (Player != null) Player.Stop();

            VideoFinishedEvent?.Invoke(this, new EventArgs());
        }

        public override void OnStop()
        {
            base.OnStop();

            if (Player != null) Player.Stop();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (Player != null) Player.Stop();
        }

        #endregion
    }
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member
}