
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace SpendWise.App.Pages;

public partial class TransactionsPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<TransactionResponse> _allTransactions = new();

    private bool _isLoading;
    private bool _isDeleting;

    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public TransactionsPage()
    {
        InitializeComponent();

        _apiService = new ApiService();

        TransactionFilterPicker.SelectedIndex = 0;
    }

    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTransactionsAsync();
    }

    // ==========================================
    // LOAD TRANSACTIONS
    // ==========================================

    private async Task LoadTransactionsAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            var result = await _apiService.GetTransactionsAsync();

            if (!result.Success || result.Data == null)
            {
                _allTransactions.Clear();
                ClearPage();
                ShowError(result.Message);
                return;
            }

            _allTransactions = result.Data
                .Where(IsSpendingTransaction)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            DisplaySummary();
            ApplyCurrentFilter();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOAD TRANSACTIONS ERROR: {ex}");

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

    // ==========================================
    // SUMMARY
    // ==========================================

    private void DisplaySummary()
    {
        decimal totalSpent = _allTransactions
            .Sum(t => Math.Abs(t.Amount));

        int purchaseCount = _allTransactions.Count;

        decimal averagePurchase = purchaseCount > 0
            ? totalSpent / purchaseCount
            : 0m;

        MoneyOutLabel.Text = FormatMoney(totalSpent);
        TransactionCountLabel.Text = purchaseCount.ToString();
        NetActivityLabel.Text = FormatMoney(averagePurchase);

        NetActivityLabel.TextColor =
            Color.FromArgb("#1E1E1E");
    }

    // ==========================================
    // FILTERS
    // ==========================================

    private void ApplyCurrentFilter()
    {
        int selectedIndex = TransactionFilterPicker.SelectedIndex;

        IEnumerable<TransactionResponse> filtered =
            _allTransactions;

        if (selectedIndex == 1)
        {
            filtered = _allTransactions
                .Where(IsGroceryTransaction);

            TransactionSectionTitle.Text = "Groceries";
            TransactionSectionSubtitle.Text =
                "Recorded grocery purchases appear here.";
        }
        else if (selectedIndex == 2)
        {
            filtered = _allTransactions
                .Where(t => !IsGroceryTransaction(t));

            TransactionSectionTitle.Text = "Other Purchases";
            TransactionSectionSubtitle.Text =
                "Recorded non-grocery purchases appear here.";
        }
        else
        {
            TransactionSectionTitle.Text = "All Spending";
            TransactionSectionSubtitle.Text =
                "Your most recently recorded purchases appear first.";
        }

        DisplayTransactions(
            filtered
                .OrderByDescending(t => t.CreatedAt)
                .ToList());
    }

    // ==========================================
    // DISPLAY TRANSACTIONS
    // ==========================================

    private void DisplayTransactions(
        List<TransactionResponse> transactions)
    {
        TransactionsLayout.Children.Clear();

        DisplayedCountLabel.Text =
            transactions.Count.ToString();

        if (transactions.Count == 0)
        {
            EmptyStateCard.IsVisible = true;

            EmptyStateLabel.Text =
                _allTransactions.Count == 0
                    ? "Purchases you record in SpendWise will appear here."
                    : "No spending records match the selected filter.";

            return;
        }

        EmptyStateCard.IsVisible = false;

        foreach (var transaction in transactions)
        {
            TransactionsLayout.Children.Add(
                CreateTransactionCard(transaction));
        }
    }

    // ==========================================
    // CREATE TRANSACTION CARD
    // ==========================================

    private Border CreateTransactionCard(
        TransactionResponse transaction)
    {
        Color red = Color.FromArgb("#C0392B");

        var typeLabel = new Label
        {
            Text = GetDisplayType(transaction),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1E1E"),
            VerticalOptions = LayoutOptions.Center
        };

        var amountLabel = new Label
        {
            Text = FormatTransactionAmount(transaction),
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = red,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        Grid.SetColumn(typeLabel, 0);
        Grid.SetColumn(amountLabel, 1);

        headerGrid.Children.Add(typeLabel);
        headerGrid.Children.Add(amountLabel);

        var descriptionLabel = new Label
        {
            Text = GetDescription(transaction),
            FontSize = 13,
            TextColor = Color.FromArgb("#555555"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var detailsLayout = new VerticalStackLayout
        {
            Spacing = 5
        };

        var categoryLabel = CreateDetailLabel(
            "Category", transaction.Category);

        var retailerLabel = CreateDetailLabel(
            "Retailer", transaction.Retailer);

        var dateLabel = CreateDetailLabel(
            "Date", FormatDate(transaction.CreatedAt));

        var referenceLabel = CreateDetailLabel(
            "Reference", transaction.Reference);

        if (categoryLabel.IsVisible)
            detailsLayout.Children.Add(categoryLabel);

        if (retailerLabel.IsVisible)
            detailsLayout.Children.Add(retailerLabel);

        detailsLayout.Children.Add(dateLabel);

        if (referenceLabel.IsVisible)
            detailsLayout.Children.Add(referenceLabel);

        var statusBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#FFF1F0"),
            Stroke = red,
            StrokeThickness = 1,
            Padding = new Thickness(10, 5),
            HorizontalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(20)
            },
            Content = new Label
            {
                Text = "Recorded Spending",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = red
            }
        };

        var deleteButton = new Button
        {
            Text = "Delete Purchase",
            BackgroundColor = Color.FromArgb("#FFF1F0"),
            TextColor = Color.FromArgb("#B42318"),
            BorderColor = Color.FromArgb("#E6B8B4"),
            BorderWidth = 1,
            CornerRadius = 10,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.End,
            CommandParameter = transaction.Id,
            IsEnabled = !_isDeleting && transaction.Id > 0,
            IsVisible = string.Equals(
                transaction.Type,
                "Purchase",
                StringComparison.OrdinalIgnoreCase)
        };

        deleteButton.Clicked += DeletePurchaseButton_Clicked;

        var content = new VerticalStackLayout
        {
            Spacing = 10
        };

        content.Children.Add(headerGrid);
        content.Children.Add(statusBorder);
        content.Children.Add(descriptionLabel);
        content.Children.Add(detailsLayout);
        content.Children.Add(deleteButton);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5E5E5"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(14)
            },
            Content = content
        };
    }

    // ==========================================
    // DELETE PURCHASE
    // ==========================================

    private async void DeletePurchaseButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (_isDeleting || _isLoading)
            return;

        if (sender is not Button button ||
            button.CommandParameter == null)
            return;

        if (!int.TryParse(
                button.CommandParameter.ToString(),
                out int transactionId) ||
            transactionId <= 0)
        {
            await DisplayAlert(
                "Invalid Transaction",
                "This purchase does not have a valid transaction ID.",
                "OK");
            return;
        }

        var selectedTransaction = _allTransactions
            .FirstOrDefault(t => t.Id == transactionId);

        if (selectedTransaction == null)
        {
            await DisplayAlert(
                "Purchase Not Found",
                "Refresh the spending history and try again.",
                "OK");
            return;
        }

        if (!string.Equals(
            selectedTransaction.Type,
            "Purchase",
            StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert(
                "Cannot Delete",
                "Only purchase transactions can be deleted here.",
                "OK");
            return;
        }

        string description = GetDescription(selectedTransaction);

        bool confirmed = await DisplayAlert(
            "Delete Purchase",
            $"Delete \"{description}\" for " +
            $"{FormatMoney(Math.Abs(selectedTransaction.Amount))}?\n\n" +
            "This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        _isDeleting = true;
        button.IsEnabled = false;

        try
        {
            var result =
                await _apiService.DeletePurchaseAsync(transactionId);

            if (!result.Success)
            {
                await DisplayAlert(
                    "Delete Failed",
                    result.Message,
                    "OK");
                return;
            }

            await LoadTransactionsAsync();

            if (ErrorCard.IsVisible)
            {
                await DisplayAlert(
                    "Purchase Deleted",
                    "The purchase was deleted, but the spending history could not be refreshed. Please refresh the page.",
                    "OK");
                return;
            }

            await DisplayAlert(
                "Purchase Deleted",
                "The purchase was deleted. Check your budget totals to confirm they updated correctly.",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DELETE PURCHASE ERROR: {ex}");

            await DisplayAlert(
                "Connection Error",
                ex.Message,
                "OK");
        }
        finally
        {
            _isDeleting = false;
            button.IsEnabled = true;
        }
    }

    // ==========================================
    // TRANSACTION HELPERS
    // ==========================================

    private string GetDisplayType(
        TransactionResponse transaction)
    {
        return IsGroceryTransaction(transaction)
            ? "Grocery Purchase"
            : "Purchase";
    }

    private Label CreateDetailLabel(
        string title,
        string? value)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(value);

        return new Label
        {
            Text = hasValue
                ? $"{title}: {value}"
                : string.Empty,
            FontSize = 11,
            TextColor = Color.FromArgb("#777777"),
            IsVisible = hasValue,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private string GetDescription(
        TransactionResponse transaction)
    {
        if (!string.IsNullOrWhiteSpace(transaction.Description))
            return transaction.Description;

        if (!string.IsNullOrWhiteSpace(transaction.Retailer))
            return transaction.Retailer;

        if (!string.IsNullOrWhiteSpace(transaction.Category))
            return transaction.Category;

        return "Recorded purchase";
    }

    private bool IsSpendingTransaction(
        TransactionResponse transaction)
    {
        if (string.Equals(
            transaction.Type,
            "Deposit",
            StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(
            transaction.Type,
            "Credit",
            StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(
            transaction.Type,
            "Refund",
            StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(
                   transaction.Type,
                   "Purchase",
                   StringComparison.OrdinalIgnoreCase)
               || transaction.Amount < 0;
    }

    private bool IsGroceryTransaction(
        TransactionResponse transaction)
    {
        if (!string.IsNullOrWhiteSpace(transaction.Category) &&
            transaction.Category.Equals(
                "Groceries",
                StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(transaction.Description) &&
            transaction.Description.Contains(
                "grocery",
                StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private string FormatTransactionAmount(
        TransactionResponse transaction)
    {
        return $"-{FormatMoney(Math.Abs(transaction.Amount))}";
    }

    private string FormatMoney(decimal amount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "R {0:N2}",
            amount);
    }

    private string FormatDate(DateTime date)
    {
        return date.ToLocalTime()
            .ToString("dd MMM yyyy, HH:mm");
    }

    // ==========================================
    // CLEAR PAGE
    // ==========================================

    private void ClearPage()
    {
        MoneyOutLabel.Text = "R 0.00";
        TransactionCountLabel.Text = "0";
        NetActivityLabel.Text = "R 0.00";

        NetActivityLabel.TextColor =
            Color.FromArgb("#1E1E1E");

        DisplayedCountLabel.Text = "0";

        TransactionSectionTitle.Text = "All Spending";

        TransactionSectionSubtitle.Text =
            "Your most recently recorded purchases appear first.";

        TransactionsLayout.Children.Clear();

        EmptyStateCard.IsVisible = true;

        EmptyStateLabel.Text =
            "Purchases you record in SpendWise will appear here.";
    }

    // ==========================================
    // LOADING AND ERRORS
    // ==========================================

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        LoadingSection.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        RefreshButton.IsEnabled = !isLoading;

        TransactionFilterPicker.IsEnabled = !isLoading;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorCard.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.Text = string.Empty;
        ErrorCard.IsVisible = false;
    }

    // ==========================================
    // NAVIGATION
    // ==========================================

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else
        {
            Application.Current!.MainPage =
                new NavigationPage(new DashboardPage());
        }
    }

    private async void RefreshButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await LoadTransactionsAsync();
    }

    private void TransactionFilterPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        ApplyCurrentFilter();
    }
}
