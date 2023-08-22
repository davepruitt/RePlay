using RePlay.ViewModel;

namespace RePlay.View;

public partial class Page_MainPage : ContentPage
{
    #region Constructor

    public Page_MainPage()
    {
        InitializeComponent();
        BindingContext = new Page_MainPageViewModel();
    }

    #endregion

    #region Button click handlers

    private void PlayButton_Clicked(object sender, EventArgs e)
    {

    }

    #endregion

    #region Private functions for handling image swapping of the play button on press and release

    private void PlayButton_Pressed(object sender, EventArgs e)
    {
        var vm = this.BindingContext as Page_MainPageViewModel;
        if (vm != null)
        {
            vm.PressPlayButton(true);
        }
    }

    private void PlayButton_Released(object sender, EventArgs e)
    {
        var vm = this.BindingContext as Page_MainPageViewModel;
        if (vm != null)
        {
            vm.PressPlayButton(false);
        }
    }

    #endregion
}