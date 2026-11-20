
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;

namespace SpendWise.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthStorageService _authStorageService;

    public LoginPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _authStorageService = new AuthStorageService();
    }

    // Back to Welcome Page
    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Show / Hide Password
    private void ShowPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        PasswordEntry.IsPassword =
            !PasswordEntry.IsPassword;

        ShowPasswordButton.Text =
            PasswordEntry.IsPassword
                ? "Show"
                : "Hide";
    }

    // Forgot Password
    private async void ForgotPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        await Navigation.PushAsync(
            new ForgotPasswordPage());
    }

    // Create Account
    private async void CreateAccountButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new RegisterPage());
    }

    // Login
    private async void LoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        string email =
            EmailEntry.Text?.Trim() ?? "";

        string password =
            PasswordEntry.Text ?? "";

        // Validate Email
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(
                "Please enter your email address.");

            return;
        }

        // Validate Password
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(
                "Please enter your password.");

            return;
        }

        try
        {
            LoginButton.IsEnabled = false;
            LoginButton.Text = "Signing in...";

            // Create Login Request
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            // Send Login Request to API
            var result =
                await _apiService.LoginAsync(request);

            // Check Login Result
            if (!result.Success ||
                result.Data == null)
            {
                ShowError(result.Message);
                return;
            }

            // Check User Information
            if (result.Data.User == null)
            {
                ShowError(
                    "The server did not return user information.");

                return;
            }

            // Save Authentication Information
            await _authStorageService.SaveLoginAsync(
                result.Data.Token,
                result.Data.User.Id,
                result.Data.User.FullName,
                result.Data.User.Email);

            // Display Personalised Welcome Modal
            WelcomeMessageLabel.Text =
                $"Welcome back, {result.Data.User.FullName}!";

            WelcomeModalOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"LOGIN ERROR: {ex}");

            ShowError(
                $"Login error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;

            LoginButton.Text = "Login  →";
        }
    }

    // Continue to Dashboard
    private void OnContinueClicked(
        object sender,
        EventArgs e)
    {
        WelcomeModalOverlay.IsVisible = false;

        Application.Current!.MainPage =
            new NavigationPage(
                new DashboardPage());
    }

    // Display Error Message
    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
