
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

        LoadUser();
        SetBankNotConnected();
    }

    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        LoadUser();
        SetBankNotConnected();

        await LoadDashboardAsync();
        await LoadUnreadNotificationsAsync();
    }

    // ==========================================
    // USER
    // ==========================================

    private void LoadUser()
    {
        string fullName =
            _authStorageService.GetUserName();

        WelcomeLabel.Text =
            string.IsNullOrWhiteSpace(fullName)
                ? "Welcome back"
                : $"Welcome back, {fullName}";
    }

    // ==========================================
    // BANK CONNECTION
    // ==========================================

    private void SetBankNotConnected()
    {
        WalletBalanceLabel.Text = "—";

        BankStatusLabel.Text = "Not Connected";

        BankAccountInfoLabel.Text =
            "Connect your bank securely to view your account balance.";

        ConnectBankButton.Text =
            "Connect Bank Account";

        ConnectBankButton.IsEnabled = true;
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

            var result =
                await _apiService.GetDashboardAsync(
                    today.Year,
                    today.Month);

            if (!result.Success ||
                result.Data == null)
            {
                ClearDashboard();

                await DisplayAlert(
                    "Dashboard",
                    result.Message,
                    "OK");

                return;
            }

            DisplayDashboard(result.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"LOAD DASHBOARD ERROR: {ex}");

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

    private void DisplayDashboard(
        DashboardResponse dashboard)
    {
        SetBankNotConnected();

        decimal budgetLimit =
            dashboard.MonthlyBudget.Limit;

        decimal spent =
            dashboard.MonthlyBudget.Spent;

        decimal remaining =
            dashboard.MonthlyBudget.Remaining;

        decimal percentageUsed =
            dashboard.MonthlyBudget.PercentageUsed;

        BudgetSpentLabel.Text =
            $"R {spent:N2} spent of R {budgetLimit:N2}";

        BudgetRemainingLabel.Text =
            $"R {remaining:N2} remaining";

        BudgetPercentageLabel.Text =
            $"{percentageUsed:N0}%";

        double progress = 0;

        if (budgetLimit > 0)
        {
            progress = (double)(spent / budgetLimit);
        }

        BudgetProgressBar.Progress =
            Math.Clamp(progress, 0, 1);

        MonthlySpentLabel.Text =
            $"R {spent:N2}";

        MonthlyRemainingLabel.Text =
            $"R {remaining:N2}";

        DisplayRecentTransactions(
            dashboard.RecentTransactions);
    }

    // ==========================================
    // RECENT TRANSACTIONS
    // ==========================================

    private void DisplayRecentTransactions(
        List<DashboardTransaction> transactions)
    {
        RecentTransactionsLayout.Children.Clear();

        if (transactions == null ||
            transactions.Count == 0)
        {
            ShowNoRecentSpending();
            return;
        }

        var spendingTransactions =
            transactions
                .Where(IsSpendingTransaction)
                .ToList();

        if (spendingTransactions.Count == 0)
        {
            ShowNoRecentSpending();
            return;
        }

        for (int i = 0;
             i < spendingTransactions.Count;
             i++)
        {
            DashboardTransaction transaction =
                spendingTransactions[i];

            RecentTransactionsLayout.Children.Add(
                CreateTransactionView(transaction));

            if (i < spendingTransactions.Count - 1)
            {
                RecentTransactionsLayout.Children.Add(
                    new BoxView
                    {
                        HeightRequest = 1,
                        BackgroundColor =
                            Color.FromArgb("#EEEEEE")
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
            string.IsNullOrWhiteSpace(
                transaction.Description)
                ? "Recorded Purchase"
                : transaction.Description;

        var descriptionLabel = new Label
        {
            Text = description,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1E1E")
        };

        var dateLabel = new Label
        {
            Text = transaction.CreatedAt
                .ToLocalTime()
                .ToString("dd MMM yyyy"),
            FontSize = 12,
            TextColor = Color.FromArgb("#777777")
        };

        var amountLabel = new Label
        {
            Text =
                $"-R {Math.Abs(transaction.Amount):N2}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#C0392B"),
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
                TextColor = Color.FromArgb("#777777"),
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

        BudgetSpentLabel.Text =
            "R 0.00 spent of R 0.00";

        BudgetRemainingLabel.Text =
            "R 0.00 remaining";

        BudgetPercentageLabel.Text = "0%";

        BudgetProgressBar.Progress = 0;

        MonthlySpentLabel.Text = "R 0.00";
        MonthlyRemainingLabel.Text = "R 0.00";

        ShowNoRecentSpending();
    }

    // ==========================================
    // LOAD UNREAD NOTIFICATIONS
    // ==========================================

    private async Task LoadUnreadNotificationsAsync()
    {
        try
        {
            string? token =
                await _authStorageService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                UnreadNotificationsLabel.IsVisible = false;
                return;
            }

            _notificationsClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);

            using var response =
                await _notificationsClient.GetAsync(
                    "api/BudgetNotifications/unread-count");

            if (!response.IsSuccessStatusCode)
            {
                UnreadNotificationsLabel.IsVisible = false;

                Debug.WriteLine(
                    $"Notification count failed: {response.StatusCode}");

                return;
            }

            var result = await response.Content
                .ReadFromJsonAsync<UnreadNotificationCountResponse>();

            int unreadCount = result?.UnreadCount ?? 0;

            UnreadNotificationsLabel.Text =
                unreadCount > 99
                    ? "99+"
                    : unreadCount.ToString();

            UnreadNotificationsLabel.IsVisible =
                unreadCount > 0;
        }
        catch (Exception ex)
        {
            UnreadNotificationsLabel.IsVisible = false;

            Debug.WriteLine(
                $"NOTIFICATION ERROR: {ex.Message}");
        }
    }

    // ==========================================
    // CONNECT BANK
    // ==========================================

    private async void ConnectBankButton_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Bank Connection",
            "Secure bank account connection will be added when the banking provider integration is configured.",
            "OK");
    }

    // ==========================================
    // NAVIGATION
    // ==========================================

    private async void WalletButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new SpendingActivityPage());
    }

    private async void BudgetButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new BudgetPage());
    }

    private async void GroceryButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new GroceryPage());
    }

    private async void MyGroceryListsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MyGroceryListsPage());
    }

    private async void PurchaseButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PurchasePage());
    }

    private async void ReportsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ReportsPage());
    }

    private async void InsightsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new InsightsPage());
    }

    private async void ViewTransactionsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new TransactionsPage());
    }

    private async void ProfileButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ProfilePage());
    }

    // ==========================================
    // OPEN NOTIFICATIONS
    // ==========================================

    private async void NotificationsButton_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(
                new SpendWise.App.NotificationsPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Navigation Error",
                ex.Message,
                "OK");
        }
    }

    // ==========================================
    // OPEN HELP & SUPPORT
    // ==========================================

    private async void HelpButton_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(
                new HelpPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Navigation Error",
                ex.Message,
                "OK");
        }
    }
}

// ==========================================
// NOTIFICATION COUNT RESPONSE
// ==========================================

public class UnreadNotificationCountResponse
{
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}
