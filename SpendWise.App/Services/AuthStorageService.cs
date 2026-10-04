namespace SpendWise.App.Services;

public class AuthStorageService
{
    private const string TokenKey = "auth_token";
    private const string UserIdKey = "user_id";
    private const string UserNameKey = "user_name";
    private const string UserEmailKey = "user_email";

    public async Task SaveLoginAsync(
        string token,
        int userId,
        string fullName,
        string email)
    {
#if WINDOWS
        // Temporary Windows development storage.
        // We will replace this with production-secure storage
        // before deployment.
        Preferences.Default.Set(TokenKey, token);
        Preferences.Default.Set(UserIdKey, userId.ToString());
        Preferences.Default.Set(UserNameKey, fullName);
        Preferences.Default.Set(UserEmailKey, email);

        await Task.CompletedTask;
#else
        await SecureStorage.Default.SetAsync(
            TokenKey,
            token);

        await SecureStorage.Default.SetAsync(
            UserIdKey,
            userId.ToString());

        await SecureStorage.Default.SetAsync(
            UserNameKey,
            fullName);

        await SecureStorage.Default.SetAsync(
            UserEmailKey,
            email);
#endif
    }

    public async Task<string?> GetTokenAsync()
    {
#if WINDOWS
        return await Task.FromResult(
            Preferences.Default.Get(
                TokenKey,
                string.Empty));
#else
        return await SecureStorage.Default.GetAsync(
            TokenKey);
#endif
    }

    public string GetUserName()
    {
        return Preferences.Default.Get(
            UserNameKey,
            string.Empty);
    }

    public string GetUserEmail()
    {
        return Preferences.Default.Get(
            UserEmailKey,
            string.Empty);
    }

    public void Logout()
    {
#if WINDOWS
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(UserNameKey);
        Preferences.Default.Remove(UserEmailKey);
#else
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(UserNameKey);
        SecureStorage.Default.Remove(UserEmailKey);
#endif
    }
}