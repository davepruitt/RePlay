using ReCheck.Droid.Model;
using ReCheck.Droid.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ReCheck.Droid.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(ReCheckConfigurationModel configurationModel)
        {
            InitializeComponent();
            BindingContext = new SettingsPageViewModel(configurationModel);
        }

        private async void SettingsDoneButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(false);
        }
        
        private void ProjectIDTextBox_Completed(object sender, EventArgs e)
        {

        }

        private void SiteIDTextBox_Completed(object sender, EventArgs e)
        {

        }
    }
}