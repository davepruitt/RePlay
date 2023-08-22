using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using ReCheck.Droid.Model;
using RePlay_Common;
using RePlay_VNS_Triggering;

namespace ReCheck.Droid.ViewModel
{
    public class MainPageViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        ReCheckConfigurationModel configuration_model;
        PCM_Manager restore_connection_manager;

        #endregion

        #region Constructor

        public MainPageViewModel(ReCheckConfigurationModel configurationModel, PCM_Manager pcm)
        {
            configuration_model = configurationModel;
            configuration_model.PropertyChanged += ExecuteReactionsToModelPropertyChanged;

            restore_connection_manager = pcm;
            restore_connection_manager.PropertyChanged += HandleUpdatesFromReStoreService;
        }

        #endregion

        #region Private methods

        private void HandleUpdatesFromReStoreService(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("PCM_Connection_Image");
            NotifyPropertyChanged("IPG_ID");
            NotifyPropertyChanged("PCM_ID");
        }

        #endregion

        #region Public properties

        public string ReCheckVersion
        {
            get
            {
                return configuration_model.SoftwareVersion;
            }
        }

        public string BuildDate
        {
            get
            {
                return configuration_model.BuildDate;
            }
        }

        public string TabletID
        {
            get
            {
                return configuration_model.TabletIdentifier;
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ProjectSiteIdentifier" })]
        public string SiteID
        {
            get
            {
                return configuration_model.ProjectSiteIdentifier;
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ProjectIdentifier" })]
        public string ProjectID
        {
            get
            {
                return configuration_model.ProjectIdentifier;
            }
        }

        public string PCM_Connection_Image
        {
            get
            {
                if (restore_connection_manager.IsConnectedToPCM)
                {
                    return "pcm_connected.png";
                }
                else
                {
                    return "pcm_disconnected.png";
                }
            }
        }

        public string IPG_ID
        {
            get
            {
                if (string.IsNullOrEmpty(restore_connection_manager.Current_IPG_Identifier))
                {
                    return "Unknown";
                }
                else
                {
                    return restore_connection_manager.Current_IPG_Identifier;
                }
            }
        }

        public string PCM_ID
        {
            get
            {
                if (string.IsNullOrEmpty(restore_connection_manager.Current_PCM_Identifier))
                {
                    return "Unknown";
                }
                else
                {
                    return restore_connection_manager.Current_PCM_Identifier;
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "AutomaticStimulationEnabled" })]
        public bool IsRecheckPCMConnectionVisible
        {
            get
            {
                return configuration_model.AutomaticStimulationEnabled;
            }
        }

        #endregion
    }
}