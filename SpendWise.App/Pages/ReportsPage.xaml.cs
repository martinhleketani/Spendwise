using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class ReportsPage : ContentPage
{
    private readonly ApiService _apiService;

    private bool _isLoading;

    private readonly List<string> _months =
        new()
        {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReportsPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        SetupPickers();

        SetupDatePickers();
    }


    // =====================================================
    // PAGE APPEARING
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadMonthlyReportAsync();
    }


    // =====================================================
    // SETUP MONTH / YEAR PICKERS
    // =====================================================

    private void SetupPickers()
    {
        MonthPicker.ItemsSource =
            _months;


        int currentMonth =
            DateTime.Now.Month;

        int currentYear =
            DateTime.Now.Year;


        MonthPicker.SelectedIndex =
            currentMonth - 1;


        List<int> years =
            new List<int>();


        for (int year = currentYear;
             year >= 2020;
             year--)
        {
            years.Add(year);
        }


        YearPicker.ItemsSource =
            years;


        if (years.Count > 0)
        {
            YearPicker.SelectedIndex = 0;
        }
    }


    // =====================================================
    // SETUP DATE PICKERS
    // =====================================================

    private void SetupDatePickers()
    {
        DateTime today =
            DateTime.Today;


        DateTime firstDayOfMonth =
            new DateTime(
                today.Year,
                today.Month,
                1);


        StartDatePicker.Date =
            firstDayOfMonth;


        EndDatePicker.Date =
            today;


        StartDatePicker.MaximumDate =
            today;


        EndDatePicker.MaximumDate =
            today;
    }


    // =====================================================
    // GET SELECTED MONTH
    // =====================================================

    private int GetSelectedMonth()
    {
        if (MonthPicker.SelectedIndex < 0)
        {
            return DateTime.Now.Month;
        }


        return MonthPicker.SelectedIndex + 1;
    }


    // =====================================================
    // GET SELECTED YEAR
    // =====================================================

    private int GetSelectedYear()
    {
        if (YearPicker.SelectedItem is int year)
        {
            return year;
        }


        return DateTime.Now.Year;
    }


    // =====================================================
    // LOAD MONTHLY REPORT
    // =====================================================

    private async Task LoadMonthlyReportAsync()
    {
        if (_isLoading)
        {
            return;
        }


        try
        {
            SetLoading(true);

            HideError();


            int month =
                GetSelectedMonth();


            int year =
                GetSelectedYear();


            var result =
                await _apiService
                    .GetMonthlyReportAsync(
                        year,
                        month);


            if (!result.Success ||
                result.Data == null)
            {
                MonthlyReportSection.IsVisible =
                    false;


                ShowError(
                    result.Message);


                return;
            }


            DisplayMonthlyReport(
                result.Data);
        }
        catch (Exception ex)
        {
            MonthlyReportSection.IsVisible =
                false;


            ShowError(
                $"Unable to load monthly report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // DISPLAY MONTHLY REPORT
    // =====================================================

    private void DisplayMonthlyReport(
        MonthlyReportResponse report)
    {
        MonthlyReportSection.IsVisible =
            true;


        string monthName =
            GetMonthName(
                report.Month);


        MonthlyPeriodLabel.Text =
            $"{monthName} {report.Year}";


        TotalSpentLabel.Text =
            FormatMoney(
                report.Summary.TotalSpent);


        TransactionCountTextLabel.Text =
            FormatPurchaseCount(
                report.Summary.TransactionCount);


        AveragePurchaseLabel.Text =
            FormatMoney(
                report.Summary.AveragePurchase);


        BiggestPurchaseLabel.Text =
            FormatMoney(
                report.Summary.BiggestPurchase);


        DisplayCategories(
            CategoryReportLayout,
            report.Categories,
            NoCategoryDataLabel);


        DisplayBiggestTransaction(
            report.BiggestTransaction);
    }


    // =====================================================
    // DISPLAY BIGGEST TRANSACTION
    // =====================================================

    private void DisplayBiggestTransaction(
        BiggestTransactionResponse? transaction)
    {
        if (transaction == null)
        {
            BiggestTransactionCard.IsVisible =
                false;

            return;
        }


        BiggestTransactionCard.IsVisible =
            true;


        BiggestTransactionDescriptionLabel.Text =
            string.IsNullOrWhiteSpace(
                transaction.Description)
                ? "Purchase"
                : transaction.Description;


        BiggestTransactionCategoryLabel.Text =
            string.IsNullOrWhiteSpace(
                transaction.Category)
                ? "Category: Not specified"
                : $"Category: {transaction.Category}";


        BiggestTransactionRetailerLabel.Text =
            string.IsNullOrWhiteSpace(
                transaction.Retailer)
                ? "Retailer: Not specified"
                : $"Retailer: {transaction.Retailer}";


        BiggestTransactionDateLabel.Text =
            transaction.CreatedAt.ToString(
                "dd MMM yyyy • HH:mm");


        BiggestTransactionAmountLabel.Text =
            FormatMoney(
                transaction.Amount);
    }


    // =====================================================
    // LOAD RETAILER REPORT
    // =====================================================

    private async Task LoadRetailerReportAsync()
    {
        if (_isLoading)
        {
            return;
        }


        try
        {
            SetLoading(true);

            HideError();


            int month =
                GetSelectedMonth();


            int year =
                GetSelectedYear();


            var result =
                await _apiService
                    .GetRetailerReportAsync(
                        year,
                        month);


            if (!result.Success ||
                result.Data == null)
            {
                RetailerReportSection.IsVisible =
                    false;


                ShowError(
                    result.Message);


                return;
            }


            DisplayRetailerReport(
                result.Data);
        }
        catch (Exception ex)
        {
            RetailerReportSection.IsVisible =
                false;


            ShowError(
                $"Unable to load retailer report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // DISPLAY RETAILER REPORT
    // =====================================================

    private void DisplayRetailerReport(
        RetailerReportResponse report)
    {
        RetailerReportSection.IsVisible =
            true;


        RetailerTotalSpentLabel.Text =
            FormatMoney(
                report.Summary
                    .TotalSpentAtRetailers);


        RetailerCountLabel.Text =
            report.Summary
                .RetailerCount
                .ToString();


        RetailerTransactionCountLabel.Text =
            report.Summary
                .TransactionCount
                .ToString();


        AverageRetailerPurchaseLabel.Text =
            FormatMoney(
                report.Summary
                    .AverageRetailerPurchase);


        DisplayRetailers(
            RetailerReportLayout,
            report.Retailers,
            NoRetailerDataLabel);


        DisplayHighestRetailer(
            report.HighestSpendingRetailer);
    }


    // =====================================================
    // DISPLAY HIGHEST SPENDING RETAILER
    // =====================================================

    private void DisplayHighestRetailer(
        ReportRetailerItem? retailer)
    {
        if (retailer == null)
        {
            HighestRetailerCard.IsVisible =
                false;

            return;
        }


        HighestRetailerCard.IsVisible =
            true;


        HighestRetailerNameLabel.Text =
            string.IsNullOrWhiteSpace(
                retailer.Retailer)
                ? "Unknown retailer"
                : retailer.Retailer;


        HighestRetailerAmountLabel.Text =
            FormatMoney(
                retailer.Amount);
    }


    // =====================================================
    // LOAD DATE RANGE REPORT
    // =====================================================

    private async Task LoadDateRangeReportAsync()
    {
        if (_isLoading)
        {
            return;
        }


        DateTime startDate =
            StartDatePicker.Date;


        DateTime endDate =
            EndDatePicker.Date;


        if (startDate.Date >
            endDate.Date)
        {
            ShowError(
                "Start date cannot be after end date.");

            return;
        }


        try
        {
            SetLoading(true);

            HideError();


            var result =
                await _apiService
                    .GetDateRangeReportAsync(
                        startDate,
                        endDate);


            if (!result.Success ||
                result.Data == null)
            {
                DateRangeReportSection.IsVisible =
                    false;


                ShowError(
                    result.Message);


                return;
            }


            DisplayDateRangeReport(
                result.Data);
        }
        catch (Exception ex)
        {
            DateRangeReportSection.IsVisible =
                false;


            ShowError(
                $"Unable to load date range report: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // DISPLAY DATE RANGE REPORT
    // =====================================================

    private void DisplayDateRangeReport(
        DateRangeReportResponse report)
    {
        DateRangeReportSection.IsVisible =
            true;


        DateRangePeriodLabel.Text =
            $"{report.StartDate:dd MMM yyyy} - " +
            $"{report.EndDate:dd MMM yyyy}";


        DateRangeTotalSpentLabel.Text =
            FormatMoney(
                report.Summary.TotalSpent);


        DateRangeTransactionCountLabel.Text =
            FormatPurchaseCount(
                report.Summary.TransactionCount);


        DateRangeAverageLabel.Text =
            FormatMoney(
                report.Summary.AveragePurchase);


        DateRangeBiggestLabel.Text =
            FormatMoney(
                report.Summary.BiggestPurchase);


        DisplayCategories(
            DateRangeCategoryLayout,
            report.Categories,
            NoDateRangeCategoryLabel);


        DisplayRetailers(
            DateRangeRetailerLayout,
            report.Retailers,
            NoDateRangeRetailerLabel);


        DisplayDateRangeTransactions(
            report.Transactions);
    }


    // =====================================================
    // DISPLAY CATEGORY LIST
    // =====================================================

    private void DisplayCategories(
        VerticalStackLayout layout,
        List<ReportCategoryItem>? categories,
        Label emptyLabel)
    {
        layout.Children.Clear();


        if (categories == null ||
            categories.Count == 0)
        {
            emptyLabel.IsVisible =
                true;

            return;
        }


        emptyLabel.IsVisible =
            false;


        foreach (ReportCategoryItem category
                 in categories)
        {
            Border card =
                CreateCategoryCard(
                    category);


            layout.Children.Add(
                card);
        }
    }


    // =====================================================
    // CREATE CATEGORY CARD
    // =====================================================

    private Border CreateCategoryCard(
        ReportCategoryItem category)
    {
        Label categoryNameLabel =
            new Label
            {
                Text =
                    string.IsNullOrWhiteSpace(
                        category.Category)
                        ? "Uncategorised"
                        : category.Category,

                FontSize = 14,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#173C2A")
            };


        Label transactionLabel =
            new Label
            {
                Text =
                    FormatPurchaseCount(
                        category.TransactionCount),

                FontSize = 11,

                TextColor =
                    Color.FromArgb(
                        "#718078")
            };


        VerticalStackLayout leftLayout =
            new VerticalStackLayout
            {
                Spacing = 2
            };


        leftLayout.Children.Add(
            categoryNameLabel);

        leftLayout.Children.Add(
            transactionLabel);


        Label amountLabel =
            new Label
            {
                Text =
                    FormatMoney(
                        category.Amount),

                FontSize = 15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#146B3A"),

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        Label percentageLabel =
            new Label
            {
                Text =
                    $"{category.Percentage:0.##}%",

                FontSize = 11,

                TextColor =
                    Color.FromArgb(
                        "#718078"),

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        VerticalStackLayout rightLayout =
            new VerticalStackLayout
            {
                Spacing = 2,

                HorizontalOptions =
                    LayoutOptions.End
            };


        rightLayout.Children.Add(
            amountLabel);

        rightLayout.Children.Add(
            percentageLabel);


        Grid contentGrid =
            new Grid
            {
                ColumnSpacing = 10
            };


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });


        contentGrid.Children.Add(
            leftLayout);

        Grid.SetColumn(
            leftLayout,
            0);


        contentGrid.Children.Add(
            rightLayout);

        Grid.SetColumn(
            rightLayout,
            1);


        Border card =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb(
                        "#F8FAF9"),

                Stroke =
                    Color.FromArgb(
                        "#E1E8E4"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(
                        12),

                Content =
                    contentGrid,

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                12)
                    }
            };


        return card;
    }


    // =====================================================
    // DISPLAY RETAILER LIST
    // =====================================================

    private void DisplayRetailers(
        VerticalStackLayout layout,
        List<ReportRetailerItem>? retailers,
        Label emptyLabel)
    {
        layout.Children.Clear();


        if (retailers == null ||
            retailers.Count == 0)
        {
            emptyLabel.IsVisible =
                true;

            return;
        }


        emptyLabel.IsVisible =
            false;


        foreach (ReportRetailerItem retailer
                 in retailers)
        {
            Border card =
                CreateRetailerCard(
                    retailer);


            layout.Children.Add(
                card);
        }
    }


    // =====================================================
    // CREATE RETAILER CARD
    // =====================================================

    private Border CreateRetailerCard(
        ReportRetailerItem retailer)
    {
        Label retailerNameLabel =
            new Label
            {
                Text =
                    string.IsNullOrWhiteSpace(
                        retailer.Retailer)
                        ? "Unknown retailer"
                        : retailer.Retailer,

                FontSize = 14,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#173C2A")
            };


        Label transactionLabel =
            new Label
            {
                Text =
                    FormatPurchaseCount(
                        retailer.TransactionCount),

                FontSize = 11,

                TextColor =
                    Color.FromArgb(
                        "#718078")
            };


        VerticalStackLayout leftLayout =
            new VerticalStackLayout
            {
                Spacing = 2
            };


        leftLayout.Children.Add(
            retailerNameLabel);

        leftLayout.Children.Add(
            transactionLabel);


        Label amountLabel =
            new Label
            {
                Text =
                    FormatMoney(
                        retailer.Amount),

                FontSize = 15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#146B3A"),

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        Label percentageLabel =
            new Label
            {
                Text =
                    $"{retailer.Percentage:0.##}%",

                FontSize = 11,

                TextColor =
                    Color.FromArgb(
                        "#718078"),

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        VerticalStackLayout rightLayout =
            new VerticalStackLayout
            {
                Spacing = 2,

                HorizontalOptions =
                    LayoutOptions.End
            };


        rightLayout.Children.Add(
            amountLabel);

        rightLayout.Children.Add(
            percentageLabel);


        Grid contentGrid =
            new Grid
            {
                ColumnSpacing = 10
            };


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });


        contentGrid.Children.Add(
            leftLayout);

        Grid.SetColumn(
            leftLayout,
            0);


        contentGrid.Children.Add(
            rightLayout);

        Grid.SetColumn(
            rightLayout,
            1);


        Border card =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb(
                        "#F8FAF9"),

                Stroke =
                    Color.FromArgb(
                        "#E1E8E4"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(
                        12),

                Content =
                    contentGrid,

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                12)
                    }
            };


        return card;
    }


    // =====================================================
    // DISPLAY DATE RANGE TRANSACTIONS
    // =====================================================

    private void DisplayDateRangeTransactions(
        List<ReportTransactionItem>? transactions)
    {
        DateRangeTransactionLayout
            .Children
            .Clear();


        if (transactions == null ||
            transactions.Count == 0)
        {
            NoDateRangeTransactionsLabel
                .IsVisible =
                true;

            return;
        }


        NoDateRangeTransactionsLabel
            .IsVisible =
            false;


        foreach (ReportTransactionItem transaction
                 in transactions)
        {
            Border card =
                CreateTransactionCard(
                    transaction);


            DateRangeTransactionLayout
                .Children
                .Add(
                    card);
        }
    }


    // =====================================================
    // CREATE TRANSACTION CARD
    // =====================================================

    private Border CreateTransactionCard(
        ReportTransactionItem transaction)
    {
        string description =
            string.IsNullOrWhiteSpace(
                transaction.Description)
                ? "Purchase"
                : transaction.Description;


        string category =
            string.IsNullOrWhiteSpace(
                transaction.Category)
                ? "Uncategorised"
                : transaction.Category;


        string retailer =
            string.IsNullOrWhiteSpace(
                transaction.Retailer)
                ? "No retailer"
                : transaction.Retailer;


        Label descriptionLabel =
            new Label
            {
                Text =
                    description,

                FontSize = 14,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#173C2A"),

                LineBreakMode =
                    LineBreakMode.TailTruncation
            };


        Label detailsLabel =
            new Label
            {
                Text =
                    $"{category} • {retailer}",

                FontSize = 11,

                TextColor =
                    Color.FromArgb(
                        "#718078"),

                LineBreakMode =
                    LineBreakMode.TailTruncation
            };


        Label dateLabel =
            new Label
            {
                Text =
                    transaction.CreatedAt
                        .ToString(
                            "dd MMM yyyy • HH:mm"),

                FontSize = 10,

                TextColor =
                    Color.FromArgb(
                        "#8A968F")
            };


        VerticalStackLayout leftLayout =
            new VerticalStackLayout
            {
                Spacing = 3
            };


        leftLayout.Children.Add(
            descriptionLabel);

        leftLayout.Children.Add(
            detailsLabel);

        leftLayout.Children.Add(
            dateLabel);


        Label amountLabel =
            new Label
            {
                Text =
                    FormatMoney(
                        transaction.Amount),

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#146B3A"),

                VerticalOptions =
                    LayoutOptions.Center,

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        Grid contentGrid =
            new Grid
            {
                ColumnSpacing = 12
            };


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });


        contentGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });


        contentGrid.Children.Add(
            leftLayout);

        Grid.SetColumn(
            leftLayout,
            0);


        contentGrid.Children.Add(
            amountLabel);

        Grid.SetColumn(
            amountLabel,
            1);


        Border card =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb(
                        "#F8FAF9"),

                Stroke =
                    Color.FromArgb(
                        "#E1E8E4"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(
                        12),

                Content =
                    contentGrid,

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                12)
                    }
            };


        return card;
    }


    // =====================================================
    // SET LOADING STATE
    // =====================================================

    private void SetLoading(
        bool isLoading)
    {
        _isLoading =
            isLoading;


        LoadingIndicator.IsVisible =
            isLoading;


        LoadingIndicator.IsRunning =
            isLoading;


        LoadMonthlyReportButton.IsEnabled =
            !isLoading;


        LoadRetailerReportButton.IsEnabled =
            !isLoading;


        LoadDateRangeReportButton.IsEnabled =
            !isLoading;


        MonthPicker.IsEnabled =
            !isLoading;


        YearPicker.IsEnabled =
            !isLoading;


        StartDatePicker.IsEnabled =
            !isLoading;


        EndDatePicker.IsEnabled =
            !isLoading;
    }


    // =====================================================
    // SHOW ERROR
    // =====================================================

    private void ShowError(
        string? message)
    {
        ErrorLabel.Text =
            string.IsNullOrWhiteSpace(
                message)
                ? "Something went wrong."
                : message;


        ErrorCard.IsVisible =
            true;
    }


    // =====================================================
    // HIDE ERROR
    // =====================================================

    private void HideError()
    {
        ErrorCard.IsVisible =
            false;


        ErrorLabel.Text =
            string.Empty;
    }


    // =====================================================
    // FORMAT MONEY
    // =====================================================

    private static string FormatMoney(
        decimal amount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "R{0:N2}",
            amount);
    }


    // =====================================================
    // FORMAT PURCHASE COUNT
    // =====================================================

    private static string FormatPurchaseCount(
        int count)
    {
        if (count == 1)
        {
            return "1 purchase";
        }


        return $"{count} purchases";
    }


    // =====================================================
    // GET MONTH NAME
    // =====================================================

    private static string GetMonthName(
        int month)
    {
        if (month < 1 ||
            month > 12)
        {
            return "Unknown Month";
        }


        return CultureInfo
            .InvariantCulture
            .DateTimeFormat
            .GetMonthName(
                month);
    }


    // =====================================================
    // BACK BUTTON
    // =====================================================

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    // =====================================================
    // REFRESH BUTTON
    // =====================================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        await LoadMonthlyReportAsync();


        await LoadRetailerReportAsync();
    }


    // =====================================================
    // VIEW MONTHLY REPORT BUTTON
    // =====================================================

    private async void LoadMonthlyReportButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadMonthlyReportAsync();
    }


    // =====================================================
    // VIEW RETAILER REPORT BUTTON
    // =====================================================

    private async void LoadRetailerReportButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadRetailerReportAsync();
    }


    // =====================================================
    // DATE RANGE REPORT BUTTON
    // =====================================================

    private async void LoadDateRangeReportButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadDateRangeReportAsync();
    }
}