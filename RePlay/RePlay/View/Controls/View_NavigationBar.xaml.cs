namespace RePlay.View.Controls;

public partial class View_NavigationBar : ContentView
{
    #region Constructor

    public View_NavigationBar()
	{
		InitializeComponent();
	}

    #endregion

    #region Private methods

    private Page GetParentPage(Element btn)
    {
        bool done = false;
        Element parent = btn;
        while (!done)
        {
            parent = parent.Parent;
            if (parent == null || parent is Page)
            {
                done = true;
            }
        }

        return (parent as Page);
    }

    #endregion

    #region Button click handlers

    private void NavigationButtonHome_Clicked(object sender, EventArgs e)
    {

    }

    private void NavigationButtonGames_Clicked(object sender, EventArgs e)
    {

    }

    private void NavigationButtonPcm_Clicked(object sender, EventArgs e)
    {

    }

    private void NavigationButtonSettings_Clicked(object sender, EventArgs e)
    {
        var sender_btn = sender as Element;
        if (sender_btn != null)
        {
            var parent_page = GetParentPage(sender_btn);
            if (parent_page != null)
            {
                parent_page.Navigation.PushAsync(new Page_Settings_EnterPassword());
            }
        }
        
    }

    #endregion

    #region Private functions for handling navigation button resizing on press and release

    /// <summary>
    /// This method handles scaling a button down when it is pressed.
    /// </summary>
    private void NavigationButton_Pressed(object sender, EventArgs e)
    {
        var btn = sender as ImageButton;
        if (btn != null)
        {
            btn.ScaleTo(0.67, 50, Easing.Linear);
        }
    }

    /// <summary>
    /// This method handles scaling a button back to its normal size when it is released
    /// </summary>
    private void NavigationButton_Released(object sender, EventArgs e)
    {
        var btn = sender as ImageButton;
        if (btn != null)
        {
            btn.ScaleTo(1.0, 50, Easing.Linear);
        }
    }

    #endregion
}