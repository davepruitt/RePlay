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
    public partial class Assessment_ParticipantSelectionPage : ContentPage
    {
        #region Private data members

        ReCheckConfigurationModel assessmentConfigModel;

        #endregion

        public Assessment_ParticipantSelectionPage(ReCheckConfigurationModel acm)
        {
            InitializeComponent();

            assessmentConfigModel = acm;
            BindingContext = new Assessment_ParticipantSelectionPage_ViewModel(assessmentConfigModel);
        }

        private void ParticipantID_ContinueButton_Clicked(object sender, EventArgs e)
        {
            if (Participant.IsOK_ParticipantID(assessmentConfigModel.ParticipantID))
            {
                Navigation.PopModalAsync(false);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            assessmentConfigModel.ParticipantID = string.Empty;
            return base.OnBackButtonPressed();
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            //If the user presses "go back", then treat it as if no participant was selected
            assessmentConfigModel.ParticipantID = string.Empty;
            await Navigation.PopModalAsync();
        }

        private void ParticipantIDTextBox_Completed(object sender, EventArgs e)
        {
            //empty
        }
    }
}