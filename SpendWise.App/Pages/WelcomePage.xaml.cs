
namespace SpendWise.App.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    // Login button
    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    // Get Started button
    private async void CreateAccountButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    // Support button
    private async void SupportButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HelpPage());
    }

    // Features button
    private async void FeaturesButton_Clicked(object sender, EventArgs e)
    {
        await MainScroll.ScrollToAsync(
            FeaturesSection,
            ScrollToPosition.MakeVisible,
            true);
    }
}
