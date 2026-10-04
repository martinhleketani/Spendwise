using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;

namespace SpendWise.App.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthStorageService _authStorageService;

    private bool _isLoading;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DashboardPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        _authStorageService =
            new AuthStorageService();

        LoadUser();

        SetBankNotConnected();
    }


    // =====================================================
    // PAGE APPEARING
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reload the stored name in case it was
        // changed from the Profile page.
        LoadUser();

        // Until a real banking provider is integrated,
        // never display a compatibility wallet balance
        // as if it were a bank balance.
        SetBankNotConnected();

        await LoadDashboardAsync();
    }


    // =====================================================
    // LOAD LOGGED-IN USER
    // =====================================================

    private void LoadUser()
    {
        string fullName =
            _authStorageService.GetUserName();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            WelcomeLabel.Text =
                $"Welcome back, {fullName}";
        }
        else
        {
            WelcomeLabel.Text =
                "Welcome back";
        }
    }


    // =====================================================
    // BANK NOT CONNECTED STATE
    // =====================================================

    private void SetBankNotConnected()
    {
        // WalletBalanceLabel keeps its old XAML name
        // temporarily.
        //
        // It does NOT represent the old SpendWise wallet.

        WalletBalanceLabel.Text =
            "—";

        BankStatusLabel.Text =
            "Not Connected";

        BankAccountInfoLabel.Text =
            "Connect your bank securely to view your account balance.";

        ConnectBankButton.Text =
            "Connect Bank Account";

        ConnectBankButton.IsEnabled =
            true;
    }


    // =====================================================
    // LOAD DASHBOARD FROM API
    // =====================================================

    private async Task LoadDashboardAsync()
    {
        if (_isLoading)
        {
            return;
        }

        try
        {
            _isLoading =
                true;

            DateTime today =
                DateTime.Now;

            int year =
                today.Year;

            int month =
                today.Month;


            var result =
                await _apiService
                    .GetDashboardAsync(
                        year,
                        month);


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


            DisplayDashboard(
                result.Data);
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
            _isLoading =
                false;
        }
    }


    // =====================================================
    // DISPLAY DASHBOARD DATA
    // =====================================================

    private void DisplayDashboard(
        DashboardResponse dashboard)
    {
        // =================================================
        // BANK ACCOUNT
        // =================================================
        //
        // Do not use the old compatibility wallet balance.
        // A real bank balance will eventually come from
        // the banking/open-finance provider.

        SetBankNotConnected();


        // =================================================
        // MONTHLY BUDGET
        // =================================================

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


        // =================================================
        // PROGRESS BAR
        // =================================================

        double progress =
            0;


        if (budgetLimit > 0)
        {
            progress =
                (double)(
                    spent /
                    budgetLimit);
        }


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        BudgetProgressBar.Progress =
            progress;


        // =================================================
        // MONTHLY SUMMARY
        // =================================================

        MonthlySpentLabel.Text =
            $"R {spent:N2}";

        MonthlyRemainingLabel.Text =
            $"R {remaining:N2}";


        // =================================================
        // RECENT SPENDING
        // =================================================

        DisplayRecentTransactions(
            dashboard.RecentTransactions);
    }


    // =====================================================
    // DISPLAY RECENT TRANSACTIONS
    // =====================================================

    private void DisplayRecentTransactions(
        List<DashboardTransaction> transactions)
    {
        RecentTransactionsLayout
            .Children
            .Clear();


        if (transactions == null ||
            transactions.Count == 0)
        {
            ShowNoRecentSpending();

            return;
        }


        // Only display actual spending records.

        var spendingTransactions =
            transactions
                .Where(
                    IsSpendingTransaction)
                .ToList();


        if (spendingTransactions.Count == 0)
        {
            ShowNoRecentSpending();

            return;
        }


        for (
            int i = 0;
            i < spendingTransactions.Count;
            i++)
        {
            DashboardTransaction transaction =
                spendingTransactions[i];


            Grid transactionGrid =
                CreateTransactionView(
                    transaction);


            RecentTransactionsLayout
                .Children
                .Add(
                    transactionGrid);


            if (i <
                spendingTransactions.Count - 1)
            {
                RecentTransactionsLayout
                    .Children
                    .Add(
                        new BoxView
                        {
                            HeightRequest =
                                1,

                            BackgroundColor =
                                Color.FromArgb(
                                    "#EEEEEE")
                        });
            }
        }
    }


    // =====================================================
    // IS SPENDING TRANSACTION
    // =====================================================

    private static bool IsSpendingTransaction(
        DashboardTransaction transaction)
    {
        if (string.Equals(
            transaction.Type,
            "Purchase",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        if (transaction.Amount < 0)
        {
            return true;
        }


        return false;
    }


    // =====================================================
    // CREATE TRANSACTION VIEW
    // =====================================================

    private Grid CreateTransactionView(
        DashboardTransaction transaction)
    {
        Grid transactionGrid =
            new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width =
                            GridLength.Star
                    },

                    new ColumnDefinition
                    {
                        Width =
                            GridLength.Auto
                    }
                },

                RowDefinitions =
                {
                    new RowDefinition
                    {
                        Height =
                            GridLength.Auto
                    },

                    new RowDefinition
                    {
                        Height =
                            GridLength.Auto
                    }
                },

                Padding =
                    new Thickness(
                        0,
                        7)
            };


        // =================================================
        // DESCRIPTION
        // =================================================

        string description =
            string.IsNullOrWhiteSpace(
                transaction.Description)
                ? "Recorded Purchase"
                : transaction.Description;


        Label descriptionLabel =
            new Label
            {
                Text =
                    description,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        // =================================================
        // DATE
        // =================================================

        Label dateLabel =
            new Label
            {
                Text =
                    transaction.CreatedAt
                        .ToLocalTime()
                        .ToString(
                            "dd MMM yyyy"),

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb(
                        "#777777")
            };


        // =================================================
        // AMOUNT
        // =================================================

        Label amountLabel =
            new Label
            {
                Text =
                    $"-R {Math.Abs(transaction.Amount):N2}",

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#C0392B"),

                VerticalOptions =
                    LayoutOptions.Center
            };


        // =================================================
        // GRID POSITIONS
        // =================================================

        Grid.SetRow(
            descriptionLabel,
            0);

        Grid.SetColumn(
            descriptionLabel,
            0);


        Grid.SetRow(
            dateLabel,
            1);

        Grid.SetColumn(
            dateLabel,
            0);


        Grid.SetRowSpan(
            amountLabel,
            2);

        Grid.SetColumn(
            amountLabel,
            1);


        transactionGrid
            .Children
            .Add(
                descriptionLabel);

        transactionGrid
            .Children
            .Add(
                dateLabel);

        transactionGrid
            .Children
            .Add(
                amountLabel);


        return transactionGrid;
    }


    // =====================================================
    // NO RECENT SPENDING
    // =====================================================

    private void ShowNoRecentSpending()
    {
        RecentTransactionsLayout
            .Children
            .Clear();


        RecentTransactionsLayout
            .Children
            .Add(
                new Label
                {
                    Text =
                        "No recent spending recorded yet.",

                    TextColor =
                        Color.FromArgb(
                            "#777777"),

                    HorizontalOptions =
                        LayoutOptions.Center,

                    HorizontalTextAlignment =
                        TextAlignment.Center,

                    Margin =
                        new Thickness(
                            0,
                            15)
                });
    }


    // =====================================================
    // CLEAR DASHBOARD
    // =====================================================

    private void ClearDashboard()
    {
        SetBankNotConnected();


        BudgetSpentLabel.Text =
            "R 0.00 spent of R 0.00";

        BudgetRemainingLabel.Text =
            "R 0.00 remaining";

        BudgetPercentageLabel.Text =
            "0%";

        BudgetProgressBar.Progress =
            0;


        MonthlySpentLabel.Text =
            "R 0.00";

        MonthlyRemainingLabel.Text =
            "R 0.00";


        ShowNoRecentSpending();
    }


    // =====================================================
    // CONNECT BANK
    // =====================================================

    private async void ConnectBankButton_Clicked(
        object sender,
        EventArgs e)
    {
        // Real bank integration will be connected later
        // through an authorised banking/payment provider.

        await DisplayAlert(
            "Bank Connection",
            "Secure bank account connection will be added when the banking provider integration is configured.",
            "OK");
    }


    // =====================================================
    // SPENDING ACTIVITY
    // =====================================================

    private async void WalletButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new SpendingActivityPage());
    }


    // =====================================================
    // BUDGET
    // =====================================================

    private async void BudgetButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new BudgetPage());
    }


    // =====================================================
    // GROCERY
    // =====================================================

    private async void GroceryButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new GroceryPage());
    }


    // =====================================================
    // PURCHASE
    // =====================================================

    private async void PurchaseButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PurchasePage());
    }


    // =====================================================
    // REPORTS
    // =====================================================

    private async void ReportsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ReportsPage());
    }


    // =====================================================
    // INSIGHTS
    // =====================================================

    private async void InsightsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new InsightsPage());
    }


    // =====================================================
    // TRANSACTIONS
    // =====================================================

    private async void ViewTransactionsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new TransactionsPage());
    }


    // =====================================================
    // PROFILE
    // =====================================================

    private async void ProfileButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ProfilePage());
    }
}