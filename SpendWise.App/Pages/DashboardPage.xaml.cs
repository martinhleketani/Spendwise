
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SpendWise.App.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthStorageService _authStorageService;

    private readonly HttpClient _notificationsClient = new()
    {
        BaseAddress = new Uri("https://localhost:7284/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private bool _isLoading;

    public DashboardPage()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _authStorageService = new AuthStorageService();

        SetBankNotConnected();
    }

    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadDashboardAsync();
        await LoadUnreadNotificationsAsync();
    }

    // ==========================================
    // BANK ACCOUNT
    // ==========================================

    private void SetBankNotConnected()
    {
        BankStatusLabel.Text = "Not Connected";
    }

    private async void OnConnectBankClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Bank Connection",
            "Secure bank account connection will be added when the banking provider integration is configured. Your bank balance is not currently available through SpendWise.",
            "OK");
    }

    // ==========================================
    // LOAD DASHBOARD
    // ==========================================

    private async Task LoadDashboardAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            DateTime today = DateTime.Now;

            var result = await _apiService.GetDashboardAsync(
                today.Year,
                today.Month);

            if (!result.Success || result.Data == null)
            {
                ClearDashboard();

                await DisplayAlert(
                    "Dashboard",
                    result.Message ?? "Unable to load dashboard information.",
                    "OK");

                return;
            }

            DisplayDashboard(result.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOAD DASHBOARD ERROR: {ex}");

            ClearDashboard();

            await DisplayAlert(
                "Dashboard Error",
                $"Unable to load dashboard: {ex.Message}",
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ==========================================
    // DISPLAY DASHBOARD
    // ==========================================

    private void DisplayDashboard(DashboardResponse dashboard)
    {
        SetBankNotConnected();

        decimal budgetLimit = dashboard.MonthlyBudget.Limit;
        decimal spent = dashboard.MonthlyBudget.Spent;
        decimal remaining = dashboard.MonthlyBudget.Remaining;
        decimal percentageUsed = dashboard.MonthlyBudget.PercentageUsed;

        TotalSpentLabel.Text = $"R {spent:N2}";

        BudgetRemainingLabel.Text = $"R {remaining:N2}";

        RemainingSummaryLabel.Text =
            $"R {remaining:N2} remaining";

        BudgetPercentLabel.Text =
            $"{percentageUsed:N0}% used";

        double progress = 0;

        if (budgetLimit > 0)
        {
            progress = (double)(spent / budgetLimit);
        }

        BudgetProgressBar.Progress =
            Math.Clamp(progress, 0, 1);

        DisplayRecentTransactions(
            dashboard.RecentTransactions);
    }

    // ==========================================
    // RECENT SPENDING
    // ==========================================

    private void DisplayRecentTransactions(
        List<DashboardTransaction> transactions)
    {
        RecentTransactionsLayout.Children.Clear();

        if (transactions == null || transactions.Count == 0)
        {
            ShowNoRecentSpending();
            return;
        }

        var spendingTransactions = transactions
            .Where(IsSpendingTransaction)
            .ToList();

        if (spendingTransactions.Count == 0)
        {
            ShowNoRecentSpending();
            return;
        }

        for (int i = 0; i < spendingTransactions.Count; i++)
        {
            var transaction = spendingTransactions[i];

            RecentTransactionsLayout.Children.Add(
                CreateTransactionView(transaction));

            if (i < spendingTransactions.Count - 1)
            {
                RecentTransactionsLayout.Children.Add(
                    new BoxView
                    {
                        HeightRequest = 1,
                        BackgroundColor = Color.FromArgb("#EEEEEE")
                    });
            }
        }
    }

    private static bool IsSpendingTransaction(
        DashboardTransaction transaction)
    {
        return string.Equals(
                   transaction.Type,
                   "Purchase",
                   StringComparison.OrdinalIgnoreCase)
               || transaction.Amount < 0;
    }

    private Grid CreateTransactionView(
        DashboardTransaction transaction)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = GridLength.Star
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            },

            RowDefinitions =
            {
                new RowDefinition
                {
                    Height = GridLength.Auto
                },
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            },

            Padding = new Thickness(0, 7)
        };

        string description =
            string.IsNullOrWhiteSpace(transaction.Description)
                ? "Recorded Purchase"
                : transaction.Description;

        var descriptionLabel = new Label
        {
            Text = description,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A")
        };

        var dateLabel = new Label
        {
            Text = transaction.CreatedAt
                .ToLocalTime()
                .ToString("dd MMM yyyy"),

            FontSize = 12,
            TextColor = Color.FromArgb("#64748B")
        };

        var amountLabel = new Label
        {
            Text = $"-R {Math.Abs(transaction.Amount):N2}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#DC2626"),
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetRow(descriptionLabel, 0);
        Grid.SetColumn(descriptionLabel, 0);

        Grid.SetRow(dateLabel, 1);
        Grid.SetColumn(dateLabel, 0);

        Grid.SetRowSpan(amountLabel, 2);
        Grid.SetColumn(amountLabel, 1);

        grid.Children.Add(descriptionLabel);
        grid.Children.Add(dateLabel);
        grid.Children.Add(amountLabel);

        return grid;
    }

    private void ShowNoRecentSpending()
    {
        RecentTransactionsLayout.Children.Clear();

        RecentTransactionsLayout.Children.Add(
            new Label
            {
                Text = "No recent spending recorded yet.",
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 15)
            });
    }

    // ==========================================
    // CLEAR DASHBOARD
    // ==========================================

    private void ClearDashboard()
    {
        SetBankNotConnected();

        TotalSpentLabel.Text = "R 0.00";

        BudgetRemainingLabel.Text = "R 0.00";

        RemainingSummaryLabel.Text =
            "R 0.00 remaining";

        BudgetPercentLabel.Text = "0% used";

        BudgetProgressBar.Progress = 0;

        ShowNoRecentSpending();
    }

    // ==========================================
    // UNREAD NOTIFICATIONS
    // ==========================================

    private async Task LoadUnreadNotificationsAsync()
    {
        try
        {
            string? token =
                await _authStorageService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                SetUnreadNotificationCount(0);
                return;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/BudgetNotifications/unread-count");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var response =
                await _notificationsClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                SetUnreadNotificationCount(0);

                Debug.WriteLine(
                    $"Notification count failed: {response.StatusCode}");

                return;
            }

            var result = await response.Content
                .ReadFromJsonAsync<UnreadNotificationCountResponse>();

            SetUnreadNotificationCount(
                result?.UnreadCount ?? 0);
        }
        catch (Exception ex)
        {
            SetUnreadNotificationCount(0);

            Debug.WriteLine(
                $"NOTIFICATION ERROR: {ex.Message}");
        }
    }

    private void SetUnreadNotificationCount(int count)
    {
        NotificationBadge.IsVisible = count > 0;

        NotificationCountLabel.Text =
            count > 99 ? "99+" : count.ToString();
    }

    // ==========================================
    // NAVIGATION
    // ==========================================

    private async void OnNotificationsTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new SpendWise.App.NotificationsPage());
    }

    private async void OnProfileTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new ProfilePage());
    }

    private async void OnViewBudgetTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new BudgetPage());
    }

    private async void OnGroceryListsTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new MyGroceryListsPage());
    }

    private async void OnGroceryTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new GroceryPage());
    }

    private async void OnActivityTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new SpendingActivityPage());
    }

    private async void OnViewTransactionsTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new TransactionsPage());
    }

    private async void OnPurchaseTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new PurchasePage());
    }

    private async void OnReportsTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new ReportsPage());
    }

    private async void OnInsightsTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new InsightsPage());
    }
}

// ==========================================
// NOTIFICATION RESPONSE
// ==========================================

public class UnreadNotificationCountResponse
{
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}
