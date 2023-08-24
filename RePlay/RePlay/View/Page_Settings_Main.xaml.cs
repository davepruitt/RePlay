namespace RePlay.View;

public partial class Page_Settings_Main : ContentPage
{
	public Page_Settings_Main()
	{
		InitializeComponent();
	}

    private void EditApplicationSettingsButton_Clicked(object sender, EventArgs e)
    {
		Navigation.PushAsync(new Page_Settings_EditApplicationSettings());
    }
}