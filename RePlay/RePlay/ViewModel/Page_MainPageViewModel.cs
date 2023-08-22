using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay.ViewModel
{
    public class Page_MainPageViewModel : INotifyPropertyChanged
    {
        #region Private data members'

        private bool _play_button_pressed = false;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged (string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs (propertyName));
        }

        #endregion

        #region Constructor

        public Page_MainPageViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        public string PlayButtonImageSource
        {
            get
            {
                if (_play_button_pressed)
                {
                    return "play_green_inverted.png";
                }
                else
                {
                    return "play_green.png";
                }
            }
        }

        #endregion

        #region Methods

        public void PressPlayButton (bool pressed)
        {
            _play_button_pressed = pressed;
            NotifyPropertyChanged("PlayButtonImageSource");
        }

        #endregion
    }
}
