using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace SpendWise.App.Pages;

public partial class TransactionsPage : ContentPage
{
    private readonly ApiService _apiService;

    private List<TransactionResponse> _allTransactions =
        new();

    private bool _isLoading;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public TransactionsPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        TransactionFilterPicker.SelectedIndex =
            0;
    }


    // =====================================================
    // PAGE APPEARING
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTransactionsAsync();
    }


    // =====================================================
    // LOAD SPENDING HISTORY
    // =====================================================

    private async Task LoadTransactionsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        try
        {
            SetLoading(true);

            HideError();

            var result =
                await _apiService
                    .GetTransactionsAsync();


            if (!result.Success ||
                result.Data == null)
            {
                _allTransactions.Clear();

                ClearPage();

                ShowError(
                    result.Message);

                return;
            }


            // =============================================
            // ONLY KEEP SPENDING RECORDS
            // =============================================
            //
            // SpendWise currently displays recorded
            // purchases and spending transactions here.
            //
            // Old wallet deposits, credits and refunds
            // are excluded from this spending view.
            // =============================================

            _allTransactions =
                result.Data
                    .Where(
                        IsSpendingTransaction)
                    .OrderByDescending(
                        transaction =>
                            transaction.CreatedAt)
                    .ToList();


            DisplaySummary();

            ApplyCurrentFilter();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"LOAD SPENDING HISTORY ERROR: {ex}");


            _allTransactions.Clear();

            ClearPage();


            ShowError(
                $"Unable to load spending history: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // DISPLAY SPENDING SUMMARY
    // =====================================================

    private void DisplaySummary()
    {
        decimal totalSpent =
            _allTransactions
                .Sum(
                    transaction =>
                        Math.Abs(
                            transaction.Amount));


        int purchaseCount =
            _allTransactions.Count;


        decimal averagePurchase =
            purchaseCount > 0
                ? totalSpent / purchaseCount
                : 0;


        // =============================================
        // MoneyOutLabel displays total recorded spending.
        // =============================================

        MoneyOutLabel.Text =
            FormatMoney(
                totalSpent);


        TransactionCountLabel.Text =
            purchaseCount.ToString();


        // =============================================
        // NetActivityLabel displays average purchase.
        // =============================================

        NetActivityLabel.Text =
            FormatMoney(
                averagePurchase);


        NetActivityLabel.TextColor =
            Color.FromArgb(
                "#1E1E1E");
    }


    // =====================================================
    // APPLY CURRENT FILTER
    // =====================================================

    private void ApplyCurrentFilter()
    {
        int selectedIndex =
            TransactionFilterPicker.SelectedIndex;


        IEnumerable<TransactionResponse>
            filteredTransactions =
                _allTransactions;


        // =================================================
        // GROCERIES
        // =================================================

        if (selectedIndex == 1)
        {
            filteredTransactions =
                _allTransactions
                    .Where(
                        IsGroceryTransaction);


            TransactionSectionTitle.Text =
                "Groceries";


            TransactionSectionSubtitle.Text =
                "Recorded grocery purchases appear here.";
        }


        // =================================================
        // OTHER PURCHASES
        // =================================================

        else if (selectedIndex == 2)
        {
            filteredTransactions =
                _allTransactions
                    .Where(
                        transaction =>
                            !IsGroceryTransaction(
                                transaction));


            TransactionSectionTitle.Text =
                "Other Purchases";


            TransactionSectionSubtitle.Text =
                "Recorded non-grocery purchases appear here.";
        }


        // =================================================
        // ALL SPENDING
        // =================================================

        else
        {
            TransactionSectionTitle.Text =
                "All Spending";


            TransactionSectionSubtitle.Text =
                "Your most recently recorded purchases appear first.";
        }


        List<TransactionResponse> transactions =
            filteredTransactions
                .OrderByDescending(
                    transaction =>
                        transaction.CreatedAt)
                .ToList();


        DisplayTransactions(
            transactions);
    }


    // =====================================================
    // DISPLAY TRANSACTIONS
    // =====================================================

    private void DisplayTransactions(
        List<TransactionResponse> transactions)
    {
        TransactionsLayout
            .Children
            .Clear();


        DisplayedCountLabel.Text =
            transactions.Count
                .ToString();


        if (transactions.Count == 0)
        {
            EmptyStateCard.IsVisible =
                true;


            if (_allTransactions.Count == 0)
            {
                EmptyStateLabel.Text =
                    "Purchases you record in SpendWise will appear here.";
            }
            else
            {
                EmptyStateLabel.Text =
                    "No spending records match the selected filter.";
            }


            return;
        }


        EmptyStateCard.IsVisible =
            false;


        foreach (
            TransactionResponse transaction
            in transactions)
        {
            Border transactionCard =
                CreateTransactionCard(
                    transaction);


            TransactionsLayout
                .Children
                .Add(
                    transactionCard);
        }
    }


    // =====================================================
    // CREATE SPENDING CARD
    // =====================================================

    private Border CreateTransactionCard(
        TransactionResponse transaction)
    {
        Color transactionColor =
            Color.FromArgb(
                "#C0392B");


        Color transactionBackground =
            Color.FromArgb(
                "#FFF1F0");


        // =================================================
        // DISPLAY TYPE
        // =================================================

        string transactionType =
            GetDisplayType(
                transaction);


        Label typeLabel =
            new Label
            {
                Text =
                    transactionType,

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E"),

                VerticalOptions =
                    LayoutOptions.Center
            };


        // =================================================
        // AMOUNT
        // =================================================

        Label amountLabel =
            new Label
            {
                Text =
                    FormatTransactionAmount(
                        transaction),

                FontSize =
                    17,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    transactionColor,

                HorizontalOptions =
                    LayoutOptions.End,

                VerticalOptions =
                    LayoutOptions.Center
            };


        Grid headerGrid =
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

                ColumnSpacing =
                    10
            };


        Grid.SetColumn(
            typeLabel,
            0);


        Grid.SetColumn(
            amountLabel,
            1);


        headerGrid.Children.Add(
            typeLabel);


        headerGrid.Children.Add(
            amountLabel);


        // =================================================
        // DESCRIPTION
        // =================================================

        Label descriptionLabel =
            new Label
            {
                Text =
                    GetDescription(
                        transaction),

                FontSize =
                    13,

                TextColor =
                    Color.FromArgb(
                        "#555555"),

                LineBreakMode =
                    LineBreakMode.WordWrap
            };


        // =================================================
        // CATEGORY
        // =================================================

        Label categoryLabel =
            CreateDetailLabel(
                "Category",
                transaction.Category);


        // =================================================
        // RETAILER
        // =================================================

        Label retailerLabel =
            CreateDetailLabel(
                "Retailer",
                transaction.Retailer);


        // =================================================
        // DATE
        // =================================================

        Label dateLabel =
            CreateDetailLabel(
                "Date",
                FormatDate(
                    transaction.CreatedAt));


        // =================================================
        // REFERENCE
        // =================================================

        Label referenceLabel =
            CreateDetailLabel(
                "Reference",
                transaction.Reference);


        // =================================================
        // DETAILS
        // =================================================

        VerticalStackLayout detailsLayout =
            new VerticalStackLayout
            {
                Spacing =
                    5
            };


        if (categoryLabel.IsVisible)
        {
            detailsLayout.Children.Add(
                categoryLabel);
        }


        if (retailerLabel.IsVisible)
        {
            detailsLayout.Children.Add(
                retailerLabel);
        }


        detailsLayout.Children.Add(
            dateLabel);


        if (referenceLabel.IsVisible)
        {
            detailsLayout.Children.Add(
                referenceLabel);
        }


        // =================================================
        // SPENDING STATUS
        // =================================================

        Border statusBorder =
            new Border
            {
                BackgroundColor =
                    transactionBackground,

                Stroke =
                    transactionColor,

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(
                        10,
                        5),

                HorizontalOptions =
                    LayoutOptions.Start,

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                20)
                    },

                Content =
                    new Label
                    {
                        Text =
                            "Recorded Spending",

                        FontSize =
                            10,

                        FontAttributes =
                            FontAttributes.Bold,

                        TextColor =
                            transactionColor
                    }
            };


        // =================================================
        // CONTENT
        // =================================================

        VerticalStackLayout content =
            new VerticalStackLayout
            {
                Spacing =
                    10
            };


        content.Children.Add(
            headerGrid);


        content.Children.Add(
            statusBorder);


        content.Children.Add(
            descriptionLabel);


        content.Children.Add(
            detailsLayout);


        // =================================================
        // CARD
        // =================================================

        Border card =
            new Border
            {
                BackgroundColor =
                    Colors.White,

                Stroke =
                    Color.FromArgb(
                        "#E5E5E5"),

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(
                        16),

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                14)
                    },

                Content =
                    content
            };


        return card;
    }


    // =====================================================
    // GET DISPLAY TYPE
    // =====================================================

    private string GetDisplayType(
        TransactionResponse transaction)
    {
        if (IsGroceryTransaction(
            transaction))
        {
            return "Grocery Purchase";
        }


        return "Purchase";
    }


    // =====================================================
    // CREATE DETAIL LABEL
    // =====================================================

    private Label CreateDetailLabel(
        string title,
        string? value)
    {
        bool hasValue =
            !string.IsNullOrWhiteSpace(
                value);


        return new Label
        {
            Text =
                hasValue
                    ? $"{title}: {value}"
                    : string.Empty,

            FontSize =
                11,

            TextColor =
                Color.FromArgb(
                    "#777777"),

            IsVisible =
                hasValue,

            LineBreakMode =
                LineBreakMode.WordWrap
        };
    }


    // =====================================================
    // GET DESCRIPTION
    // =====================================================

    private string GetDescription(
        TransactionResponse transaction)
    {
        if (!string.IsNullOrWhiteSpace(
            transaction.Description))
        {
            return transaction.Description;
        }


        if (!string.IsNullOrWhiteSpace(
            transaction.Retailer))
        {
            return transaction.Retailer;
        }


        if (!string.IsNullOrWhiteSpace(
            transaction.Category))
        {
            return transaction.Category;
        }


        return "Recorded purchase";
    }


    // =====================================================
    // IS SPENDING TRANSACTION
    // =====================================================

    private bool IsSpendingTransaction(
        TransactionResponse transaction)
    {
        if (string.Equals(
            transaction.Type,
            "Deposit",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


        if (string.Equals(
            transaction.Type,
            "Credit",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


        if (string.Equals(
            transaction.Type,
            "Refund",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


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
    // IS GROCERY TRANSACTION
    // =====================================================

    private bool IsGroceryTransaction(
        TransactionResponse transaction)
    {
        if (!string.IsNullOrWhiteSpace(
                transaction.Category) &&
            transaction.Category.Equals(
                "Groceries",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        if (!string.IsNullOrWhiteSpace(
                transaction.Description) &&
            transaction.Description.Contains(
                "grocery",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        return false;
    }


    // =====================================================
    // FORMAT TRANSACTION AMOUNT
    // =====================================================

    private string FormatTransactionAmount(
        TransactionResponse transaction)
    {
        decimal amount =
            Math.Abs(
                transaction.Amount);


        return
            $"-{FormatMoney(amount)}";
    }


    // =====================================================
    // FORMAT MONEY
    // =====================================================

    private string FormatMoney(
        decimal amount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "R {0:N2}",
            amount);
    }


    // =====================================================
    // FORMAT DATE
    // =====================================================

    private string FormatDate(
        DateTime date)
    {
        return date
            .ToLocalTime()
            .ToString(
                "dd MMM yyyy, HH:mm");
    }


    // =====================================================
    // CLEAR PAGE
    // =====================================================

    private void ClearPage()
    {
        MoneyOutLabel.Text =
            "R 0.00";


        TransactionCountLabel.Text =
            "0";


        NetActivityLabel.Text =
            "R 0.00";


        NetActivityLabel.TextColor =
            Color.FromArgb(
                "#1E1E1E");


        DisplayedCountLabel.Text =
            "0";


        TransactionSectionTitle.Text =
            "All Spending";


        TransactionSectionSubtitle.Text =
            "Your most recently recorded purchases appear first.";


        TransactionsLayout
            .Children
            .Clear();


        EmptyStateCard.IsVisible =
            true;


        EmptyStateLabel.Text =
            "Purchases you record in SpendWise will appear here.";
    }


    // =====================================================
    // SET LOADING
    // =====================================================

    private void SetLoading(
        bool isLoading)
    {
        _isLoading =
            isLoading;


        LoadingSection.IsVisible =
            isLoading;


        LoadingIndicator.IsRunning =
            isLoading;


        RefreshButton.IsEnabled =
            !isLoading;


        TransactionFilterPicker.IsEnabled =
            !isLoading;
    }


    // =====================================================
    // SHOW ERROR
    // =====================================================

    private void ShowError(
        string message)
    {
        ErrorLabel.Text =
            message;


        ErrorCard.IsVisible =
            true;
    }


    // =====================================================
    // HIDE ERROR
    // =====================================================

    private void HideError()
    {
        ErrorLabel.Text =
            string.Empty;


        ErrorCard.IsVisible =
            false;
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
        }
        else
        {
            Application.Current!.MainPage =
                new NavigationPage(
                    new DashboardPage());
        }
    }


    // =====================================================
    // REFRESH BUTTON
    // =====================================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadTransactionsAsync();
    }


    // =====================================================
    // FILTER CHANGED
    // =====================================================

    private void TransactionFilterPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }


        ApplyCurrentFilter();
    }
}