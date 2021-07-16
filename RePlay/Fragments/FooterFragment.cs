using Android.App;
using Android.OS;
using Android.Views;

namespace RePlay.Fragments
{
    // Footer portion of the RePlay app
    public class FooterFragment : Fragment
    {
        #region OnCreate
        // Default OnCreate
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        // Default OnCreateView
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Footer, container, false);

            return view;
        }
        #endregion
    }
}
