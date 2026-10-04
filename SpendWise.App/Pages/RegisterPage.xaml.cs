using SpendWise.App.Models;
using SpendWise.App.Services;

namespace SpendWise.App.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void ShowPasswordButton_Clicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        ShowPasswordButton.Text =
            PasswordEntry.IsPassword ? "Show" : "Hide";
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        string fullName = FullNameEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";
        string confirmPassword = ConfirmPasswordEntry.Text ?? "";

        // ============================
        // FRONTEND VALIDATION
        // ============================

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Please enter your full name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Please enter your email address.");
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError("Please enter a valid email address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter a password.");
            return;
        }

        if (password.Length < 8)
        {
            ShowError(
                "Password must contain at least 8 characters.");
            return;
        }

        if (!password.Any(char.IsUpper))
        {
            ShowError(
                "Password must contain at least one uppercase letter.");
            return;
        }

        if (!password.Any(char.IsLower))
        {
            ShowError(
                "Password must contain at least one lowercase letter.");
            return;
        }

        if (!password.Any(char.IsDigit))
        {
            ShowError(
                "Password must contain at least one number.");
            return;
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            ShowError(
                "Password must contain at least one special character.");
            return;
        }

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowError("Please confirm your password.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Passwords do not match.");
            return;
        }

        // ============================
        // PREPARE REQUEST
        // ============================

        var request = new RegisterRequest
        {
            FullName = fullName,
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        try
        {
            // Prevent multiple clicks while request is running
            SetRegistrationLoading(true);

            // ============================
            // CALL SPENDWISE API
            // ============================

            var result =
                await _apiService.RegisterAsync(request);

            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            // ============================
            // SUCCESS
            // ============================

            await DisplayAlert(
                "Account Created",
                "Your Martin SpendWise account was created successfully. You can now log in.",
                "Continue");

            // Go to Login page
            await Navigation.PushAsync(new LoginPage());
        }
        catch (Exception)
        {
            ShowError(
                "Something went wrong while creating your account.");
        }
        finally
        {
            SetRegistrationLoading(false);
        }
    }

    private void SetRegistrationLoading(bool isLoading)
    {
        // Find the Create Account button from the page
        // and prevent repeated registration requests.
        IsEnabled = !isLoading;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}