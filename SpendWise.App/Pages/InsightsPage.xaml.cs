using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class InsightsPage : ContentPage
{
    private readonly ApiService _apiService;

    private bool _isLoading;


    // =====================================================
    // MONTHS
    // =====================================================

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

    public InsightsPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        SetupPickers();
    }


    // =====================================================
    // PAGE APPEARING
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadInsightsAsync();
    }


    // =====================================================
    // SETUP PICKERS
    // =====================================================

    private void SetupPickers()
    {
        DateTime today =
            DateTime.Now;


        // MONTHS

        MonthPicker.ItemsSource =
            _months;

        MonthPicker.SelectedIndex =
            today.Month - 1;


        // YEARS

        List<int> years =
            new List<int>();


        for (int year = today.Year;
             year >= 2020;
             year--)
        {
            years.Add(year);
        }


        YearPicker.ItemsSource =
            years;

        YearPicker.SelectedIndex =
            0;
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
    // LOAD INSIGHTS
    // =====================================================

    private async Task LoadInsightsAsync()
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
                    .GetInsightsAsync(
                        year,
                        month);


            if (!result.Success ||
                result.Data == null)
            {
                ClearInsights();

                ShowError(
                    result.Message);

                return;
            }


            DisplayInsights(
                result.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"LOAD INSIGHTS ERROR: {ex}");


            ClearInsights();


            ShowError(
                $"Unable to load insights: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }


    // =====================================================
    // DISPLAY ALL INSIGHTS
    // =====================================================

    private void DisplayInsights(
        InsightsResponse data)
    {
        InsightsSection.IsVisible =
            true;


        // =================================================
        // PERIOD
        // =================================================

        InsightsPeriodLabel.Text =
            $"{GetMonthName(data.Month)} {data.Year}";


        // =================================================
        // SUMMARY
        // =================================================

        decimal totalBudget =
            data.Summary.TotalBudget;


        decimal totalSpent =
            data.Summary.TotalSpent;


        decimal totalRemaining =
            data.Summary.TotalRemaining;


        // =================================================
        // AVAILABLE BUDGET
        // =================================================
        //
        // This is a budgeting value only.
        //
        // It is NOT:
        // - a SpendWise wallet balance
        // - a bank account balance
        // - money held by SpendWise
        //
        // Available Budget =
        // Total Budget - Total Spent
        // =================================================

        decimal availableBudget =
            totalBudget - totalSpent;


        TotalBudgetLabel.Text =
            FormatMoney(
                totalBudget);


        TotalSpentLabel.Text =
            FormatMoney(
                totalSpent);


        SetSignedMoneyLabel(
            TotalRemainingLabel,
            totalRemaining);


        SetSignedMoneyLabel(
            AvailableBudgetLabel,
            availableBudget);


        PercentageUsedLabel.Text =
            $"{data.Summary.PercentageUsed:N1}%";


        // =================================================
        // PROGRESS
        // =================================================

        double progress =
            0;


        if (totalBudget > 0)
        {
            progress =
                (double)(
                    totalSpent /
                    totalBudget);
        }


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        BudgetProgressBar.Progress =
            progress;


        // =================================================
        // GENERAL INSIGHTS
        // =================================================

        DisplayGeneralInsights(
            data.Insights);


        // =================================================
        // CATEGORY INSIGHTS
        // =================================================

        DisplayCategoryInsights(
            data.Categories);
    }


    // =====================================================
    // SET SIGNED MONEY LABEL
    // =====================================================

    private void SetSignedMoneyLabel(
        Label label,
        decimal amount)
    {
        if (amount < 0)
        {
            label.Text =
                $"-{FormatMoney(
                    Math.Abs(amount))}";


            label.TextColor =
                Color.FromArgb(
                    "#FFD6D2");
        }
        else
        {
            label.Text =
                FormatMoney(
                    amount);


            label.TextColor =
                Colors.White;
        }
    }


    // =====================================================
    // DISPLAY GENERAL INSIGHTS
    // =====================================================

    private void DisplayGeneralInsights(
        List<InsightItem>? insights)
    {
        GeneralInsightsLayout
            .Children.Clear();


        if (insights == null ||
            insights.Count == 0)
        {
            NoGeneralInsightsLabel.IsVisible =
                true;

            return;
        }


        NoGeneralInsightsLabel.IsVisible =
            false;


        foreach (InsightItem insight
                 in insights)
        {
            Border card =
                CreateGeneralInsightCard(
                    insight);


            GeneralInsightsLayout
                .Children.Add(
                    card);
        }
    }


    // =====================================================
    // CREATE GENERAL INSIGHT CARD
    // =====================================================

    private Border CreateGeneralInsightCard(
        InsightItem insight)
    {
        string level =
            insight.Level ??
            string.Empty;


        Color statusColor =
            GetStatusColor(
                level);


        Color backgroundColor =
            GetStatusBackgroundColor(
                level);


        // =================================================
        // STATUS LABEL
        // =================================================

        Label statusLabel =
            new Label
            {
                Text =
                    string.IsNullOrWhiteSpace(
                        insight.Level)
                        ? "Info"
                        : insight.Level,

                FontSize =
                    11,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    statusColor,

                HorizontalOptions =
                    LayoutOptions.End,

                VerticalOptions =
                    LayoutOptions.Center
            };


        // =================================================
        // TITLE
        // =================================================

        Label titleLabel =
            new Label
            {
                Text =
                    string.IsNullOrWhiteSpace(
                        insight.Type)
                        ? "Insight"
                        : insight.Type,

                FontSize =
                    15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E"),

                VerticalOptions =
                    LayoutOptions.Center
            };


        // =================================================
        // HEADER
        // =================================================

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
                }
            };


        Grid.SetColumn(
            titleLabel,
            0);


        Grid.SetColumn(
            statusLabel,
            1);


        headerGrid.Children.Add(
            titleLabel);


        headerGrid.Children.Add(
            statusLabel);


        // =================================================
        // CATEGORY
        // =================================================

        Label categoryLabel =
            new Label
            {
                Text =
                    string.IsNullOrWhiteSpace(
                        insight.Category)
                        ? string.Empty
                        : $"Category: {insight.Category}",

                FontSize =
                    12,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    statusColor,

                IsVisible =
                    !string.IsNullOrWhiteSpace(
                        insight.Category)
            };


        // =================================================
        // MESSAGE
        // =================================================

        Label messageLabel =
            new Label
            {
                Text =
                    insight.Message ??
                    string.Empty,

                FontSize =
                    13,

                TextColor =
                    Color.FromArgb(
                        "#555555"),

                LineBreakMode =
                    LineBreakMode.WordWrap
            };


        // =================================================
        // CONTENT
        // =================================================

        VerticalStackLayout content =
            new VerticalStackLayout
            {
                Spacing =
                    8
            };


        content.Children.Add(
            headerGrid);


        if (categoryLabel.IsVisible)
        {
            content.Children.Add(
                categoryLabel);
        }


        content.Children.Add(
            messageLabel);


        // =================================================
        // CARD
        // =================================================

        Border card =
            new Border
            {
                BackgroundColor =
                    backgroundColor,

                Stroke =
                    statusColor,

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(
                        15),

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(
                                13)
                    },

                Content =
                    content
            };


        return card;
    }


    // =====================================================
    // DISPLAY CATEGORY INSIGHTS
    // =====================================================

    private void DisplayCategoryInsights(
        List<CategoryInsightItem>? categories)
    {
        CategoryInsightsLayout
            .Children.Clear();


        if (categories == null ||
            categories.Count == 0)
        {
            NoCategoryInsightsLabel.IsVisible =
                true;

            return;
        }


        NoCategoryInsightsLabel.IsVisible =
            false;


        foreach (CategoryInsightItem category
                 in categories)
        {
            Border card =
                CreateCategoryInsightCard(
                    category);


            CategoryInsightsLayout
                .Children.Add(
                    card);
        }
    }


    // =====================================================
    // CREATE CATEGORY INSIGHT CARD
    // =====================================================

    private Border CreateCategoryInsightCard(
        CategoryInsightItem category)
    {
        Color statusColor =
            GetStatusColor(
                category.Status);


        // =================================================
        // CATEGORY NAME
        // =================================================

        Label categoryNameLabel =
            new Label
            {
                Text =
                    category.Category,

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        // =================================================
        // STATUS
        // =================================================

        Label statusLabel =
            new Label
            {
                Text =
                    category.Status,

                FontSize =
                    11,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    statusColor,

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
                }
            };


        Grid.SetColumn(
            categoryNameLabel,
            0);


        Grid.SetColumn(
            statusLabel,
            1);


        headerGrid.Children.Add(
            categoryNameLabel);


        headerGrid.Children.Add(
            statusLabel);


        // =================================================
        // BUDGET
        // =================================================

        Label budgetTitleLabel =
            CreateSmallTitleLabel(
                "Budget");


        Label budgetValueLabel =
            CreateMoneyValueLabel(
                category.Budget);


        VerticalStackLayout budgetLayout =
            new VerticalStackLayout
            {
                Spacing =
                    2
            };


        budgetLayout.Children.Add(
            budgetTitleLabel);


        budgetLayout.Children.Add(
            budgetValueLabel);


        // =================================================
        // SPENT
        // =================================================

        Label spentTitleLabel =
            CreateSmallTitleLabel(
                "Spent");


        Label spentValueLabel =
            CreateMoneyValueLabel(
                category.Spent);


        VerticalStackLayout spentLayout =
            new VerticalStackLayout
            {
                Spacing =
                    2
            };


        spentLayout.Children.Add(
            spentTitleLabel);


        spentLayout.Children.Add(
            spentValueLabel);


        // =================================================
        // REMAINING
        // =================================================

        Label remainingTitleLabel =
            CreateSmallTitleLabel(
                "Remaining");


        Label remainingValueLabel =
            CreateMoneyValueLabel(
                category.Remaining);


        if (category.Remaining < 0)
        {
            remainingValueLabel.Text =
                $"-{FormatMoney(
                    Math.Abs(
                        category.Remaining))}";


            remainingValueLabel.TextColor =
                Color.FromArgb(
                    "#C0392B");
        }


        VerticalStackLayout remainingLayout =
            new VerticalStackLayout
            {
                Spacing =
                    2
            };


        remainingLayout.Children.Add(
            remainingTitleLabel);


        remainingLayout.Children.Add(
            remainingValueLabel);


        // =================================================
        // VALUES GRID
        // =================================================

        Grid valuesGrid =
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
                            GridLength.Star
                    },

                    new ColumnDefinition
                    {
                        Width =
                            GridLength.Star
                    }
                },

                ColumnSpacing =
                    10
            };


        Grid.SetColumn(
            budgetLayout,
            0);


        Grid.SetColumn(
            spentLayout,
            1);


        Grid.SetColumn(
            remainingLayout,
            2);


        valuesGrid.Children.Add(
            budgetLayout);


        valuesGrid.Children.Add(
            spentLayout);


        valuesGrid.Children.Add(
            remainingLayout);


        // =================================================
        // PERCENTAGE HEADER
        // =================================================

        Label usageTitleLabel =
            new Label
            {
                Text =
                    "Budget Used",

                FontSize =
                    11,

                TextColor =
                    Color.FromArgb(
                        "#777777")
            };


        Label percentageLabel =
            new Label
            {
                Text =
                    $"{category.PercentageUsed:N1}%",

                FontSize =
                    11,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    statusColor,

                HorizontalOptions =
                    LayoutOptions.End
            };


        Grid usageHeaderGrid =
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
                }
            };


        Grid.SetColumn(
            usageTitleLabel,
            0);


        Grid.SetColumn(
            percentageLabel,
            1);


        usageHeaderGrid.Children.Add(
            usageTitleLabel);


        usageHeaderGrid.Children.Add(
            percentageLabel);


        // =================================================
        // PROGRESS
        // =================================================

        double progress =
            0;


        if (category.Budget > 0)
        {
            progress =
                (double)(
                    category.Spent /
                    category.Budget);
        }


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        ProgressBar progressBar =
            new ProgressBar
            {
                Progress =
                    progress,

                ProgressColor =
                    statusColor,

                BackgroundColor =
                    Color.FromArgb(
                        "#E7E7E7"),

                HeightRequest =
                    7
            };


        // =================================================
        // MESSAGE
        // =================================================

        Label messageLabel =
            new Label
            {
                Text =
                    category.Message,

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb(
                        "#555555"),

                LineBreakMode =
                    LineBreakMode.WordWrap
            };


        // =================================================
        // CONTENT
        // =================================================

        VerticalStackLayout content =
            new VerticalStackLayout
            {
                Spacing =
                    11
            };


        content.Children.Add(
            headerGrid);


        content.Children.Add(
            valuesGrid);


        content.Children.Add(
            usageHeaderGrid);


        content.Children.Add(
            progressBar);


        content.Children.Add(
            messageLabel);


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
                    new Microsoft.Maui.Controls.Shapes
                        .RoundRectangle
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
    // SMALL TITLE LABEL
    // =====================================================

    private Label CreateSmallTitleLabel(
        string text)
    {
        return new Label
        {
            Text =
                text,

            FontSize =
                10,

            TextColor =
                Color.FromArgb(
                    "#777777")
        };
    }


    // =====================================================
    // MONEY VALUE LABEL
    // =====================================================

    private Label CreateMoneyValueLabel(
        decimal amount)
    {
        Label label =
            new Label
            {
                FontSize =
                    13,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        if (amount < 0)
        {
            label.Text =
                $"-{FormatMoney(
                    Math.Abs(amount))}";


            label.TextColor =
                Color.FromArgb(
                    "#C0392B");
        }
        else
        {
            label.Text =
                FormatMoney(
                    amount);
        }


        return label;
    }


    // =====================================================
    // STATUS COLOR
    // =====================================================

    private Color GetStatusColor(
        string? status)
    {
        string value =
            status?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;


        return value switch
        {
            "safe" =>
                Color.FromArgb(
                    "#146B3A"),

            "info" =>
                Color.FromArgb(
                    "#1565C0"),

            "warning" =>
                Color.FromArgb(
                    "#C47F00"),

            "high warning" =>
                Color.FromArgb(
                    "#D35400"),

            "budget reached" =>
                Color.FromArgb(
                    "#C0392B"),

            "over budget" =>
                Color.FromArgb(
                    "#B42318"),

            _ =>
                Color.FromArgb(
                    "#555555")
        };
    }


    // =====================================================
    // STATUS BACKGROUND COLOR
    // =====================================================

    private Color GetStatusBackgroundColor(
        string? status)
    {
        string value =
            status?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;


        return value switch
        {
            "safe" =>
                Color.FromArgb(
                    "#EEF7F1"),

            "info" =>
                Color.FromArgb(
                    "#EEF5FC"),

            "warning" =>
                Color.FromArgb(
                    "#FFF8E8"),

            "high warning" =>
                Color.FromArgb(
                    "#FFF3E8"),

            "budget reached" =>
                Color.FromArgb(
                    "#FFF1F0"),

            "over budget" =>
                Color.FromArgb(
                    "#FFF1F0"),

            _ =>
                Color.FromArgb(
                    "#F7F7F7")
        };
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
    // GET MONTH NAME
    // =====================================================

    private string GetMonthName(
        int month)
    {
        if (month < 1 ||
            month > 12)
        {
            return string.Empty;
        }


        return _months[
            month - 1];
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


        LoadInsightsButton.IsEnabled =
            !isLoading;


        RefreshButton.IsEnabled =
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
    // CLEAR INSIGHTS
    // =====================================================

    private void ClearInsights()
    {
        InsightsSection.IsVisible =
            false;


        TotalBudgetLabel.Text =
            "R 0.00";


        TotalSpentLabel.Text =
            "R 0.00";


        TotalRemainingLabel.Text =
            "R 0.00";


        TotalRemainingLabel.TextColor =
            Colors.White;


        AvailableBudgetLabel.Text =
            "R 0.00";


        AvailableBudgetLabel.TextColor =
            Colors.White;


        PercentageUsedLabel.Text =
            "0%";


        BudgetProgressBar.Progress =
            0;


        GeneralInsightsLayout
            .Children.Clear();


        CategoryInsightsLayout
            .Children.Clear();


        NoGeneralInsightsLabel.IsVisible =
            false;


        NoCategoryInsightsLabel.IsVisible =
            false;
    }


    // =====================================================
    // BACK
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
    // REFRESH
    // =====================================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadInsightsAsync();
    }


    // =====================================================
    // LOAD INSIGHTS BUTTON
    // =====================================================

    private async void LoadInsightsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadInsightsAsync();
    }
}