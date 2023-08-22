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
using Plugin.CurrentActivity;
using ReCheck.Droid.Model;
using RePlay_Common;

namespace ReCheck.Droid.ViewModel
{
    public class Assessment_ParticipantSelectionPage_ViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        ReCheckConfigurationModel model;

        #endregion

        #region Constructor

        public Assessment_ParticipantSelectionPage_ViewModel(ReCheckConfigurationModel m)
        {
            model = m;
            m.PropertyChanged += ExecuteReactionsToModelPropertyChanged;
        }

        #endregion

        #region Properties

        [ReactToModelPropertyChanged(new string[] { "ParticipantID" })]
        public string ParticipantID
        {
            get
            {
                return model.ParticipantID;
            }
            set
            {
                model.ParticipantID = value;
            }
        }

        #endregion
    }
}