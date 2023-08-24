namespace RePlay.View;

public partial class Page_Settings_EnterPassword : ContentPage
{
    #region Constructor

    public Page_Settings_EnterPassword()
	{
		InitializeComponent();
	}

    #endregion

    #region Button click handlers

    private void NextButton_Clicked(object sender, EventArgs e)
    {
        //Insert the main settings page into the navigation stack
        //so that the user is navigated to that page when this
        //page is popped off the stack
        Navigation.InsertPageBefore(new Page_Settings_Main(), this);

        //Pop this page
        Navigation.PopAsync();
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        //Pop this page
        Navigation.PopAsync();
    }

    #endregion
}