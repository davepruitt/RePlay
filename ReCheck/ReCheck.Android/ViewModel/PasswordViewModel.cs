using RePlay_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReCheck.ViewModel
{
    /// <summary>
    /// View-model class for the password control
    /// </summary>
    public class PasswordViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        string _password = "123456";
        int _match = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public PasswordViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        public bool IsErrorMessageVisible
        {
            get
            {
                if (_match == -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        #region Methods

        public bool CheckPassword(string pw)
        {
            bool match = (pw.Equals(_password));

            if (match)
            {
                _match = 1;
            }
            else
            {
                _match = -1;
            }

            NotifyPropertyChanged("IsErrorMessageVisible");

            return match;
        }

        #endregion
    }
}
