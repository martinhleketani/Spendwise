
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class ReportsPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isLoading;

    private readonly List<string> _months = new()
    {
        "January", "February", "March", "April",
        "May", "June", "July", "August",
        "September", "October", "November", "December"
    };

    public ReportsPage()
    {
        InitializeComponent();

        _apiService = new ApiService();

        SetupPickers();
        SetupDatePickers();
    }

    // =====================================================
    // PAGE SETUP
    // =====================================================

    private void SetupPickers()
    {
        DateTime today = DateTime.Today;

        MonthPicker.ItemsSource = _months;
        MonthPicker.SelectedIndex = today.Month - 1;

        YearEntry.Text = today.Year.ToString(
            CultureInfo.InvariantCulture);
    }

    private void SetupDatePickers()
    {
        DateTime today = DateTime.Today;

        StartDatePicker.Date =
            new DateTime(today.Year, today.Month, 1);

        EndDatePicker.Date = today;

        StartDatePicker.MaximumDate = today;
        EndDatePicker.MaximumDate = today;
    }

    private int GetSelectedMonth()
    {
        return MonthPicker.SelectedIndex < 0
            ? DateTime.Today.Month
            : MonthPicker.SelectedIndex + 1;
    }

    private bool TryGetSelectedYear(out int year)
    {
        year = 0;

        string enteredYear = YearEntry.Text?.Trim() ?? "";

        if (!int.TryParse(
            enteredYear,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out year))
        {
            return false;
        }

        return year >= 2020 && year <= 2100;
    }

    private bool ValidateMonthlySelection(out int year, out int month)
    {
        year = 0;
        month = 0;

        if (!TryGetSelectedYear(out year))
        {
            ShowError(
                "Please enter a valid year between 2020 and 2100.");

            return false;
        }

        if (MonthPicker.SelectedIndex < 0)
        {
            ShowError("Please select a month.");
            return false;
        }

        month = GetSelectedMonth();

        return true;
    }

    // =====================================================
    // MONTHLY REPORT
    // =====================================================

    private async Task LoadMonthlyReportAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            MonthlyReportSection.IsVisible = false;

            if (!ValidateMonthlySelection(out int year, out int month))
                return;

            var result =
                await _apiService.GetMonthlyReportAsync(year, month);

            if (!result.Success || result.Data == null)
            {
                ShowError(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Unable to retrieve the monthly report."
                        : result.Message);

                return;
            }

            DisplayMonthlyReport(result.Data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"MONTHLY REPORT ERROR: {ex}");

            ShowError(
                $"Unable to load monthly report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void DisplayMonthlyReport(MonthlyReportResponse report)
    {
        MonthlyReportSection.IsVisible = true;

        MonthlyPeriodLabel.Text =
            $"{GetMonthName(report.Month)} {report.Year}";

        TotalSpentLabel.Text =
            FormatMoney(report.Summary.TotalSpent);

        TransactionCountTextLabel.Text =
            FormatPurchaseCount(report.Summary.TransactionCount);

        AveragePurchaseLabel.Text =
            FormatMoney(report.Summary.AveragePurchase);

        BiggestPurchaseLabel.Text =
            FormatMoney(report.Summary.BiggestPurchase);

        DisplayCategories(
            CategoryReportLayout,
            report.Categories,
            NoCategoryDataLabel);

        DisplayBiggestTransaction(report.BiggestTransaction);
    }

    private void DisplayBiggestTransaction(
        BiggestTransactionResponse? transaction)
    {
        if (transaction == null)
        {
            BiggestTransactionCard.IsVisible = false;
            return;
        }

        BiggestTransactionCard.IsVisible = true;

        BiggestTransactionDescriptionLabel.Text =
            string.IsNullOrWhiteSpace(transaction.Description)
                ? "Purchase"
                : transaction.Description;

        BiggestTransactionCategoryLabel.Text =
            string.IsNullOrWhiteSpace(transaction.Category)
                ? "Category: Not specified"
                : $"Category: {transaction.Category}";

        BiggestTransactionRetailerLabel.Text =
            string.IsNullOrWhiteSpace(transaction.Retailer)
                ? "Retailer: Not specified"
                : $"Retailer: {transaction.Retailer}";

        BiggestTransactionDateLabel.Text =
            transaction.CreatedAt.ToString(
                "dd MMM yyyy • HH:mm");

        BiggestTransactionAmountLabel.Text =
            FormatMoney(transaction.Amount);
    }

    // =====================================================
    // RETAILER SPENDING REPORT
    // =====================================================

    private async Task LoadRetailerReportAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            RetailerReportSection.IsVisible = false;

            if (!ValidateMonthlySelection(out int year, out int month))
                return;

            var result =
                await _apiService.GetRetailerReportAsync(year, month);

            if (!result.Success || result.Data == null)
            {
                ShowError(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Unable to retrieve the retailer report."
                        : result.Message);

                return;
            }

            DisplayRetailerReport(result.Data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"RETAILER REPORT ERROR: {ex}");

            ShowError(
                $"Unable to load retailer report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void DisplayRetailerReport(RetailerReportResponse report)
    {
        RetailerReportSection.IsVisible = true;

        RetailerTotalSpentLabel.Text =
            FormatMoney(report.Summary.TotalSpentAtRetailers);

        RetailerCountLabel.Text =
            report.Summary.RetailerCount.ToString();

        RetailerTransactionCountLabel.Text =
            report.Summary.TransactionCount.ToString();

        AverageRetailerPurchaseLabel.Text =
            FormatMoney(report.Summary.AverageRetailerPurchase);

        DisplayRetailers(
            RetailerReportLayout,
            report.Retailers,
            NoRetailerDataLabel);

        DisplayHighestRetailer(report.HighestSpendingRetailer);
    }

    private void DisplayHighestRetailer(
        ReportRetailerItem? retailer)
    {
        if (retailer == null)
        {
            HighestRetailerCard.IsVisible = false;
            return;
        }

        HighestRetailerCard.IsVisible = true;

        HighestRetailerNameLabel.Text =
            string.IsNullOrWhiteSpace(retailer.Retailer)
                ? "Unknown retailer"
                : retailer.Retailer;

        HighestRetailerAmountLabel.Text =
            FormatMoney(retailer.Amount);
    }

    // =====================================================
    // DATE RANGE REPORT
    // =====================================================

    private async Task LoadDateRangeReportAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            DateRangeReportSection.IsVisible = false;

            DateTime startDate = StartDatePicker.Date;
            DateTime endDate = EndDatePicker.Date;

            if (startDate.Date > endDate.Date)
            {
                ShowError("Start date cannot be after end date.");
                return;
            }

            var result =
                await _apiService.GetDateRangeReportAsync(
                    startDate,
                    endDate);

            if (!result.Success || result.Data == null)
            {
                ShowError(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Unable to retrieve the date range report."
                        : result.Message);

                return;
            }

            DisplayDateRangeReport(result.Data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"DATE RANGE REPORT ERROR: {ex}");

            ShowError(
                $"Unable to load date range report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void DisplayDateRangeReport(
        DateRangeReportResponse report)
    {
        DateRangeReportSection.IsVisible = true;

        DateRangePeriodLabel.Text =
            $"{report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy}";

        DateRangeTotalSpentLabel.Text =
            FormatMoney(report.Summary.TotalSpent);

        DateRangeTransactionCountLabel.Text =
            FormatPurchaseCount(report.Summary.TransactionCount);

        DateRangeAverageLabel.Text =
            FormatMoney(report.Summary.AveragePurchase);

        DateRangeBiggestLabel.Text =
            FormatMoney(report.Summary.BiggestPurchase);

        DisplayCategories(
            DateRangeCategoryLayout,
            report.Categories,
            NoDateRangeCategoryLabel);

        DisplayRetailers(
            DateRangeRetailerLayout,
            report.Retailers,
            NoDateRangeRetailerLabel);

        DisplayDateRangeTransactions(report.Transactions);
    }

    // =====================================================
    // CATEGORY DISPLAY
    // =====================================================

    private void DisplayCategories(
        VerticalStackLayout layout,
        List<ReportCategoryItem>? categories,
        Label emptyLabel)
    {
        layout.Children.Clear();

        if (categories == null || categories.Count == 0)
        {
            emptyLabel.IsVisible = true;
            return;
        }

        emptyLabel.IsVisible = false;

        foreach (ReportCategoryItem category in categories)
        {
            layout.Children.Add(CreateCategoryCard(category));
        }
    }

    private Border CreateCategoryCard(
        ReportCategoryItem category)
    {
        Label categoryNameLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(category.Category)
                ? "Uncategorised"
                : category.Category,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#173C2A")
        };

        Label transactionLabel = new()
        {
            Text = FormatPurchaseCount(category.TransactionCount),
            FontSize = 11,
            TextColor = Color.FromArgb("#718078")
        };

        VerticalStackLayout leftLayout = new()
        {
            Spacing = 2,
            Children =
            {
                categoryNameLabel,
                transactionLabel
            }
        };

        Label amountLabel = new()
        {
            Text = FormatMoney(category.Amount),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#146B3A"),
            HorizontalTextAlignment = TextAlignment.End
        };

        Label percentageLabel = new()
        {
            Text = $"{category.Percentage:0.##}%",
            FontSize = 11,
            TextColor = Color.FromArgb("#718078"),
            HorizontalTextAlignment = TextAlignment.End
        };

        VerticalStackLayout rightLayout = new()
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                amountLabel,
                percentageLabel
            }
        };

        Grid contentGrid = new()
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            }
        };

        Grid.SetColumn(leftLayout, 0);
        Grid.SetColumn(rightLayout, 1);

        contentGrid.Children.Add(leftLayout);
        contentGrid.Children.Add(rightLayout);

        return CreateReportBorder(contentGrid);
    }

    // =====================================================
    // RETAILER DISPLAY
    // =====================================================

    private void DisplayRetailers(
        VerticalStackLayout layout,
        List<ReportRetailerItem>? retailers,
        Label emptyLabel)
    {
        layout.Children.Clear();

        if (retailers == null || retailers.Count == 0)
        {
            emptyLabel.IsVisible = true;
            return;
        }

        emptyLabel.IsVisible = false;

        foreach (ReportRetailerItem retailer in retailers)
        {
            layout.Children.Add(CreateRetailerCard(retailer));
        }
    }

    private Border CreateRetailerCard(
        ReportRetailerItem retailer)
    {
        Label retailerNameLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(retailer.Retailer)
                ? "Unknown retailer"
                : retailer.Retailer,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#173C2A")
        };

        Label transactionLabel = new()
        {
            Text = FormatPurchaseCount(retailer.TransactionCount),
            FontSize = 11,
            TextColor = Color.FromArgb("#718078")
        };

        VerticalStackLayout leftLayout = new()
        {
            Spacing = 2,
            Children =
            {
                retailerNameLabel,
                transactionLabel
            }
        };

        Label amountLabel = new()
        {
            Text = FormatMoney(retailer.Amount),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#146B3A"),
            HorizontalTextAlignment = TextAlignment.End
        };

        Label percentageLabel = new()
        {
            Text = $"{retailer.Percentage:0.##}%",
            FontSize = 11,
            TextColor = Color.FromArgb("#718078"),
            HorizontalTextAlignment = TextAlignment.End
        };

        VerticalStackLayout rightLayout = new()
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                amountLabel,
                percentageLabel
            }
        };

        Grid contentGrid = new()
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            }
        };

        Grid.SetColumn(leftLayout, 0);
        Grid.SetColumn(rightLayout, 1);

        contentGrid.Children.Add(leftLayout);
        contentGrid.Children.Add(rightLayout);

        return CreateReportBorder(contentGrid);
    }

    // =====================================================
    // TRANSACTION DISPLAY
    // =====================================================

    private void DisplayDateRangeTransactions(
        List<ReportTransactionItem>? transactions)
    {
        DateRangeTransactionLayout.Children.Clear();

        if (transactions == null || transactions.Count == 0)
        {
            NoDateRangeTransactionsLabel.IsVisible = true;
            return;
        }

        NoDateRangeTransactionsLabel.IsVisible = false;

        foreach (ReportTransactionItem transaction in transactions)
        {
            DateRangeTransactionLayout.Children.Add(
                CreateTransactionCard(transaction));
        }
    }

    private Border CreateTransactionCard(
        ReportTransactionItem transaction)
    {
        string description =
            string.IsNullOrWhiteSpace(transaction.Description)
                ? "Purchase"
                : transaction.Description;

        string category =
            string.IsNullOrWhiteSpace(transaction.Category)
                ? "Uncategorised"
                : transaction.Category;

        string retailer =
            string.IsNullOrWhiteSpace(transaction.Retailer)
                ? "No retailer"
                : transaction.Retailer;

        Label descriptionLabel = new()
        {
            Text = description,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#173C2A"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        Label detailsLabel = new()
        {
            Text = $"{category} • {retailer}",
            FontSize = 11,
            TextColor = Color.FromArgb("#718078"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        Label dateLabel = new()
        {
            Text = transaction.CreatedAt.ToString(
                "dd MMM yyyy • HH:mm"),
            FontSize = 10,
            TextColor = Color.FromArgb("#8A968F")
        };

        VerticalStackLayout leftLayout = new()
        {
            Spacing = 3,
            Children =
            {
                descriptionLabel,
                detailsLabel,
                dateLabel
            }
        };

        Label amountLabel = new()
        {
            Text = FormatMoney(transaction.Amount),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#146B3A"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.End
        };

        Grid contentGrid = new()
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            }
        };

        Grid.SetColumn(leftLayout, 0);
        Grid.SetColumn(amountLabel, 1);

        contentGrid.Children.Add(leftLayout);
        contentGrid.Children.Add(amountLabel);

        return CreateReportBorder(contentGrid);
    }

    // =====================================================
    // SHARED REPORT CARD
    // =====================================================

    private static Border CreateReportBorder(View content)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAF9"),
            Stroke = Color.FromArgb("#E1E8E4"),
            StrokeThickness = 1,
            Padding = new Thickness(12),
            Content = content,
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(12)
                }
        };
    }

    // =====================================================
    // LOADING AND ERROR HANDLING
    // =====================================================

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        LoadMonthlyReportButton.IsEnabled = !isLoading;
        LoadRetailerReportButton.IsEnabled = !isLoading;
        LoadDateRangeReportButton.IsEnabled = !isLoading;

        MonthPicker.IsEnabled = !isLoading;
        YearEntry.IsEnabled = !isLoading;

        StartDatePicker.IsEnabled = !isLoading;
        EndDatePicker.IsEnabled = !isLoading;
    }

    private void ShowError(string? message)
    {
        ErrorLabel.Text =
            string.IsNullOrWhiteSpace(message)
                ? "Something went wrong."
                : message;

        ErrorCard.IsVisible = true;
    }

    private void HideError()
    {
        ErrorCard.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    // =====================================================
    // FORMATTING
    // =====================================================

    private static string FormatMoney(decimal amount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "R{0:N2}",
            amount);
    }

    private static string FormatPurchaseCount(int count)
    {
        return count == 1
            ? "1 purchase"
            : $"{count} purchases";
    }

    private static string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
            return "Unknown Month";

        return CultureInfo.InvariantCulture
            .DateTimeFormat
            .GetMonthName(month);
    }

    // =====================================================
    // BUTTON EVENTS
    // =====================================================

    private async void BackButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    private async void RefreshButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            bool hasError = false;

            // Monthly report
            if (ValidateMonthlySelection(out int year, out int month))
            {
                var monthly =
                    await _apiService.GetMonthlyReportAsync(year, month);

                if (monthly.Success && monthly.Data != null)
                    DisplayMonthlyReport(monthly.Data);
                else
                {
                    MonthlyReportSection.IsVisible = false;
                    hasError = true;
                }

                // Retailer report
                var retailer =
                    await _apiService.GetRetailerReportAsync(year, month);

                if (retailer.Success && retailer.Data != null)
                    DisplayRetailerReport(retailer.Data);
                else
                {
                    RetailerReportSection.IsVisible = false;
                    hasError = true;
                }
            }
            else
            {
                MonthlyReportSection.IsVisible = false;
                RetailerReportSection.IsVisible = false;
                hasError = true;
            }

            // Date range report
            if (StartDatePicker.Date.Date <= EndDatePicker.Date.Date)
            {
                var dateRange =
                    await _apiService.GetDateRangeReportAsync(
                        StartDatePicker.Date,
                        EndDatePicker.Date);

                if (dateRange.Success && dateRange.Data != null)
                    DisplayDateRangeReport(dateRange.Data);
                else
                {
                    DateRangeReportSection.IsVisible = false;
                    hasError = true;
                }
            }
            else
            {
                DateRangeReportSection.IsVisible = false;
                hasError = true;
            }

            if (hasError)
            {
                ShowError(
                    "Some reports could not be refreshed. Please check your selections and try again.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"REPORT REFRESH ERROR: {ex}");

            ShowError(
                $"Unable to refresh reports: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void LoadMonthlyReportButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await LoadMonthlyReportAsync();
    }

    private async void LoadRetailerReportButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await LoadRetailerReportAsync();
    }

    private async void LoadDateRangeReportButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await LoadDateRangeReportAsync();
    }
}
