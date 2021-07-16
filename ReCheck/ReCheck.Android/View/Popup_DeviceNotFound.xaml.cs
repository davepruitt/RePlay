using Rg.Plugins.Popup.Services;
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
    public partial class Popup_DeviceNotFound : Rg.Plugins.Popup.Pages.PopupPage
    {
        public Popup_DeviceNotFound()
        {
            InitializeComponent();
        }

        private void Popup_OK_Button_Clicked(object sender, EventArgs e)
        {
            PopupNavigation.Instance.PopAsync(true);
        }
    }
}