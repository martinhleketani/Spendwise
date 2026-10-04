using SpendWise.App.Pages;

namespace SpendWise.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new WelcomePage());
    }
}