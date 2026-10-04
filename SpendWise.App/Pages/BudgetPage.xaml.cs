using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;

namespace SpendWise.App.Pages;

public partial class BudgetPage : ContentPage
{
    private readonly ApiService _apiService;

    private readonly int _year;
    private readonly int _month;

    private bool _isLoading;


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public BudgetPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        DateTime today =
            DateTime.Now;

        _year =
            today.Year;

        _month =
            today.Month;

        MonthLabel.Text =
            today.ToString(
                "MMMM yyyy");
    }


    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadBudgetAsync();
    }


    // ==========================================
    // LOAD BUDGET
    // ==========================================

    private async Task LoadBudgetAsync()
    {
        if (_isLoading)
        {
            return;
        }

        try
        {
            _isLoading = true;

            var result =
                await _apiService
                    .GetBudgetAsync(
                        _year,
                        _month);

            if (!result.Success ||
                result.Data == null)
            {
                ShowCreateBudgetMode();

                return;
            }

            ShowExistingBudget(
                result.Data);
        }
        catch (Exception ex)
        {
            ShowCreateBudgetMode();

            ShowError(
                $"Unable to load budget: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }


    // ==========================================
    // CREATE MODE
    // ==========================================

    private void ShowCreateBudgetMode()
    {
        NoBudgetCard.IsVisible =
            true;

        CreateBudgetSection.IsVisible =
            true;

        ExistingCategoriesSection.IsVisible =
            false;

        TotalBudgetLabel.Text =
            "R 0.00";

        TotalSpentLabel.Text =
            "R 0.00";

        TotalRemainingLabel.Text =
            "R 0.00";
    }


    // ==========================================
    // DISPLAY EXISTING BUDGET
    // ==========================================

    private void ShowExistingBudget(
        BudgetResponse budget)
    {
        NoBudgetCard.IsVisible =
            false;

        CreateBudgetSection.IsVisible =
            false;

        ExistingCategoriesSection.IsVisible =
            true;


        // ======================================
        // SUMMARY
        // ======================================

        TotalBudgetLabel.Text =
            $"R {budget.TotalLimit:N2}";

        TotalSpentLabel.Text =
            $"R {budget.TotalSpent:N2}";

        TotalRemainingLabel.Text =
            $"R {budget.Remaining:N2}";


        // ======================================
        // CATEGORY CARDS
        // ======================================

        CategoriesLayout.Children.Clear();

        if (budget.Categories == null ||
            budget.Categories.Count == 0)
        {
            CategoriesLayout.Children.Add(
                new Label
                {
                    Text =
                        "No category budgets were created.",

                    TextColor =
                        Color.FromArgb("#777777")
                });

            return;
        }


        foreach (var category
                 in budget.Categories)
        {
            CategoriesLayout.Children.Add(
                CreateCategoryCard(
                    category));
        }
    }


    // ==========================================
    // CREATE CATEGORY DISPLAY CARD
    // ==========================================

    private View CreateCategoryCard(
        BudgetCategoryResponse category)
    {
        decimal percentage = 0;

        if (category.Limit > 0)
        {
            percentage =
                (category.Spent /
                 category.Limit) * 100;
        }


        double progress =
            category.Limit > 0
                ? (double)(
                    category.Spent /
                    category.Limit)
                : 0;


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        var titleLabel =
            new Label
            {
                Text =
                    category.Category,

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb("#1E1E1E")
            };


        var percentageLabel =
            new Label
            {
                Text =
                    $"{percentage:N0}%",

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    GetPercentageColor(
                        percentage),

                HorizontalOptions =
                    LayoutOptions.End
            };


        var headerGrid =
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
            percentageLabel,
            1);


        headerGrid.Children.Add(
            titleLabel);

        headerGrid.Children.Add(
            percentageLabel);


        var progressBar =
            new ProgressBar
            {
                Progress =
                    progress,

                HeightRequest =
                    7
            };


        var amountGrid =
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


        var spentLabel =
            new Label
            {
                Text =
                    $"Spent: R {category.Spent:N2}",

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb("#666666")
            };


        var remainingLabel =
            new Label
            {
                Text =
                    $"R {category.Remaining:N2} left",

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb("#666666"),

                HorizontalOptions =
                    LayoutOptions.End
            };


        Grid.SetColumn(
            spentLabel,
            0);

        Grid.SetColumn(
            remainingLabel,
            1);


        amountGrid.Children.Add(
            spentLabel);

        amountGrid.Children.Add(
            remainingLabel);


        var limitLabel =
            new Label
            {
                Text =
                    $"Limit: R {category.Limit:N2}",

                FontSize =
                    12,

                TextColor =
                    Color.FromArgb("#777777")
            };


        var content =
            new VerticalStackLayout
            {
                Spacing =
                    9,

                Children =
                {
                    headerGrid,
                    limitLabel,
                    progressBar,
                    amountGrid
                }
            };


        return new Border
        {
            BackgroundColor =
                Colors.White,

            Stroke =
                Color.FromArgb("#E1E5E3"),

            StrokeThickness =
                1,

            Padding =
                16,

            StrokeShape =
                new Microsoft.Maui.Controls.Shapes
                    .RoundRectangle
                {
                    CornerRadius =
                        new CornerRadius(14)
                },

            Content =
                content
        };
    }


    // ==========================================
    // CATEGORY COLOUR
    // ==========================================

    private Color GetPercentageColor(
        decimal percentage)
    {
        if (percentage > 100)
        {
            return Color.FromArgb(
                "#C0392B");
        }

        if (percentage >= 90)
        {
            return Color.FromArgb(
                "#D35400");
        }

        if (percentage >= 75)
        {
            return Color.FromArgb(
                "#D68910");
        }

        return Color.FromArgb(
            "#146B3A");
    }


    // ==========================================
    // UPDATE ALLOCATION TOTAL
    // ==========================================

    private void BudgetEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        decimal totalLimit =
            GetAmount(
                TotalLimitEntry.Text);

        decimal allocated =
            GetAmount(
                GroceriesEntry.Text)
            +
            GetAmount(
                TransportEntry.Text)
            +
            GetAmount(
                EntertainmentEntry.Text)
            +
            GetAmount(
                UtilitiesEntry.Text)
            +
            GetAmount(
                OtherEntry.Text);


        decimal unallocated =
            totalLimit -
            allocated;


        AllocatedLabel.Text =
            $"R {allocated:N2}";

        UnallocatedLabel.Text =
            $"R {unallocated:N2}";


        if (unallocated < 0)
        {
            UnallocatedLabel.TextColor =
                Color.FromArgb("#C0392B");
        }
        else
        {
            UnallocatedLabel.TextColor =
                Color.FromArgb("#146B3A");
        }
    }


    // ==========================================
    // SAVE BUDGET
    // ==========================================

    private async void SaveBudgetButton_Clicked(
        object sender,
        EventArgs e)
    {
        ErrorLabel.IsVisible =
            false;


        decimal totalLimit =
            GetAmount(
                TotalLimitEntry.Text);


        if (totalLimit <= 0)
        {
            ShowError(
                "Enter a valid monthly budget.");

            return;
        }


        decimal groceries =
            GetAmount(
                GroceriesEntry.Text);

        decimal transport =
            GetAmount(
                TransportEntry.Text);

        decimal entertainment =
            GetAmount(
                EntertainmentEntry.Text);

        decimal utilities =
            GetAmount(
                UtilitiesEntry.Text);

        decimal other =
            GetAmount(
                OtherEntry.Text);


        decimal categoryTotal =
            groceries +
            transport +
            entertainment +
            utilities +
            other;


        if (categoryTotal >
            totalLimit)
        {
            ShowError(
                "Category limits cannot exceed your total monthly budget.");

            return;
        }


        // ======================================
        // CREATE REQUEST
        // ======================================

        var request =
            new CreateBudgetRequest
            {
                Month =
                    _month,

                Year =
                    _year,

                TotalLimit =
                    totalLimit,

                Categories =
                    new List<CreateBudgetCategoryRequest>
                    {
                        new()
                        {
                            Category =
                                "Groceries",

                            LimitAmount =
                                groceries
                        },

                        new()
                        {
                            Category =
                                "Transport",

                            LimitAmount =
                                transport
                        },

                        new()
                        {
                            Category =
                                "Entertainment",

                            LimitAmount =
                                entertainment
                        },

                        new()
                        {
                            Category =
                                "Utilities",

                            LimitAmount =
                                utilities
                        },

                        new()
                        {
                            Category =
                                "Other",

                            LimitAmount =
                                other
                        }
                    }
            };


        try
        {
            SaveBudgetButton.IsEnabled =
                false;

            SaveBudgetButton.Text =
                "Saving...";


            var result =
                await _apiService
                    .CreateBudgetAsync(
                        request);


            if (!result.Success)
            {
                ShowError(
                    result.Message);

                return;
            }


            await DisplayAlert(
                "Budget Created",
                result.Message,
                "OK");


            // Reload the page using the
            // budget now stored in MySQL.

            await LoadBudgetAsync();
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to save budget: {ex.Message}");
        }
        finally
        {
            SaveBudgetButton.IsEnabled =
                true;

            SaveBudgetButton.Text =
                "Save Monthly Budget";
        }
    }


    // ==========================================
    // GET DECIMAL FROM ENTRY
    // ==========================================

    private decimal GetAmount(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return 0;
        }


        if (decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal amount))
        {
            return amount;
        }


        if (decimal.TryParse(
            value,
            out amount))
        {
            return amount;
        }


        return 0;
    }


    // ==========================================
    // ERROR
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
    // REFRESH
    // ==========================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadBudgetAsync();
    }


    // ==========================================
    // BACK
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


    // ==========================================
    // HOME
    // ==========================================

    private async void HomeButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopToRootAsync();
        }
        else
        {
            Application.Current!.MainPage =
                new NavigationPage(
                    new DashboardPage());
        }
    }


    // ==========================================
    // GROCERY
    // ==========================================

    private async void GroceryButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new GroceryPage());
    }


    // ==========================================
    // PROFILE
    // ==========================================

    private async void ProfileButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ProfilePage());
    }
}