
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpendWise.App.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthStorageService _authStorageService;

    private bool _isLoading;
    private ProfileResponse? _currentProfile;
    private string? _profilePhotoPath;

    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ProfilePage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _authStorageService = new AuthStorageService();
    }

    // =====================================================
    // PAGE APPEARING
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadProfileAsync();
        LoadProfilePhoto();
    }

    // =====================================================
    // LOAD PROFILE
    // =====================================================

    private async Task LoadProfileAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);

            HideError();
            HideSuccess();

            var result = await _apiService.GetProfileAsync();

            if (!result.Success || result.Data == null)
            {
                ClearProfile();
                ShowError(result.Message);
                return;
            }

            _currentProfile = result.Data;

            DisplayProfile(result.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOAD PROFILE ERROR: {ex}");

            ClearProfile();

            ShowError(
                $"Unable to load profile: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    // =====================================================
    // DISPLAY PROFILE
    // =====================================================

    private void DisplayProfile(ProfileResponse profile)
    {
        FullNameEntry.Text = profile.FullName ?? string.Empty;
        EmailEntry.Text = profile.Email ?? string.Empty;
        PhoneNumberEntry.Text = profile.PhoneNumber ?? string.Empty;

        ProfileNameLabel.Text =
            string.IsNullOrWhiteSpace(profile.FullName)
                ? "SpendWise User"
                : profile.FullName;

        ProfileEmailLabel.Text =
            string.IsNullOrWhiteSpace(profile.Email)
                ? "No email address"
                : profile.Email;

        ProfileInitialLabel.Text =
            GetInitial(profile.FullName);

        AccountIdLabel.Text = profile.Id.ToString();

        MemberSinceLabel.Text =
            profile.CreatedAt
                .ToLocalTime()
                .ToString("dd MMM yyyy");

        string status = profile.IsActive ? "Active" : "Inactive";

        AccountStatusLabel.Text = $"● {status}";
        StatusValueLabel.Text = status;

        if (profile.IsActive)
        {
            AccountStatusLabel.TextColor =
                Color.FromArgb("#D9F2E3");

            StatusValueLabel.TextColor =
                Color.FromArgb("#146B3A");
        }
        else
        {
            AccountStatusLabel.TextColor =
                Color.FromArgb("#FFD6D6");

            StatusValueLabel.TextColor =
                Color.FromArgb("#B42318");
        }
    }

    // =====================================================
    // SAVE PROFILE
    // =====================================================

    private async Task SaveProfileAsync()
    {
        if (_isLoading)
            return;

        HideError();
        HideSuccess();

        string fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string phoneNumber = PhoneNumberEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Please enter your full name.");
            FullNameEntry.Focus();
            return;
        }

        if (fullName.Length < 2)
        {
            ShowError("Full name must contain at least 2 characters.");
            FullNameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Please enter your email address.");
            EmailEntry.Focus();
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Please enter a valid email address.");
            EmailEntry.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) &&
            !IsValidSouthAfricanPhoneNumber(phoneNumber))
        {
            ShowError(
                "Phone number must contain 10 digits and start with 0.");

            PhoneNumberEntry.Focus();
            return;
        }

        try
        {
            SetLoading(true);

            UpdateProfileRequest request = new()
            {
                FullName = fullName,
                Email = email.ToLowerInvariant(),
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
                    ? null
                    : phoneNumber
            };

            var result = await _apiService.UpdateProfileAsync(request);

            if (!result.Success || result.Data == null)
            {
                ShowError(result.Message);
                return;
            }

            _currentProfile = result.Data.User;

            DisplayProfile(result.Data.User);

            ShowSuccess(
                string.IsNullOrWhiteSpace(result.Message)
                    ? "Profile updated successfully."
                    : result.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UPDATE PROFILE ERROR: {ex}");

            ShowError(
                $"Unable to update profile: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    // =====================================================
    // CHANGE PROFILE PHOTO
    // =====================================================

    private async void ChangePhotoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        try
        {
            HideError();
            HideSuccess();

            FileResult? photo =
                await MediaPicker.Default.PickPhotoAsync();

            if (photo == null)
                return;

            string extension =
                Path.GetExtension(photo.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ShowError("Please select a JPG, JPEG or PNG image.");
                return;
            }

            string profileDirectory =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Profile");

            Directory.CreateDirectory(profileDirectory);

            string localFilePath =
                Path.Combine(
                    profileDirectory,
                    "profile_photo" + extension);

            DeleteExistingProfilePhotoFiles(profileDirectory);

            await using Stream sourceStream =
                await photo.OpenReadAsync();

            await using FileStream destinationStream =
                File.Create(localFilePath);

            await sourceStream.CopyToAsync(destinationStream);

            _profilePhotoPath = localFilePath;

            Preferences.Default.Set(
                "ProfilePhotoPath",
                localFilePath);

            DisplayProfilePhoto(localFilePath);

            ShowSuccess("Profile photo updated successfully.");
        }
        catch (FeatureNotSupportedException)
        {
            ShowError(
                "Photo selection is not supported on this device.");
        }
        catch (PermissionException)
        {
            ShowError(
                "SpendWise does not have permission to access your photos.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PROFILE PHOTO ERROR: {ex}");

            ShowError(
                $"Unable to update profile photo: {ex.Message}");
        }
    }

    // =====================================================
    // LOAD SAVED PROFILE PHOTO
    // =====================================================

    private void LoadProfilePhoto()
    {
        try
        {
            string savedPath =
                Preferences.Default.Get(
                    "ProfilePhotoPath",
                    string.Empty);

            if (string.IsNullOrWhiteSpace(savedPath))
            {
                ShowProfileInitial();
                return;
            }

            if (!File.Exists(savedPath))
            {
                Preferences.Default.Remove("ProfilePhotoPath");
                ShowProfileInitial();
                return;
            }

            _profilePhotoPath = savedPath;

            DisplayProfilePhoto(savedPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOAD PROFILE PHOTO ERROR: {ex}");
            ShowProfileInitial();
        }
    }

    // =====================================================
    // DISPLAY PROFILE PHOTO
    // =====================================================

    private void DisplayProfilePhoto(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            ShowProfileInitial();
            return;
        }

        ProfileImage.Source = ImageSource.FromFile(filePath);
        ProfileImage.IsVisible = true;
        ProfileInitialLabel.IsVisible = false;
        RemovePhotoButton.IsVisible = true;
        ChangePhotoButton.Text = "Change Photo";
    }

    // =====================================================
    // SHOW PROFILE INITIAL
    // =====================================================

    private void ShowProfileInitial()
    {
        ProfileImage.Source = null;
        ProfileImage.IsVisible = false;
        ProfileInitialLabel.IsVisible = true;
        RemovePhotoButton.IsVisible = false;
        ChangePhotoButton.Text = "Add Photo";

        _profilePhotoPath = null;
    }

    // =====================================================
    // REMOVE PROFILE PHOTO
    // =====================================================

    private async void RemovePhotoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        bool confirm = await DisplayAlert(
            "Remove Photo",
            "Are you sure you want to remove your profile photo?",
            "Remove",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            HideError();
            HideSuccess();

            string profileDirectory =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Profile");

            DeleteExistingProfilePhotoFiles(profileDirectory);

            Preferences.Default.Remove("ProfilePhotoPath");

            ShowProfileInitial();

            ShowSuccess("Profile photo removed.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"REMOVE PROFILE PHOTO ERROR: {ex}");

            ShowError(
                $"Unable to remove profile photo: {ex.Message}");
        }
    }

    // =====================================================
    // DELETE OLD PROFILE PHOTO FILES
    // =====================================================

    private static void DeleteExistingProfilePhotoFiles(
        string profileDirectory)
    {
        if (!Directory.Exists(profileDirectory))
            return;

        string[] extensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        foreach (string extension in extensions)
        {
            string path =
                Path.Combine(
                    profileDirectory,
                    "profile_photo" + extension);

            if (File.Exists(path))
                File.Delete(path);
        }
    }

    // =====================================================
    // DELETE MY ACCOUNT
    // =====================================================

    private async void DeleteAccountButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        HideError();
        HideSuccess();

        bool firstConfirmation = await DisplayAlert(
            "Permanently Delete Account",
            "Are you sure you want to permanently delete your SpendWise account?\n\n" +
            "Your account and associated data may be permanently deleted. " +
            "This action cannot be undone.",
            "Continue",
            "Cancel");

        if (!firstConfirmation)
            return;

        bool finalConfirmation = await DisplayAlert(
            "Final Confirmation",
            "This is your final warning. Do you really want to delete your account?",
            "Delete Permanently",
            "Keep My Account");

        if (!finalConfirmation)
            return;

        try
        {
            SetLoading(true);

            HideError();
            HideSuccess();

            var result =
                await _apiService.DeleteMyAccountAsync();

            if (!result.Success)
            {
                ShowError(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Unable to delete your account."
                        : result.Message);

                return;
            }

            // Clear saved authentication details.
            // ApiService also clears these after successful deletion.
            _authStorageService.Logout();

            // Remove the local profile photo.
            string profileDirectory =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Profile");

            DeleteExistingProfilePhotoFiles(profileDirectory);

            Preferences.Default.Remove("ProfilePhotoPath");

            // Clear the current profile from memory.
            _currentProfile = null;
            _profilePhotoPath = null;

            // Return the user to the welcome page.
            Application.Current!.MainPage =
                new NavigationPage(new WelcomePage());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DELETE ACCOUNT ERROR: {ex}");

            ShowError(
                $"Unable to delete your account: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    // =====================================================
    // VALIDATE EMAIL
    // =====================================================

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

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
    // VALIDATE SOUTH AFRICAN PHONE NUMBER
    // =====================================================

    private static bool IsValidSouthAfricanPhoneNumber(
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return true;

        return Regex.IsMatch(
            phoneNumber,
            @"^0\d{9}$");
    }

    // =====================================================
    // GET PROFILE INITIAL
    // =====================================================

    private static string GetInitial(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        string trimmed = fullName.Trim();

        if (trimmed.Length == 0)
            return "?";

        return trimmed
            .Substring(0, 1)
            .ToUpperInvariant();
    }

    // =====================================================
    // CLEAR PROFILE
    // =====================================================

    private void ClearProfile()
    {
        _currentProfile = null;

        FullNameEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        PhoneNumberEntry.Text = string.Empty;

        ProfileInitialLabel.Text = "?";
        ProfileNameLabel.Text = "SpendWise User";
        ProfileEmailLabel.Text = "-";

        AccountStatusLabel.Text = "Account status";
        AccountIdLabel.Text = "-";
        MemberSinceLabel.Text = "-";
        StatusValueLabel.Text = "-";
    }

    // =====================================================
    // SET LOADING
    // =====================================================

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        SaveProfileButton.IsEnabled = !isLoading;
        LogoutButton.IsEnabled = !isLoading;
        DeleteAccountButton.IsEnabled = !isLoading;

        ChangePhotoButton.IsEnabled = !isLoading;
        RemovePhotoButton.IsEnabled = !isLoading;

        FullNameEntry.IsEnabled = !isLoading;
        EmailEntry.IsEnabled = !isLoading;
        PhoneNumberEntry.IsEnabled = !isLoading;
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

        ErrorCard.IsVisible = true;
    }

    // =====================================================
    // HIDE ERROR
    // =====================================================

    private void HideError()
    {
        ErrorCard.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    // =====================================================
    // SHOW SUCCESS
    // =====================================================

    private void ShowSuccess(string? message)
    {
        HideError();

        SuccessLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Profile updated successfully."
                : message;

        SuccessCard.IsVisible = true;
    }

    // =====================================================
    // HIDE SUCCESS
    // =====================================================

    private void HideSuccess()
    {
        SuccessCard.IsVisible = false;
        SuccessLabel.Text = string.Empty;
    }

    // =====================================================
    // BACK BUTTON
    // =====================================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        Application.Current!.MainPage =
            new NavigationPage(new DashboardPage());
    }

    // =====================================================
    // REFRESH BUTTON
    // =====================================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadProfileAsync();
        LoadProfilePhoto();
    }

    // =====================================================
    // SAVE PROFILE BUTTON
    // =====================================================

    private async void SaveProfileButton_Clicked(
        object sender,
        EventArgs e)
    {
        await SaveProfileAsync();
    }

    // =====================================================
    // LOGOUT BUTTON
    // =====================================================

    private async void LogoutButton_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Log Out",
            "Are you sure you want to log out of Martin SpendWise?",
            "Log Out",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            SetLoading(true);

            HideError();
            HideSuccess();

            _authStorageService.Logout();

            Application.Current!.MainPage =
                new NavigationPage(new WelcomePage());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOGOUT ERROR: {ex}");

            ShowError(
                $"Unable to log out: {ex.Message}");

            SetLoading(false);
        }
    }
}
