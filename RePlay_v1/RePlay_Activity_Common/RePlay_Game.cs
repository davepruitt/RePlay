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
using Microsoft.Xna.Framework;

namespace RePlay_Activity_Common
{
    public class RePlay_Game : Game
    {
        #region Define events

        public event EventHandler<RePlayGameSetupCompletedEventArgs> SetupCompleted;
        public event EventHandler<EventArgs> DeviceCommunicationError;
        public event EventHandler<EventArgs> DeviceRebaseline;

        #endregion

        #region Event notification methods

        protected void NotifySetupCompleted (bool successful)
        {
            SetupCompleted?.Invoke(this, new RePlayGameSetupCompletedEventArgs() { Successful = successful });
        }

        protected void NotifyDeviceCommunicationError ()
        {
            DeviceCommunicationError?.Invoke(this, new EventArgs());
        }

        protected void NotifyDeviceRebaseline ()
        {
            DeviceRebaseline?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Public methods

        public virtual bool ConnectToDevice ()
        {
            return true;
        }

        public virtual void ContinueGame ()
        {
            //empty
        }

        public virtual void EndGame ()
        {
            //empty
        }

        #endregion
    }
}