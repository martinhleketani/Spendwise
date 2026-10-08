
using SpendWise.App.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SpendWise.App
{
    public partial class NotificationsPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly AuthStorageService _authStorage;

        // Same API address as your existing ApiService
        private const string ApiBaseUrl =
            "https://localhost:7284/";

        public NotificationsPage()
        {
            InitializeComponent();

            _authStorage = new AuthStorageService();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadNotificationsAsync();
        }

        // ============================================
        // SET JWT AUTHORIZATION
        // ============================================

        private async Task<bool> SetAuthorizationAsync()
        {
            string? token =
                await _authStorage.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert(
                    "Login Required",
                    "Please log in to view notifications.",
                    "OK");

                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return true;
        }

        // ============================================
        // LOAD NOTIFICATIONS
        // ============================================

        private async Task LoadNotificationsAsync()
        {
            try
            {
                if (!await SetAuthorizationAsync())
                    return;

                var response = await _httpClient.GetAsync(
                    "api/BudgetNotifications");

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert(
                        "Error",
                        $"Unable to load notifications: {response.StatusCode}",
                        "OK");
                    return;
                }

                var result = await response.Content
                    .ReadFromJsonAsync<NotificationsResponse>();

                NotificationsList.ItemsSource =
                    result?.Notifications ??
                    new List<NotificationItem>();

                UnreadCountLabel.Text =
                    $"{result?.UnreadCount ?? 0} unread notifications";
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Connection Error",
                    ex.Message,
                    "OK");
            }
        }

        // ============================================
        // REFRESH
        // ============================================

        private async void OnRefreshClicked(
            object sender,
            EventArgs e)
        {
            await LoadNotificationsAsync();
        }

        // ============================================
        // MARK ONE NOTIFICATION AS READ
        // ============================================

        private async void OnMarkReadClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter == null)
                return;

            int id = Convert.ToInt32(
                button.CommandParameter);

            try
            {
                if (!await SetAuthorizationAsync())
                    return;

                var response = await _httpClient.PutAsync(
                    $"api/BudgetNotifications/{id}/read",
                    null);

                if (response.IsSuccessStatusCode)
                {
                    await LoadNotificationsAsync();
                }
                else
                {
                    await DisplayAlert(
                        "Error",
                        "Could not mark notification as read.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }

        // ============================================
        // MARK ALL NOTIFICATIONS AS READ
        // ============================================

        private async void OnMarkAllReadClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                if (!await SetAuthorizationAsync())
                    return;

                var response = await _httpClient.PutAsync(
                    "api/BudgetNotifications/read-all",
                    null);

                if (response.IsSuccessStatusCode)
                {
                    await LoadNotificationsAsync();
                }
                else
                {
                    await DisplayAlert(
                        "Error",
                        "Could not mark notifications as read.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }
    }

    public class NotificationsResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("unreadCount")]
        public int UnreadCount { get; set; }

        [JsonPropertyName("notifications")]
        public List<NotificationItem> Notifications { get; set; } = new();
    }

    public class NotificationItem
    {
        public int Id { get; set; }

        public string Category { get; set; } = "";

        public int Threshold { get; set; }

        public string Message { get; set; } = "";

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsUnread => !IsRead;

        public string Status =>
            IsRead ? "Read" : "Unread";
    }
}
