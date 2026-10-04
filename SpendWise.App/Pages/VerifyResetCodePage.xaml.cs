using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpendWise.App.Pages;

public partial class VerifyResetCodePage : ContentPage
{
    private readonly ApiService _apiService;

    private readonly string _email;

    private bool _isLoading;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public VerifyResetCodePage(
        string email)
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        _email =
            email?.Trim().ToLowerInvariant()
            ?? string.Empty;

        EmailLabel.Text =
            _email;
    }


    // =====================================================
    // VERIFY CODE
    // =====================================================

    private async Task VerifyCodeAsync()
    {
        if (_isLoading)
        {
            return;
        }


        HideError();
        HideSuccess();


        string code =
            CodeEntry.Text?.Trim()
            ?? string.Empty;


        // =================================================
        // CODE REQUIRED
        // =================================================

        if (string.IsNullOrWhiteSpace(
            code))
        {
            ShowError(
                "Please enter the verification code.");

            CodeEntry.Focus();

            return;
        }


        // =================================================
        // EXACTLY 6 DIGITS
        // =================================================

        if (!Regex.IsMatch(
            code,
            @"^\d{6}$"))
        {
            ShowError(
                "Verification code must contain exactly 6 digits.");

            CodeEntry.Focus();

            return;
        }


        // =================================================
        // EMAIL CHECK
        // =================================================

        if (string.IsNullOrWhiteSpace(
            _email))
        {
            ShowError(
                "The email address is missing. Please return to the previous page and try again.");

            return;
        }


        try
        {
            SetLoading(true);


            // =============================================
            // CREATE REQUEST
            // =============================================

            VerifyResetCodeRequest request =
                new VerifyResetCodeRequest
                {
                    Email =
                        _email,

                    Code =
                        code
                };


            // =============================================
            // VERIFY WITH API
            // =============================================

            var result =
                await _apiService
                    .VerifyResetCodeAsync(
                        request);


            // =============================================
            // CHECK RESULT
            // =============================================

            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                return;
            }


            // =============================================
            // RESET TOKEN
            // =============================================

            string resetToken =
                result.Data.ResetToken;


            if (string.IsNullOrWhiteSpace(
                resetToken))
            {
                ShowError(
                    "The server did not return a password reset token.");

                return;
            }


            // =============================================
            // SUCCESS
            // =============================================

            ShowSuccess(
                string.IsNullOrWhiteSpace(
                    result.Message)
                    ? "Verification code accepted."
                    : result.Message);


            await Task.Delay(
                400);


            // =============================================
            // OPEN NEW PASSWORD PAGE
            // =============================================

            await Navigation.PushAsync(
                new ResetPasswordPage(
                    resetToken));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"VERIFY RESET CODE ERROR: {ex}");


            ShowError(
                $"Unable to verify code: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // RESEND CODE
    // =====================================================

    private async Task ResendCodeAsync()
    {
        if (_isLoading)
        {
            return;
        }


        HideError();
        HideSuccess();


        if (string.IsNullOrWhiteSpace(
            _email))
        {
            ShowError(
                "The email address is missing.");

            return;
        }


        try
        {
            SetLoading(true);


            ForgotPasswordRequest request =
                new ForgotPasswordRequest
                {
                    Email =
                        _email
                };


            var result =
                await _apiService
                    .ForgotPasswordAsync(
                        request);


            if (!result.Success)
            {
                ShowError(
                    result.Message);

                return;
            }


            // Clear any old code.
            CodeEntry.Text =
                string.Empty;


            ShowSuccess(
                string.IsNullOrWhiteSpace(
                    result.Message)
                    ? "A new verification code has been sent."
                    : result.Message);


            CodeEntry.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"RESEND RESET CODE ERROR: {ex}");


            ShowError(
                $"Unable to resend verification code: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // SET LOADING
    // =====================================================

    private void SetLoading(
        bool isLoading)
    {
        _isLoading =
            isLoading;


        LoadingIndicator.IsVisible =
            isLoading;


        LoadingIndicator.IsRunning =
            isLoading;


        VerifyButton.IsEnabled =
            !isLoading;


        ResendCodeButton.IsEnabled =
            !isLoading;


        CodeEntry.IsEnabled =
            !isLoading;


        VerifyButton.Text =
            isLoading
                ? "Please wait..."
                : "Verify Code";
    }


    // =====================================================
    // SHOW ERROR
    // =====================================================

    private void ShowError(
        string? message)
    {
        HideSuccess();


        ErrorLabel.Text =
            string.IsNullOrWhiteSpace(
                message)
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

    private void ShowSuccess(
        string? message)
    {
        HideError();


        SuccessLabel.Text =
            string.IsNullOrWhiteSpace(
                message)
                ? "Verification successful."
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
    // VERIFY BUTTON
    // =====================================================

    private async void VerifyButton_Clicked(
        object sender,
        EventArgs e)
    {
        await VerifyCodeAsync();
    }


    // =====================================================
    // ENTER KEY
    // =====================================================

    private async void CodeEntry_Completed(
        object sender,
        EventArgs e)
    {
        await VerifyCodeAsync();
    }


    // =====================================================
    // RESEND BUTTON
    // =====================================================

    private async void ResendCodeButton_Clicked(
        object sender,
        EventArgs e)
    {
        await ResendCodeAsync();
    }


    // =====================================================
    // CHANGE EMAIL
    // =====================================================

    private async void ChangeEmailButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // =====================================================
    // BACK
    // =====================================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}