using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Plugin.CurrentActivity;
using ReCheck.Droid.Model;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace ReCheck.Droid.View
{
    public partial class App : Application
    {
        #region Private data members

        DeviceManager device_manager = null;
        ReCheckConfigurationModel assessment_configuration_model = new ReCheckConfigurationModel();

        #endregion

        public App()
        {
            InitializeComponent();

            device_manager = new DeviceManager(CrossCurrentActivity.Current.Activity);
            MainPage = new MainPage(device_manager, assessment_configuration_model);
        }

        protected override void OnStart()
        {
            /* The following line has been rendered non-functional and commented out for the github release */
            //AppCenter.Start("", typeof(Analytics), typeof(Crashes));
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
