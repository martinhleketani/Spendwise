
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Diagnostics;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class InsightsPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isLoading;

    private readonly List<string> _months = new()
    {
        "January", "February", "March", "April",
        "May", "June", "July", "August",
        "September", "October", "November", "December"
    };

    public InsightsPage()
    {
        InitializeComponent();

        _apiService = new ApiService();

        SetupPickers();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInsightsAsync();
    }

    // =====================================================
    // PICKERS
    // =====================================================

    private void SetupPickers()
    {
        DateTime today = DateTime.Now;

        MonthPicker.ItemsSource = _months;
        MonthPicker.SelectedIndex = today.Month - 1;

        YearEntry.Text = today.Year.ToString();
    }

    private int GetSelectedMonth()
    {
        return MonthPicker.SelectedIndex < 0
            ? DateTime.Now.Month
            : MonthPicker.SelectedIndex + 1;
    }

    private bool TryGetSelectedYear(out int year)
    {
        return int.TryParse(YearEntry.Text, out year)
               && year >= 1
               && year <= 9999;
    }

    // =====================================================
    // LOAD INSIGHTS
    // =====================================================

    private async Task LoadInsightsAsync()
    {
        if (_isLoading)
            return;

        try
        {
            SetLoading(true);
            HideError();

            if (!TryGetSelectedYear(out int year))
            {
                ClearInsights();

                ShowError(
                    "Please enter a valid year between 1 and 9999.");

                return;
            }

            int month = GetSelectedMonth();

            Debug.WriteLine(
                $"Loading insights for {year}/{month}");

            var result =
                await _apiService.GetInsightsAsync(year, month);

            if (!result.Success || result.Data == null)
            {
                ClearInsights();
                ShowError(result.Message);
                return;
            }

            DisplayInsights(result.Data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LOAD INSIGHTS ERROR: {ex}");

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
    // DISPLAY INSIGHTS
    // =====================================================

    private void DisplayInsights(InsightsResponse data)
    {
        InsightsSection.IsVisible = true;

        InsightsPeriodLabel.Text =
            $"{GetMonthName(data.Month)} {data.Year}";

        decimal totalBudget = data.Summary.TotalBudget;
        decimal totalSpent = data.Summary.TotalSpent;
        decimal totalRemaining = data.Summary.TotalRemaining;

        decimal availableBudget = totalBudget - totalSpent;

        TotalBudgetLabel.Text = FormatMoney(totalBudget);
        TotalSpentLabel.Text = FormatMoney(totalSpent);

        SetSignedMoneyLabel(
            TotalRemainingLabel,
            totalRemaining);

        SetSignedMoneyLabel(
            AvailableBudgetLabel,
            availableBudget);

        PercentageUsedLabel.Text =
            $"{data.Summary.PercentageUsed:N1}%";

        double progress = totalBudget > 0
            ? (double)(totalSpent / totalBudget)
            : 0;

        BudgetProgressBar.Progress =
            Math.Clamp(progress, 0, 1);

        DisplayGeneralInsights(data.Insights);
        DisplayCategoryInsights(data.Categories);
    }

    private void SetSignedMoneyLabel(
        Label label,
        decimal amount)
    {
        label.Text = amount < 0
            ? $"-{FormatMoney(Math.Abs(amount))}"
            : FormatMoney(amount);

        label.TextColor = amount < 0
            ? Color.FromArgb("#FFD6D2")
            : Colors.White;
    }

    // =====================================================
    // GENERAL INSIGHTS
    // =====================================================

    private void DisplayGeneralInsights(
        List<InsightItem>? insights)
    {
        GeneralInsightsLayout.Children.Clear();

        if (insights == null || insights.Count == 0)
        {
            NoGeneralInsightsLabel.IsVisible = true;
            return;
        }

        NoGeneralInsightsLabel.IsVisible = false;

        foreach (InsightItem insight in insights)
        {
            GeneralInsightsLayout.Children.Add(
                CreateGeneralInsightCard(insight));
        }
    }

    private Border CreateGeneralInsightCard(
        InsightItem insight)
    {
        string level = insight.Level ?? string.Empty;

        Color statusColor = GetStatusColor(level);
        Color backgroundColor =
            GetStatusBackgroundColor(level);

        Label statusLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(insight.Level)
                ? "Info"
                : insight.Level,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = statusColor,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        Label titleLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(insight.Type)
                ? "Insight"
                : insight.Type,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1E1E"),
            VerticalOptions = LayoutOptions.Center
        };

        Grid headerGrid = new()
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
            }
        };

        Grid.SetColumn(titleLabel, 0);
        Grid.SetColumn(statusLabel, 1);

        headerGrid.Children.Add(titleLabel);
        headerGrid.Children.Add(statusLabel);

        Label categoryLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(insight.Category)
                ? string.Empty
                : $"Category: {insight.Category}",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = statusColor,
            IsVisible =
                !string.IsNullOrWhiteSpace(insight.Category)
        };

        Label messageLabel = new()
        {
            Text = insight.Message ?? string.Empty,
            FontSize = 13,
            TextColor = Color.FromArgb("#555555"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        VerticalStackLayout content = new()
        {
            Spacing = 8
        };

        content.Children.Add(headerGrid);

        if (categoryLabel.IsVisible)
            content.Children.Add(categoryLabel);

        content.Children.Add(messageLabel);

        return new Border
        {
            BackgroundColor = backgroundColor,
            Stroke = statusColor,
            StrokeThickness = 1,
            Padding = new Thickness(15),
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(13)
                },
            Content = content
        };
    }

    // =====================================================
    // CATEGORY INSIGHTS
    // =====================================================

    private void DisplayCategoryInsights(
        List<CategoryInsightItem>? categories)
    {
        CategoryInsightsLayout.Children.Clear();

        if (categories == null || categories.Count == 0)
        {
            NoCategoryInsightsLabel.IsVisible = true;
            return;
        }

        NoCategoryInsightsLabel.IsVisible = false;

        foreach (CategoryInsightItem category in categories)
        {
            CategoryInsightsLayout.Children.Add(
                CreateCategoryInsightCard(category));
        }
    }

    private Border CreateCategoryInsightCard(
        CategoryInsightItem category)
    {
        Color statusColor =
            GetStatusColor(category.Status);

        Label categoryNameLabel = new()
        {
            Text = category.Category,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1E1E")
        };

        Label statusLabel = new()
        {
            Text = category.Status,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = statusColor,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        Grid headerGrid = new()
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
            }
        };

        Grid.SetColumn(categoryNameLabel, 0);
        Grid.SetColumn(statusLabel, 1);

        headerGrid.Children.Add(categoryNameLabel);
        headerGrid.Children.Add(statusLabel);

        VerticalStackLayout budgetLayout =
            CreateValueLayout("Budget", category.Budget);

        VerticalStackLayout spentLayout =
            CreateValueLayout("Spent", category.Spent);

        VerticalStackLayout remainingLayout =
            CreateValueLayout("Remaining", category.Remaining);

        if (category.Remaining < 0)
        {
            Label remainingValue =
                (Label)remainingLayout.Children[1];

            remainingValue.Text =
                $"-{FormatMoney(Math.Abs(category.Remaining))}";

            remainingValue.TextColor =
                Color.FromArgb("#C0392B");
        }

        Grid valuesGrid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = GridLength.Star
                },
                new ColumnDefinition
                {
                    Width = GridLength.Star
                },
                new ColumnDefinition
                {
                    Width = GridLength.Star
                }
            },
            ColumnSpacing = 10
        };

        Grid.SetColumn(budgetLayout, 0);
        Grid.SetColumn(spentLayout, 1);
        Grid.SetColumn(remainingLayout, 2);

        valuesGrid.Children.Add(budgetLayout);
        valuesGrid.Children.Add(spentLayout);
        valuesGrid.Children.Add(remainingLayout);

        Label usageTitleLabel = new()
        {
            Text = "Budget Used",
            FontSize = 11,
            TextColor = Color.FromArgb("#777777")
        };

        Label percentageLabel = new()
        {
            Text = $"{category.PercentageUsed:N1}%",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = statusColor,
            HorizontalOptions = LayoutOptions.End
        };

        Grid usageHeaderGrid = new()
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
            }
        };

        Grid.SetColumn(usageTitleLabel, 0);
        Grid.SetColumn(percentageLabel, 1);

        usageHeaderGrid.Children.Add(usageTitleLabel);
        usageHeaderGrid.Children.Add(percentageLabel);

        double progress = category.Budget > 0
            ? (double)(category.Spent / category.Budget)
            : 0;

        ProgressBar progressBar = new()
        {
            Progress = Math.Clamp(progress, 0, 1),
            ProgressColor = statusColor,
            BackgroundColor = Color.FromArgb("#E7E7E7"),
            HeightRequest = 7
        };

        Label messageLabel = new()
        {
            Text = category.Message,
            FontSize = 12,
            TextColor = Color.FromArgb("#555555"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        VerticalStackLayout content = new()
        {
            Spacing = 11
        };

        content.Children.Add(headerGrid);
        content.Children.Add(valuesGrid);
        content.Children.Add(usageHeaderGrid);
        content.Children.Add(progressBar);
        content.Children.Add(messageLabel);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5E5E5"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(14)
                },
            Content = content
        };
    }

    private VerticalStackLayout CreateValueLayout(
        string title,
        decimal amount)
    {
        VerticalStackLayout layout = new()
        {
            Spacing = 2
        };

        layout.Children.Add(CreateSmallTitleLabel(title));
        layout.Children.Add(CreateMoneyValueLabel(amount));

        return layout;
    }

    private Label CreateSmallTitleLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 10,
            TextColor = Color.FromArgb("#777777")
        };
    }

    private Label CreateMoneyValueLabel(decimal amount)
    {
        Label label = new()
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E1E1E")
        };

        label.Text = amount < 0
            ? $"-{FormatMoney(Math.Abs(amount))}"
            : FormatMoney(amount);

        if (amount < 0)
            label.TextColor = Color.FromArgb("#C0392B");

        return label;
    }

    // =====================================================
    // STATUS COLOURS
    // =====================================================

    private Color GetStatusColor(string? status)
    {
        string value =
            status?.Trim().ToLowerInvariant() ?? string.Empty;

        return value switch
        {
            "safe" => Color.FromArgb("#146B3A"),
            "info" => Color.FromArgb("#1565C0"),
            "warning" => Color.FromArgb("#C47F00"),
            "high warning" => Color.FromArgb("#D35400"),
            "budget reached" => Color.FromArgb("#C0392B"),
            "over budget" => Color.FromArgb("#B42318"),
            _ => Color.FromArgb("#555555")
        };
    }

    private Color GetStatusBackgroundColor(string? status)
    {
        string value =
            status?.Trim().ToLowerInvariant() ?? string.Empty;

        return value switch
        {
            "safe" => Color.FromArgb("#EEF7F1"),
            "info" => Color.FromArgb("#EEF5FC"),
            "warning" => Color.FromArgb("#FFF8E8"),
            "high warning" => Color.FromArgb("#FFF3E8"),
            "budget reached" => Color.FromArgb("#FFF1F0"),
            "over budget" => Color.FromArgb("#FFF1F0"),
            _ => Color.FromArgb("#F7F7F7")
        };
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private string FormatMoney(decimal amount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "R {0:N2}",
            amount);
    }

    private string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
            return string.Empty;

        return _months[month - 1];
    }

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;

        LoadingSection.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;

        LoadInsightsButton.IsEnabled = !isLoading;
        RefreshButton.IsEnabled = !isLoading;

        MonthPicker.IsEnabled = !isLoading;
        YearEntry.IsEnabled = !isLoading;
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

    private void ClearInsights()
    {
        InsightsSection.IsVisible = false;

        TotalBudgetLabel.Text = "R 0.00";
        TotalSpentLabel.Text = "R 0.00";

        TotalRemainingLabel.Text = "R 0.00";
        TotalRemainingLabel.TextColor = Colors.White;

        AvailableBudgetLabel.Text = "R 0.00";
        AvailableBudgetLabel.TextColor = Colors.White;

        PercentageUsedLabel.Text = "0%";
        BudgetProgressBar.Progress = 0;

        GeneralInsightsLayout.Children.Clear();
        CategoryInsightsLayout.Children.Clear();

        NoGeneralInsightsLabel.IsVisible = false;
        NoCategoryInsightsLabel.IsVisible = false;
    }

    // =====================================================
    // BUTTONS
    // =====================================================

    private async void BackButton_Clicked(
        object sender, EventArgs e)
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
        object sender, EventArgs e)
    {
        await LoadInsightsAsync();
    }

    private async void LoadInsightsButton_Clicked(
        object sender, EventArgs e)
    {
        await LoadInsightsAsync();
    }
}
