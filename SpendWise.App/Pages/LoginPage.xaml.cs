using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;

namespace SpendWise.App.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthStorageService _authStorageService;


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public LoginPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _authStorageService = new AuthStorageService();
    }


    // ==========================================
    // BACK BUTTON
    // ==========================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // ==========================================
    // SHOW / HIDE PASSWORD
    // ==========================================

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


    // ==========================================
    // FORGOT PASSWORD
    // ==========================================

    private async void ForgotPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        await Navigation.PushAsync(
            new ForgotPasswordPage());
    }


    // ==========================================
    // CREATE ACCOUNT
    // ==========================================

    private async void CreateAccountButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new RegisterPage());
    }


    // ==========================================
    // LOGIN
    // ==========================================

    private async void LoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        string email =
            EmailEntry.Text?.Trim() ?? "";

        string password =
            PasswordEntry.Text ?? "";


        // ======================================
        // VALIDATION
        // ======================================

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(
                "Please enter your email address.");

            return;
        }


        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(
                "Please enter your password.");

            return;
        }


        try
        {
            LoginButton.IsEnabled = false;

            LoginButton.Text =
                "Signing in...";


            // ==================================
            // CREATE LOGIN REQUEST
            // ==================================

            var request =
                new LoginRequest
                {
                    Email = email,
                    Password = password
                };


            // ==================================
            // SEND LOGIN TO API
            // ==================================

            var result =
                await _apiService
                    .LoginAsync(request);


            // ==================================
            // CHECK LOGIN RESULT
            // ==================================

            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                return;
            }


            // ==================================
            // CHECK USER INFORMATION
            // ==================================

            if (result.Data.User == null)
            {
                ShowError(
                    "The server did not return user information.");

                return;
            }


            // ==================================
            // STORE AUTHENTICATION
            // ==================================

            await _authStorageService
                .SaveLoginAsync(
                    result.Data.Token,
                    result.Data.User.Id,
                    result.Data.User.FullName,
                    result.Data.User.Email);


            // ==================================
            // WELCOME MESSAGE
            // ==================================

            await DisplayAlert(
                "Welcome",
                $"Welcome back, {result.Data.User.FullName}!",
                "Continue");


            // ==================================
            // OPEN DASHBOARD
            // ==================================

            Application.Current!.MainPage =
                new NavigationPage(
                    new DashboardPage());
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

            LoginButton.Text =
                "Login";
        }
    }


    // ==========================================
    // DISPLAY ERROR
    // ==========================================

    private void ShowError(
        string message)
    {
        ErrorLabel.Text =
            message;

        ErrorLabel.IsVisible =
            true;
    }
}