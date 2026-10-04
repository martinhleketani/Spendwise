




using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpendWise.App.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _apiService;

    private bool _isLoading;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ForgotPasswordPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
    }


    // =====================================================
    // SEND RESET CODE
    // =====================================================

    private async Task SendResetCodeAsync()
    {
        if (_isLoading)
        {
            return;
        }

        HideError();
        HideSuccess();

        string email =
            EmailEntry.Text?.Trim()
            ?? string.Empty;


        // =================================================
        // EMAIL VALIDATION
        // =================================================

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(
                "Please enter your email address.");

            EmailEntry.Focus();

            return;
        }


        if (!IsValidEmail(email))
        {
            ShowError(
                "Please enter a valid email address.");

            EmailEntry.Focus();

            return;
        }


        try
        {
            SetLoading(true);


            // =============================================
            // CREATE REQUEST
            // =============================================

            var request =
                new ForgotPasswordRequest
                {
                    Email =
                        email.ToLowerInvariant()
                };


            // =============================================
            // CALL API
            // =============================================

            var result =
                await _apiService
                    .ForgotPasswordAsync(request);


            // =============================================
            // CHECK RESULT
            // =============================================

            if (!result.Success)
            {
                ShowError(result.Message);

                return;
            }


            // =============================================
            // SUCCESS
            // =============================================

            ShowSuccess(
                string.IsNullOrWhiteSpace(
                    result.Message)
                    ? "If the email belongs to a SpendWise account, a verification code has been sent."
                    : result.Message);


            await Task.Delay(500);


            // =============================================
            // GO TO VERIFY CODE PAGE
            // =============================================

            await Navigation.PushAsync(
                new VerifyResetCodePage(email));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"FORGOT PASSWORD ERROR: {ex}");

            ShowError(
                $"Unable to send verification code: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // VALIDATE EMAIL
    // =====================================================

    private static bool IsValidEmail(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }


    // =====================================================
    // SET LOADING
    // =====================================================

    private void SetLoading(
        bool isLoading)
    {
        _isLoading = isLoading;

        LoadingIndicator.IsVisible =
            isLoading;

        LoadingIndicator.IsRunning =
            isLoading;

        SendCodeButton.IsEnabled =
            !isLoading;

        EmailEntry.IsEnabled =
            !isLoading;

        SendCodeButton.Text =
            isLoading
                ? "Sending Code..."
                : "Send Verification Code";
    }


    // =====================================================
    // SHOW ERROR
    // =====================================================

    private void ShowError(
        string? message)
    {
        HideSuccess();

        ErrorLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Something went wrong."
                : message;

        ErrorCard.IsVisible = true;
    }


    // =====================================================
    // HIDE ERROR
    // =====================================================

    private void HideError()
    {
        ErrorCard.IsVisible = false;

        ErrorLabel.Text =
            string.Empty;
    }


    // =====================================================
    // SHOW SUCCESS
    // =====================================================

    private void ShowSuccess(
        string? message)
    {
        HideError();

        SuccessLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Verification code sent."
                : message;

        SuccessCard.IsVisible = true;
    }


    // =====================================================
    // HIDE SUCCESS
    // =====================================================

    private void HideSuccess()
    {
        SuccessCard.IsVisible = false;

        SuccessLabel.Text =
            string.Empty;
    }


    // =====================================================
    // SEND CODE BUTTON
    // =====================================================

    private async void SendCodeButton_Clicked(
        object sender,
        EventArgs e)
    {
        await SendResetCodeAsync();
    }


    // =====================================================
    // EMAIL ENTER KEY
    // =====================================================

    private async void EmailEntry_Completed(
        object sender,
        EventArgs e)
    {
        await SendResetCodeAsync();
    }


    // =====================================================
    // HEADER BACK BUTTON
    // =====================================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // =====================================================
    // BACK TO LOGIN BUTTON
    // =====================================================

    private async void BackToLoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}