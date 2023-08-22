namespace RePlay.View;

public partial class Page_Settings_EnterPassword : ContentPage
{
	public Page_Settings_EnterPassword()
	{
		InitializeComponent();
	}

    private void NextButton_Clicked(object sender, EventArgs e)
    {
		Navigation.PushAsync(new Page_Settings_Main());
    }
}