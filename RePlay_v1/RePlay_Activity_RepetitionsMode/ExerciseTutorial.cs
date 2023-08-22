using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.UniversalVideoViewLib;
using RePlay_Exercises;

namespace RePlay_Activity_RepetitionsMode
{
    [Activity(Label = "ExerciseTutorial")]
    public class ExerciseTutorial : Activity
    {
        private int resource_id = 0;
        private Button SkipButton;
        private Button WatchTutorialButton;
        private bool VideoTutorialLaunched = false;

        private FrameLayout MainLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            resource_id = Intent.GetIntExtra("resid", 0);
            if (resource_id > 0)
            {
                var res_id = Resources.GetIdentifier("exercise_tutorial", "layout", Application.Context.PackageName);
                View exercise_tutorial_layout = LayoutInflater.Inflate(res_id, null);
                MainLayout = new FrameLayout(this.ApplicationContext);
                MainLayout.AddView(exercise_tutorial_layout);

                SetContentView(MainLayout);

                StartImmersiveMode();

                WatchTutorialButton = FindViewById<Button>(Resources.GetIdentifier("watch_tutorial_button", "id", Application.Context.PackageName));
                WatchTutorialButton.Click += WatchTutorialButton_Click;

                SkipButton = FindViewById<Button>(Resources.GetIdentifier("skip_tutorial", "id", Application.Context.PackageName));
                SkipButton.Click += SkipButton_Click;

                TextView exercise_text = FindViewById<TextView>(Resources.GetIdentifier("exercise_description_name", "id", Application.Context.PackageName));
                exercise_text.Text = Intent.GetStringExtra("exercise");
            }
            else
            {
                Intent newIntent = CopyIntent();
                StartActivity(newIntent);
                Finish();
            }
        }

        private void WatchTutorialButton_Click(object sender, System.EventArgs e)
        {
            VideoTutorialLaunched = true;

            FragmentTransaction fm = FragmentManager.BeginTransaction();
            VideoTutorialFragment vid = VideoTutorialFragment.NewInstance(this, resource_id);
            vid.VideoFinishedEvent += Vid_VideoFinishedEvent;
            vid.Show(fm, "dialog fragment");
        }

        private void Vid_VideoFinishedEvent(object sender, System.EventArgs e)
        {
            VideoTutorialLaunched = false;
        }

        // Skip tutorial
        private void SkipButton_Click(object sender, System.EventArgs e)
        {
            Intent newIntent = CopyIntent();
            StartActivity(newIntent);
            Finish();
        }

        // Restart immersive mode
        protected override void OnResume()
        {
            base.OnResume();

            StartImmersiveMode();
        }

        // Immersive mode for games
        private void StartImmersiveMode()
        {
            View decorView = Window.DecorView;
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            var uiOptions = (int)decorView.SystemUiVisibility;
            var newUiOptions = (int)uiOptions;

            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.Immersive;
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
        }

        // Copy all intent parameters
        private Intent CopyIntent()
        {
            Intent newIntent = new Intent(this, typeof(ExerciseRunning));
            newIntent.PutExtras(Intent);
            return newIntent;
        }
    }
}