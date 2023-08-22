using ReCheck.ViewModel;
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
    public partial class Assessment_LoginPage : ContentPage
    {
        public bool IsPasswordCorrect = false;

        public Assessment_LoginPage()
        {
            InitializeComponent();
            BindingContext = new PasswordViewModel();
        }

        protected override void OnAppearing()
        {
            IsPasswordCorrect = false;
            PasswordEntryBox.Text = string.Empty;
            PasswordEntryBox.Focus();
        }

        private void PasswordEntryButton_Clicked(object sender, EventArgs e)
        {
            AttemptPasswordCommit();
        }

        private void PasswordEntryBox_Completed(object sender, EventArgs e)
        {
            AttemptPasswordCommit();
        }

        private void AttemptPasswordCommit ()
        {
            PasswordViewModel vm = BindingContext as PasswordViewModel;
            if (vm != null)
            {
                bool success = vm.CheckPassword(PasswordEntryBox.Text);
                if (success)
                {
                    IsPasswordCorrect = true;
                    Navigation.PopModalAsync();
                }
            }
        }

        public void ResetLoginPage ()
        {
            IsPasswordCorrect = false;
            PasswordEntryBox.Text = string.Empty;
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            IsPasswordCorrect = false;
            Navigation.PopModalAsync(false);
        }

        protected override bool OnBackButtonPressed()
        {
            IsPasswordCorrect = false;
            return base.OnBackButtonPressed();
        }
    }
}