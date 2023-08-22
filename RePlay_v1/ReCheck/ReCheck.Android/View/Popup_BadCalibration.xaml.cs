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
    public partial class Popup_BadCalibration : Rg.Plugins.Popup.Pages.PopupPage
    {
        public Popup_BadCalibration()
        {
            InitializeComponent();
        }
    }
}