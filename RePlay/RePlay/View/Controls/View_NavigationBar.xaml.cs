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

    private void Handle_EnterPasswordPage_SuccessfulLogin(object sender, EventArgs e)
    {
        //Upon successful login, push the settings pain page onto the stack
        Navigation.PushAsync(new Page_Settings_Main());
    }

    private void PopAllPages_ExceptCurrentPageAndMainPage (Page current_page)
    {
        bool done = false;
        while (!done)
        {
            //Remove all pages from the page stack except the current page
            //We will be done when the number of pages is <= 2.
            //This is because the following pages will be remaining on the stack:
            //  1. The main page
            //  2. The current page
            var current_navigation_stack = Navigation.NavigationStack;
            if (current_navigation_stack.Count > 2)
            {
                var first_page_to_remove = current_navigation_stack.Where(x => (x != null) && (x != current_page)).FirstOrDefault();
                if (first_page_to_remove != null)
                {
                    //Remove the page from the stack
                    Navigation.RemovePage(first_page_to_remove);
                }
                else
                {
                    done = true;
                }
            }
            else
            {
                done = true;
            }
        }
    }

    #endregion

    #region Button click handlers

    private void NavigationButtonHome_Clicked(object sender, EventArgs e)
    {
        //If the home button is pressed, pop all pages down to the root page
        Navigation.PopToRootAsync();
    }

    private void NavigationButtonGames_Clicked(object sender, EventArgs e)
    {
        var sender_btn = sender as Element;
        if (sender_btn != null)
        {
            var parent_page = GetParentPage(sender_btn);
            if (parent_page != null)
            {
                if (parent_page is Page_MainPage)
                {
                    //If the current page is the "main page"

                    //Then simply push the games page onto the stack
                    Navigation.PushAsync(new Page_GamesPage());
                }
                else if (parent_page is Page_GamesPage)
                {
                    //If the current page is the "games page", then do nothing...
                }
                else
                {
                    //If the current page is any other page...

                    //Pop all pages on the stack except the current page and the main page
                    PopAllPages_ExceptCurrentPageAndMainPage(parent_page);
                    
                    //Now let's insert the games page onto the stack before the current page
                    Navigation.InsertPageBefore(new Page_GamesPage(), parent_page);

                    //Now pop the current page
                    Navigation.PopAsync();
                }
            }
        }
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
                if (parent_page is Page_MainPage)
                {
                    //If the user has pressed the "settings" button while on the main page

                    //Create an instance of the "enter password" page
                    var enter_password_page = new Page_Settings_EnterPassword();

                    //Navigate to the "enter password" page
                    parent_page.Navigation.PushAsync(enter_password_page);
                }
                else if (parent_page is Page_GamesPage)
                {
                    //If the user has pressed the "settings" button while on the games page

                    //Pop all pages on the stack except the current page and the main page
                    PopAllPages_ExceptCurrentPageAndMainPage(parent_page);

                    //Now let's insert the "enter password" page onto the stack before the current page
                    Navigation.InsertPageBefore(new Page_Settings_EnterPassword(), parent_page);

                    //Now pop the current page
                    Navigation.PopAsync();
                }
                else if (parent_page is Page_Settings_EditApplicationSettings)
                {
                    //If the user has pressed the "settings" button from within the "edit application settings" page

                    //Simply pop the current page to return to the "settings main" page
                    Navigation.PopAsync();
                }
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