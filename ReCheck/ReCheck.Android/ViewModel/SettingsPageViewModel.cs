using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ReCheck.Droid.Model;
using RePlay_Common;

namespace ReCheck.Droid.ViewModel
{
    public class SettingsPageViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ReCheckConfigurationModel configuration_model;

        #endregion

        #region Constructor

        public SettingsPageViewModel (ReCheckConfigurationModel configurationModel)
        {
            configuration_model = configurationModel;
            configuration_model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
        }

        #endregion

        #region Public properties
        
        [ReactToModelPropertyChanged(new string[] { "ProjectIdentifier" })]
        public string ProjectID
        {
            get
            {
                return configuration_model.ProjectIdentifier;
            }
            set
            {
                configuration_model.ProjectIdentifier = value.Trim();
            }
        }
        
        [ReactToModelPropertyChanged(new string[] { "ProjectSiteIdentifier" })]
        public string SiteID
        {
            get
            {
                return configuration_model.ProjectSiteIdentifier;
            }
            set
            {
                configuration_model.ProjectSiteIdentifier = value.Trim();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "RepetitionsRequiredForTaskCompletion" })]
        public string RepetitionsRequiredForTaskCompletion
        {
            get
            {
                return configuration_model.RepetitionsRequiredForTaskCompletion.ToString();
            }
            set
            {
                string input = value.Trim();
                bool conversion_success = Int32.TryParse(input, out int result);
                if (conversion_success)
                {
                    if (result > 0)
                    {
                        configuration_model.RepetitionsRequiredForTaskCompletion = result;
                    }
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "AutomaticStimulationEnabled" })]
        public bool AutomaticStimulationEnabled
        {
            get
            {
                return configuration_model.AutomaticStimulationEnabled;
            }
            set
            {
                configuration_model.AutomaticStimulationEnabled = value;
            }
        }
        
        #endregion
    }
}