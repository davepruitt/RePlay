namespace RePlay.View;

public partial class Page_Settings_EditApplicationSettings : ContentPage
{
    #region Constructor

    public Page_Settings_EditApplicationSettings()
	{
		InitializeComponent();
	}

    #endregion

    #region Button click handlers

    private void DoneButton_Clicked(object sender, EventArgs e)
    {
        //Pop this page
        Navigation.PopAsync();
    }

    #endregion
}