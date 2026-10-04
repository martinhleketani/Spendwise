using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class PurchasePage : ContentPage
{
    private readonly ApiService _apiService;

    private BudgetResponse? _currentBudget;

    private bool _isProcessingPurchase;


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public PurchasePage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        CategoryPicker.SelectedIndexChanged +=
            CategoryPicker_SelectedIndexChanged;
    }


    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPageAsync();
    }


    // ==========================================
    // LOAD PAGE
    // ==========================================

    private async Task LoadPageAsync()
    {
        HideError();

        await LoadBudgetAsync();

        await LoadPurchaseHistoryAsync();
    }


    // ==========================================
    // LOAD CURRENT MONTH BUDGET
    // ==========================================

    private async Task LoadBudgetAsync()
    {
        DateTime today =
            DateTime.Now;


        var result =
            await _apiService.GetBudgetAsync(
                today.Year,
                today.Month);


        CategoryPicker.Items.Clear();

        _currentBudget =
            null;


        if (!result.Success ||
            result.Data == null)
        {
            CategoryBudgetLabel.Text =
                "No budget is available for this month.";

            ShowError(
                result.Message);

            return;
        }


        _currentBudget =
            result.Data;


        foreach (BudgetCategoryResponse category
                 in _currentBudget.Categories)
        {
            CategoryPicker.Items.Add(
                category.Category);
        }


        if (CategoryPicker.Items.Count > 0)
        {
            CategoryPicker.SelectedIndex =
                0;
        }
        else
        {
            CategoryBudgetLabel.Text =
                "No budget categories are available.";
        }
    }


    // ==========================================
    // CATEGORY CHANGED
    // ==========================================

    private void CategoryPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        UpdateCategoryBudgetInformation();
    }


    // ==========================================
    // UPDATE CATEGORY BUDGET INFORMATION
    // ==========================================

    private void UpdateCategoryBudgetInformation()
    {
        if (_currentBudget == null ||
            CategoryPicker.SelectedIndex < 0)
        {
            CategoryBudgetLabel.Text =
                "Select a category to see its budget.";

            return;
        }


        string categoryName =
            CategoryPicker.SelectedItem
                ?.ToString()
            ?? string.Empty;


        BudgetCategoryResponse? category =
            _currentBudget.Categories
                .FirstOrDefault(
                    x =>
                        x.Category.Equals(
                            categoryName,
                            StringComparison.OrdinalIgnoreCase));


        if (category == null)
        {
            CategoryBudgetLabel.Text =
                "Budget information unavailable.";

            return;
        }


        CategoryBudgetLabel.Text =
            $"Budget: R {category.Limit:N2}   |   " +
            $"Spent: R {category.Spent:N2}   |   " +
            $"Remaining: R {category.Remaining:N2}";
    }


    // ==========================================
    // RECORD PURCHASE
    // ==========================================

    private async void PurchaseButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isProcessingPurchase)
        {
            return;
        }


        HideError();


        // --------------------------------------
        // CATEGORY
        // --------------------------------------

        string category =
            CategoryPicker.SelectedItem
                ?.ToString()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            category))
        {
            ShowError(
                "Please select a budget category.");

            return;
        }


        // --------------------------------------
        // DESCRIPTION
        // --------------------------------------

        string description =
            DescriptionEntry.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            description))
        {
            ShowError(
                "Please enter a purchase description.");

            return;
        }


        // --------------------------------------
        // AMOUNT
        // --------------------------------------

        decimal amount =
            GetDecimal(
                AmountEntry.Text);


        if (amount <= 0)
        {
            ShowError(
                "Purchase amount must be greater than R0.");

            return;
        }


        DateTime today =
            DateTime.Now;


        // --------------------------------------
        // FIND SELECTED BUDGET CATEGORY
        // --------------------------------------

        BudgetCategoryResponse? selectedCategory =
            _currentBudget?.Categories
                .FirstOrDefault(
                    x =>
                        x.Category.Equals(
                            category,
                            StringComparison.OrdinalIgnoreCase));


        // --------------------------------------
        // CONFIRMATION MESSAGE
        // --------------------------------------

        string confirmationMessage =
            $"Category: {category}\n" +
            $"Description: {description}\n" +
            $"Amount Spent: R {amount:N2}";


        if (selectedCategory != null)
        {
            confirmationMessage +=
                $"\n\nCategory Remaining: " +
                $"R {selectedCategory.Remaining:N2}";


            if (amount >
                selectedCategory.Remaining)
            {
                confirmationMessage +=
                    "\n\nWarning: recording this purchase " +
                    "will take this category over budget.";
            }
        }


        confirmationMessage +=
            "\n\nSpendWise will record this spending " +
            "and update your budget category." +
            "\n\nNo bank or card payment will be processed.";


        bool confirmed =
            await DisplayAlert(
                "Record Purchase",
                confirmationMessage,
                "Record Purchase",
                "Cancel");


        if (!confirmed)
        {
            return;
        }


        // --------------------------------------
        // UNIQUE REQUEST ID
        // --------------------------------------

        string requestId =
            $"PURCHASE-{Guid.NewGuid():N}";


        CreatePurchaseRequest request =
            new CreatePurchaseRequest
            {
                Category =
                    category,

                Description =
                    description,

                Amount =
                    amount,

                Month =
                    today.Month,

                Year =
                    today.Year,

                RequestId =
                    requestId
            };


        try
        {
            _isProcessingPurchase =
                true;


            PurchaseButton.IsEnabled =
                false;

            PurchaseButton.Text =
                "Recording...";


            // ----------------------------------
            // SEND SPENDING RECORD TO API
            // ----------------------------------

            var result =
                await _apiService
                    .CreatePurchaseAsync(
                        request);


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                return;
            }


            CreatePurchaseResponse data =
                result.Data;


            // ----------------------------------
            // DUPLICATE REQUEST
            // ----------------------------------

            if (data.Duplicate)
            {
                await DisplayAlert(
                    "Already Recorded",
                    string.IsNullOrWhiteSpace(
                        data.Message)
                        ? "This purchase has already been recorded."
                        : data.Message,
                    "OK");
            }
            else
            {
                // ----------------------------------
                // SUCCESS
                // ----------------------------------

                string budgetStatus =
                    data.WithinBudget
                        ? "Within budget"
                        : "Over budget";


                string successMessage =
                    $"Amount Recorded: R {data.PurchaseAmount:N2}\n\n" +
                    $"Category: {data.Category}\n" +
                    $"Category Spent: R {data.CategorySpent:N2}\n" +
                    $"Category Remaining: R {data.CategoryRemaining:N2}\n" +
                    $"Budget Status: {budgetStatus}\n\n" +
                    $"Reference: {data.TransactionReference}\n\n" +
                    "No bank or card payment was processed by SpendWise.";


                await DisplayAlert(
                    "Purchase Recorded",
                    successMessage,
                    "OK");
            }


            // ----------------------------------
            // CLEAR INPUTS
            // ----------------------------------

            DescriptionEntry.Text =
                string.Empty;

            AmountEntry.Text =
                string.Empty;


            // ----------------------------------
            // REFRESH BUDGET + HISTORY
            // ----------------------------------

            await LoadBudgetAsync();

            await LoadPurchaseHistoryAsync();
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to record purchase: {ex.Message}");
        }
        finally
        {
            _isProcessingPurchase =
                false;

            PurchaseButton.IsEnabled =
                true;

            PurchaseButton.Text =
                "Record Purchase";
        }
    }


    // ==========================================
    // LOAD PURCHASE HISTORY
    // ==========================================

    private async Task LoadPurchaseHistoryAsync(
        string? search = null)
    {
        HistoryLoadingIndicator.IsVisible =
            true;

        HistoryLoadingIndicator.IsRunning =
            true;


        try
        {
            DateTime today =
                DateTime.Now;


            var result =
                await _apiService
                    .GetPurchasesAsync(
                        search: search,
                        month: today.Month,
                        year: today.Year);


            PurchaseHistoryLayout.Children.Clear();


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                ShowNoPurchases();

                return;
            }


            PurchaseHistoryResponse history =
                result.Data;


            HistorySummaryLabel.Text =
                $"{history.Count} purchase(s) • " +
                $"R {history.TotalAmount:N2}";


            if (history.Purchases == null ||
                history.Purchases.Count == 0)
            {
                ShowNoPurchases();

                return;
            }


            foreach (PurchaseHistoryItem purchase
                     in history.Purchases)
            {
                PurchaseHistoryLayout.Children.Add(
                    CreatePurchaseHistoryCard(
                        purchase));
            }
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to load purchases: {ex.Message}");

            ShowNoPurchases();
        }
        finally
        {
            HistoryLoadingIndicator.IsRunning =
                false;

            HistoryLoadingIndicator.IsVisible =
                false;
        }
    }


    // ==========================================
    // CREATE PURCHASE HISTORY CARD
    // ==========================================

    private View CreatePurchaseHistoryCard(
        PurchaseHistoryItem purchase)
    {
        Border card =
            new Border
            {
                BackgroundColor =
                    Colors.White,

                Stroke =
                    Color.FromArgb(
                        "#E1E5E3"),

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(16),

                StrokeShape =
                    new Microsoft.Maui
                        .Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            14
                    }
            };


        Grid grid =
            new Grid
            {
                ColumnSpacing =
                    12
            };


        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Star
            });


        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });


        // --------------------------------------
        // LEFT SIDE
        // --------------------------------------

        VerticalStackLayout details =
            new VerticalStackLayout
            {
                Spacing =
                    4
            };


        Label descriptionLabel =
            new Label
            {
                Text =
                    purchase.Description,

                FontSize =
                    15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        Label categoryLabel =
            new Label
            {
                Text =
                    purchase.Category
                    ?? "Uncategorised",

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb(
                        "#146B3A")
            };


        Label dateLabel =
            new Label
            {
                Text =
                    purchase.CreatedAt
                        .ToLocalTime()
                        .ToString(
                            "dd MMM yyyy HH:mm"),

                FontSize =
                    11,

                TextColor =
                    Color.FromArgb(
                        "#888888")
            };


        Label referenceLabel =
            new Label
            {
                Text =
                    purchase.Reference,

                FontSize =
                    10,

                TextColor =
                    Color.FromArgb(
                        "#999999")
            };


        details.Children.Add(
            descriptionLabel);

        details.Children.Add(
            categoryLabel);

        details.Children.Add(
            dateLabel);

        details.Children.Add(
            referenceLabel);


        // --------------------------------------
        // RIGHT SIDE - AMOUNT
        // --------------------------------------

        Label amountLabel =
            new Label
            {
                Text =
                    $"- R {purchase.Amount:N2}",

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#C0392B"),

                VerticalOptions =
                    LayoutOptions.Center,

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        Grid.SetColumn(
            details,
            0);

        Grid.SetColumn(
            amountLabel,
            1);


        grid.Children.Add(
            details);

        grid.Children.Add(
            amountLabel);


        card.Content =
            grid;


        return card;
    }


    // ==========================================
    // SHOW NO PURCHASES
    // ==========================================

    private void ShowNoPurchases()
    {
        PurchaseHistoryLayout
            .Children
            .Clear();


        Border card =
            new Border
            {
                BackgroundColor =
                    Colors.White,

                Stroke =
                    Color.FromArgb(
                        "#E1E5E3"),

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(20),

                StrokeShape =
                    new Microsoft.Maui
                        .Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            14
                    }
            };


        card.Content =
            new Label
            {
                Text =
                    "No purchases found.",

                TextColor =
                    Color.FromArgb(
                        "#777777"),

                HorizontalOptions =
                    LayoutOptions.Center,

                HorizontalTextAlignment =
                    TextAlignment.Center
            };


        PurchaseHistoryLayout
            .Children
            .Add(card);
    }


    // ==========================================
    // SEARCH BUTTON
    // ==========================================

    private async void SearchButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        string? search =
            SearchEntry.Text
                ?.Trim();


        await LoadPurchaseHistoryAsync(
            search);
    }


    // ==========================================
    // ENTER KEY SEARCH
    // ==========================================

    private async void SearchEntry_Completed(
        object sender,
        EventArgs e)
    {
        HideError();


        string? search =
            SearchEntry.Text
                ?.Trim();


        await LoadPurchaseHistoryAsync(
            search);
    }


    // ==========================================
    // HISTORY REFRESH
    // ==========================================

    private async void HistoryRefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        SearchEntry.Text =
            string.Empty;


        await LoadPurchaseHistoryAsync();
    }


    // ==========================================
    // FULL PAGE REFRESH
    // ==========================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadPageAsync();
    }


    // ==========================================
    // CONVERT TEXT TO DECIMAL
    // ==========================================

    private decimal GetDecimal(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return 0;
        }


        string cleaned =
            value
                .Trim()
                .Replace(
                    "R",
                    "",
                    StringComparison.OrdinalIgnoreCase)
                .Trim();


        if (decimal.TryParse(
            cleaned,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out decimal amount))
        {
            return amount;
        }


        if (decimal.TryParse(
            cleaned,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount))
        {
            return amount;
        }


        return 0;
    }


    // ==========================================
    // SHOW ERROR
    // ==========================================

    private void ShowError(
        string message)
    {
        ErrorLabel.Text =
            message;

        ErrorLabel.IsVisible =
            true;
    }


    // ==========================================
    // HIDE ERROR
    // ==========================================

    private void HideError()
    {
        ErrorLabel.Text =
            string.Empty;

        ErrorLabel.IsVisible =
            false;
    }


    // ==========================================
    // BACK BUTTON
    // ==========================================

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
}