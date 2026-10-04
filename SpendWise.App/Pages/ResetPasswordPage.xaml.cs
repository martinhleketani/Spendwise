using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;

namespace SpendWise.App.Pages;

public partial class ResetPasswordPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly string _resetToken;

    private bool _isLoading;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ResetPasswordPage(string resetToken)
    {
        InitializeComponent();

        _apiService = new ApiService();

        _resetToken = resetToken;
    }


    // =====================================================
    // RESET PASSWORD
    // =====================================================

    private async Task ResetPasswordAsync()
    {
        if (_isLoading)
        {
            return;
        }

        HideError();
        HideSuccess();

        string newPassword =
            NewPasswordEntry.Text ?? string.Empty;

        string confirmPassword =
            ConfirmPasswordEntry.Text ?? string.Empty;


        // =================================================
        // VALIDATION
        // =================================================

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ShowError(
                "Please enter your new password.");

            NewPasswordEntry.Focus();

            return;
        }


        if (newPassword.Length < 8)
        {
            ShowError(
                "Password must be at least 8 characters.");

            NewPasswordEntry.Focus();

            return;
        }


        if (!newPassword.Any(char.IsUpper))
        {
            ShowError(
                "Password must contain at least one uppercase letter.");

            NewPasswordEntry.Focus();

            return;
        }


        if (!newPassword.Any(char.IsLower))
        {
            ShowError(
                "Password must contain at least one lowercase letter.");

            NewPasswordEntry.Focus();

            return;
        }


        if (!newPassword.Any(char.IsDigit))
        {
            ShowError(
                "Password must contain at least one number.");

            NewPasswordEntry.Focus();

            return;
        }


        if (!newPassword.Any(
            character =>
                !char.IsLetterOrDigit(character)))
        {
            ShowError(
                "Password must contain at least one special character.");

            NewPasswordEntry.Focus();

            return;
        }


        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowError(
                "Please confirm your new password.");

            ConfirmPasswordEntry.Focus();

            return;
        }


        if (newPassword != confirmPassword)
        {
            ShowError(
                "The passwords do not match.");

            ConfirmPasswordEntry.Focus();

            return;
        }


        if (string.IsNullOrWhiteSpace(_resetToken))
        {
            ShowError(
                "The password reset session is invalid. Please request a new verification code.");

            return;
        }


        try
        {
            SetLoading(true);


            // =============================================
            // CREATE REQUEST
            // =============================================

            ResetPasswordRequest request =
                new ResetPasswordRequest
                {
                    ResetToken = _resetToken,

                    NewPassword = newPassword,

                    ConfirmPassword = confirmPassword
                };


            // =============================================
            // SEND TO API
            // =============================================

            var result =
                await _apiService.ResetPasswordAsync(
                    request);


            // =============================================
            // CHECK RESULT
            // =============================================

            if (!result.Success)
            {
                ShowError(
                    result.Message);

                return;
            }


            // =============================================
            // SUCCESS
            // =============================================

            ShowSuccess(
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Your password has been reset successfully."
                    : result.Message);


            NewPasswordEntry.Text =
                string.Empty;

            ConfirmPasswordEntry.Text =
                string.Empty;


            await Task.Delay(1000);


            // =============================================
            // RETURN TO LOGIN
            // =============================================

            Application.Current!.MainPage =
                new NavigationPage(
                    new LoginPage());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"RESET PASSWORD ERROR: {ex}");

            ShowError(
                $"Unable to reset password: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // SET LOADING
    // =====================================================

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        LoadingIndicator.IsVisible =
            isLoading;

        LoadingIndicator.IsRunning =
            isLoading;

        ResetPasswordButton.IsEnabled =
            !isLoading;

        NewPasswordEntry.IsEnabled =
            !isLoading;

        ConfirmPasswordEntry.IsEnabled =
            !isLoading;

        ShowNewPasswordButton.IsEnabled =
            !isLoading;

        ShowConfirmPasswordButton.IsEnabled =
            !isLoading;

        ResetPasswordButton.Text =
            isLoading
                ? "Resetting Password..."
                : "Reset Password";
    }


    // =====================================================
    // SHOW / HIDE NEW PASSWORD
    // =====================================================

    private void ShowNewPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        NewPasswordEntry.IsPassword =
            !NewPasswordEntry.IsPassword;

        ShowNewPasswordButton.Text =
            NewPasswordEntry.IsPassword
                ? "Show"
                : "Hide";
    }


    // =====================================================
    // SHOW / HIDE CONFIRM PASSWORD
    // =====================================================

    private void ShowConfirmPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword =
            !ConfirmPasswordEntry.IsPassword;

        ShowConfirmPasswordButton.Text =
            ConfirmPasswordEntry.IsPassword
                ? "Show"
                : "Hide";
    }


    // =====================================================
    // ENTER KEY
    // =====================================================

    private async void ConfirmPasswordEntry_Completed(
        object sender,
        EventArgs e)
    {
        await ResetPasswordAsync();
    }


    // =====================================================
    // RESET PASSWORD BUTTON
    // =====================================================

    private async void ResetPasswordButton_Clicked(
        object sender,
        EventArgs e)
    {
        await ResetPasswordAsync();
    }


    // =====================================================
    // SHOW ERROR
    // =====================================================

    private void ShowError(string? message)
    {
        HideSuccess();

        ErrorLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Something went wrong."
                : message;

        ErrorCard.IsVisible =
            true;
    }


    // =====================================================
    // HIDE ERROR
    // =====================================================

    private void HideError()
    {
        ErrorCard.IsVisible =
            false;

        ErrorLabel.Text =
            string.Empty;
    }


    // =====================================================
    // SHOW SUCCESS
    // =====================================================

    private void ShowSuccess(string? message)
    {
        HideError();

        SuccessLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Password reset successfully."
                : message;

        SuccessCard.IsVisible =
            true;
    }


    // =====================================================
    // HIDE SUCCESS
    // =====================================================

    private void HideSuccess()
    {
        SuccessCard.IsVisible =
            false;

        SuccessLabel.Text =
            string.Empty;
    }


    // =====================================================
    // BACK BUTTON
    // =====================================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // =====================================================
    // BACK TO LOGIN
    // =====================================================

    private void BackToLoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        Application.Current!.MainPage =
            new NavigationPage(
                new LoginPage());
    }
}