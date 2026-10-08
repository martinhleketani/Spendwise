
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Linq;

namespace SpendWise.App.Pages;

public partial class MyGroceryListsPage : ContentPage
{
    private readonly ApiService _apiService = new();

    private readonly List<GroceryListSummary> _lists = new();

    private bool _isLoading;
    private bool _isDeleting;

    private static readonly Color Green =
        Color.FromArgb("#146B3A");

    private static readonly Color Dark =
        Color.FromArgb("#172B20");

    private static readonly Color Gray =
        Color.FromArgb("#68776E");

    public MyGroceryListsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadListsAsync();
    }

    // ==========================================
    // LOAD SAVED LISTS
    // ==========================================

    private async Task LoadListsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ErrorLabel.IsVisible = false;

        try
        {
            var result =
                await _apiService.GetGroceryListsAsync();

            if (!result.Success || result.Data == null)
            {
                ShowError(result.Message);
                return;
            }

            _lists.Clear();
            _lists.AddRange(result.Data);

            TotalListsLabel.Text = _lists.Count.ToString();

            ActiveListsLabel.Text =
                _lists.Count(x =>
                    IsActive(x.Status)).ToString();

            DisplayFilteredLists();
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to load grocery lists: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
        }
    }

    // ==========================================
    // SEARCH AND FILTER
    // ==========================================

    private void SearchEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        DisplayFilteredLists();
    }

    private void StatusPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        DisplayFilteredLists();
    }

    private void DisplayFilteredLists()
    {
        ListsLayout.Children.Clear();

        string search =
            SearchEntry.Text?.Trim() ?? "";

        string filter =
            StatusPicker.SelectedItem?.ToString()
            ?? "All Lists";

        var filtered = _lists
            .Where(list =>
                string.IsNullOrWhiteSpace(search) ||
                list.ListName.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                (list.Retailer ?? "").Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase))
            .Where(list =>
                filter == "All Lists" ||
                (filter == "Active" &&
                 IsActive(list.Status)) ||
                (filter == "Completed" &&
                 !IsActive(list.Status)))
            .ToList();

        ResultsCountLabel.Text =
            $"{filtered.Count} results";

        if (filtered.Count == 0)
        {
            ListsLayout.Children.Add(new Label
            {
                Text = _lists.Count == 0
                    ? "You have no saved grocery lists yet."
                    : "No grocery lists match your search.",
                TextColor = Gray,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 25)
            });

            return;
        }

        foreach (var list in filtered)
        {
            ListsLayout.Children.Add(
                CreateListCard(list));
        }
    }

    private static bool IsActive(string? status)
    {
        return string.Equals(
            status,
            "Active",
            StringComparison.OrdinalIgnoreCase);
    }

    // ==========================================
    // CREATE LIST CARD
    // ==========================================

    private View CreateListCard(GroceryListSummary list)
    {
        var card = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E4EAE6"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(14)
                }
        };

        var content = new VerticalStackLayout
        {
            Spacing = 12
        };

        var header = new Grid
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
            ColumnSpacing = 10
        };

        var title = new Label
        {
            Text = list.ListName,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Dark,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var status = new Label
        {
            Text = IsActive(list.Status)
                ? "Active"
                : "Completed",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = IsActive(list.Status)
                ? Green
                : Gray,
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(title, 0);
        Grid.SetColumn(status, 1);

        header.Children.Add(title);
        header.Children.Add(status);

        content.Children.Add(header);

        content.Children.Add(new Label
        {
            Text = $"Retailer: {list.Retailer}",
            TextColor = Gray,
            FontSize = 13
        });

        content.Children.Add(new Label
        {
            Text = $"{list.ItemCount} products",
            TextColor = Gray,
            FontSize = 13
        });

        content.Children.Add(new Label
        {
            Text = $"R {list.Total:N2}",
            FontSize = 23,
            FontAttributes = FontAttributes.Bold,
            TextColor = Green
        });

        var buttons = new Grid
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
                }
            },
            ColumnSpacing = 10
        };

        var openButton = new Button
        {
            Text = IsActive(list.Status)
                ? "View / Edit"
                : "View List",
            BackgroundColor = Green,
            TextColor = Colors.White,
            CornerRadius = 9,
            HeightRequest = 45,
            CommandParameter = list.Id
        };

        openButton.Clicked += OpenListButton_Clicked;

        var deleteButton = new Button
        {
            Text = "Delete",
            BackgroundColor = Color.FromArgb("#FCEDEA"),
            TextColor = Color.FromArgb("#B42318"),
            CornerRadius = 9,
            HeightRequest = 45,
            CommandParameter = list.Id
        };

        deleteButton.Clicked += DeleteListButton_Clicked;

        Grid.SetColumn(openButton, 0);
        Grid.SetColumn(deleteButton, 1);

        buttons.Children.Add(openButton);
        buttons.Children.Add(deleteButton);

        content.Children.Add(buttons);

        card.Content = content;

        return card;
    }

    // ==========================================
    // OPEN SAVED LIST
    // ==========================================

    private async void OpenListButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not int listId)
            return;

        await Navigation.PushAsync(
            new GroceryPage(listId));
    }

    // ==========================================
    // DELETE SAVED LIST
    // ==========================================

    private async void DeleteListButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isDeleting ||
            sender is not Button button ||
            button.CommandParameter is not int listId)
            return;

        var list = _lists.FirstOrDefault(
            x => x.Id == listId);

        if (list == null)
            return;

        bool confirm = await DisplayAlert(
            "Delete Grocery List",
            $"Are you sure you want to permanently delete '{list.ListName}'?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        _isDeleting = true;
        button.IsEnabled = false;

        try
        {
            var result =
                await _apiService.DeleteGroceryListAsync(
                    listId);

            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            _lists.RemoveAll(x => x.Id == listId);

            TotalListsLabel.Text = _lists.Count.ToString();

            ActiveListsLabel.Text =
                _lists.Count(x =>
                    IsActive(x.Status)).ToString();

            DisplayFilteredLists();

            await DisplayAlert(
                "Deleted",
                "Grocery list deleted successfully.",
                "OK");
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to delete list: {ex.Message}");
        }
        finally
        {
            _isDeleting = false;
            button.IsEnabled = true;
        }
    }

    // ==========================================
    // CREATE NEW LIST
    // ==========================================

    private async void CreateListButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new GroceryPage());
    }

    // ==========================================
    // REFRESH
    // ==========================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadListsAsync();
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

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
