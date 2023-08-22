using ReCheck.Droid.Model;
using RePlay_DeviceCommunications;
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
    public partial class ExercisePage_ChooseHand : ContentPage
    {
        #region Public data members

        public bool Cancelled { get; set; } = false;
        public bool LeftHand { get; set; } = false;

        #endregion

        public ExercisePage_ChooseHand(ReCheckConfigurationModel config, ReplayMicrocontroller microcontroller, Participant p)
        {
            InitializeComponent();
        }

        private void LeftHandButton_Clicked(object sender, EventArgs e)
        {
            LeftHand = true;
            Navigation.PopModalAsync(false);
        }

        private void RightHandButton_Clicked(object sender, EventArgs e)
        {
            LeftHand = false;
            Navigation.PopModalAsync(false);
        }

        protected override bool OnBackButtonPressed()
        {
            Cancelled = true;
            return base.OnBackButtonPressed();
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            Cancelled = true;
            Navigation.PopModalAsync(false);
        }
    }
}