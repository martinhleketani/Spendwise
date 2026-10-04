using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;
using System.Xml.Linq;

namespace SpendWise.App.Pages;

public partial class GroceryPage : ContentPage
{
    private readonly ApiService _apiService;

    private readonly List<LocalGroceryItem> _items =
        new();

    private int? _savedGroceryListId;

    private bool _isSaving;


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public GroceryPage()
    {
        InitializeComponent();

        _apiService =
            new ApiService();

        RetailerPicker.SelectedIndex = 0;

        UpdateSummary();

        DisplayItems();
    }


    // ==========================================
    // PAGE APPEARING
    // ==========================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadGroceryListsAsync();
    }


    // ==========================================
    // ADD ITEM LOCALLY
    // ==========================================

    private void AddItemButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();

        string productName =
            ProductNameEntry.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            productName))
        {
            ShowError(
                "Please enter a product name.");

            return;
        }


        int quantity =
            GetQuantity(
                QuantityEntry.Text);


        if (quantity <= 0)
        {
            ShowError(
                "Quantity must be greater than 0.");

            return;
        }


        decimal price =
            GetDecimal(
                PriceEntry.Text);


        if (price <= 0)
        {
            ShowError(
                "Price must be greater than R0.");

            return;
        }


        LocalGroceryItem item =
            new()
            {
                LocalId =
                    Guid.NewGuid(),

                ProductName =
                    productName,

                Quantity =
                    quantity,

                UnitPrice =
                    price
            };


        _items.Add(item);


        ProductNameEntry.Text =
            string.Empty;

        QuantityEntry.Text =
            "1";

        PriceEntry.Text =
            string.Empty;


        DisplayItems();

        UpdateSummary();
    }


    // ==========================================
    // DISPLAY ITEMS
    // ==========================================

    private void DisplayItems()
    {
        ItemsLayout.Children.Clear();


        if (_items.Count == 0)
        {
            ItemsLayout.Children.Add(
                new Label
                {
                    Text =
                        "No items added yet.",

                    TextColor =
                        Color.FromArgb(
                            "#777777"),

                    HorizontalOptions =
                        LayoutOptions.Center,

                    Margin =
                        new Thickness(
                            0,
                            20)
                });

            return;
        }


        foreach (LocalGroceryItem item
                 in _items)
        {
            Grid itemView =
                CreateItemView(item);

            ItemsLayout.Children.Add(
                itemView);


            if (item != _items.Last())
            {
                ItemsLayout.Children.Add(
                    new BoxView
                    {
                        HeightRequest = 1,

                        BackgroundColor =
                            Color.FromArgb(
                                "#EEEEEE"),

                        Margin =
                            new Thickness(
                                0,
                                7)
                    });
            }
        }
    }


    // ==========================================
    // CREATE ITEM VIEW
    // ==========================================

    private Grid CreateItemView(
        LocalGroceryItem item)
    {
        Grid grid =
            new()
            {
                Padding =
                    new Thickness(
                        0,
                        8),

                ColumnSpacing = 10
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


        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });


        VerticalStackLayout information =
            new()
            {
                Spacing = 3
            };


        Label productLabel =
            new()
            {
                Text =
                    item.ProductName,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        Label quantityLabel =
            new()
            {
                Text =
                    $"{item.Quantity} × R {item.UnitPrice:N2}",

                FontSize = 12,

                TextColor =
                    Color.FromArgb(
                        "#777777")
            };


        information.Children.Add(
            productLabel);

        information.Children.Add(
            quantityLabel);


        Label totalLabel =
            new()
            {
                Text =
                    $"R {item.Total:N2}",

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#146B3A"),

                VerticalOptions =
                    LayoutOptions.Center
            };


        Button removeButton =
            new()
            {
                Text = "✕",

                FontSize = 14,

                TextColor =
                    Color.FromArgb(
                        "#C0392B"),

                BackgroundColor =
                    Colors.Transparent,

                WidthRequest = 42,

                HeightRequest = 42,

                Padding = 0,

                CommandParameter =
                    item.LocalId
            };


        removeButton.Clicked +=
            RemoveItemButton_Clicked;


        Grid.SetColumn(
            information,
            0);

        Grid.SetColumn(
            totalLabel,
            1);

        Grid.SetColumn(
            removeButton,
            2);


        grid.Children.Add(
            information);

        grid.Children.Add(
            totalLabel);

        grid.Children.Add(
            removeButton);


        return grid;
    }


    // ==========================================
    // REMOVE ITEM
    // ==========================================

    private async void RemoveItemButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }


        if (button.CommandParameter
            is not Guid localId)
        {
            return;
        }


        LocalGroceryItem? item =
            _items.FirstOrDefault(
                x => x.LocalId == localId);


        if (item == null)
        {
            return;
        }


        if (_savedGroceryListId.HasValue &&
            item.DatabaseId.HasValue)
        {
            var result =
                await _apiService
                    .DeleteGroceryItemAsync(
                        _savedGroceryListId.Value,
                        item.DatabaseId.Value);


            if (!result.Success)
            {
                ShowError(
                    result.Message);

                return;
            }
        }


        _items.Remove(item);

        DisplayItems();

        UpdateSummary();

        await LoadGroceryListsAsync();
    }


    // ==========================================
    // CLEAR ITEMS
    // ==========================================

    private async void ClearItemsButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        if (_items.Count == 0)
        {
            return;
        }


        bool clear =
            await DisplayAlert(
                "Clear Grocery List",
                "Remove all items from this list?",
                "Clear",
                "Cancel");


        if (!clear)
        {
            return;
        }


        if (!_savedGroceryListId.HasValue)
        {
            _items.Clear();

            DisplayItems();

            UpdateSummary();

            return;
        }


        List<LocalGroceryItem> savedItems =
            _items
                .Where(
                    x =>
                        x.DatabaseId.HasValue)
                .ToList();


        foreach (LocalGroceryItem item
                 in savedItems)
        {
            var result =
                await _apiService
                    .DeleteGroceryItemAsync(
                        _savedGroceryListId.Value,
                        item.DatabaseId!.Value);


            if (!result.Success)
            {
                ShowError(
                    result.Message);

                return;
            }
        }


        _items.Clear();

        DisplayItems();

        UpdateSummary();

        await LoadGroceryListsAsync();
    }


    // ==========================================
    // SAVE LIST
    // ==========================================

    private async void SaveListButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_isSaving)
        {
            return;
        }


        HideError();


        string listName =
            ListNameEntry.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            listName))
        {
            ShowError(
                "Please enter a grocery list name.");

            return;
        }


        string retailer =
            RetailerPicker.SelectedItem
                ?.ToString()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            retailer))
        {
            ShowError(
                "Please select a retailer.");

            return;
        }


        if (_items.Count == 0)
        {
            ShowError(
                "Add at least one grocery item.");

            return;
        }


        try
        {
            _isSaving = true;

            SaveListButton.IsEnabled =
                false;

            SaveListButton.Text =
                "Saving...";


            // CREATE LIST FIRST
            if (!_savedGroceryListId.HasValue)
            {
                CreateGroceryListRequest
                    createRequest =
                    new()
                    {
                        ListName =
                            listName,

                        Retailer =
                            retailer
                    };


                var createResult =
                    await _apiService
                        .CreateGroceryListAsync(
                            createRequest);


                if (!createResult.Success ||
                    createResult.Data == null)
                {
                    ShowError(
                        createResult.Message);

                    return;
                }


                _savedGroceryListId =
                    createResult.Data.Id;
            }


            // ADD UNSAVED ITEMS
            foreach (LocalGroceryItem item
                     in _items.Where(
                         x =>
                             !x.DatabaseId.HasValue))
            {
                AddGroceryItemRequest request =
                    new()
                    {
                        ProductName =
                            item.ProductName,

                        Quantity =
                            item.Quantity,

                        UnitPrice =
                            item.UnitPrice
                    };


                var itemResult =
                    await _apiService
                        .AddGroceryItemAsync(
                            _savedGroceryListId.Value,
                            request);


                if (!itemResult.Success ||
                    itemResult.Data == null)
                {
                    ShowError(
                        itemResult.Message);

                    return;
                }


                item.DatabaseId =
                    itemResult.Data.Item.Id;
            }


            await DisplayAlert(
                "Saved",
                "Your grocery list and items were saved successfully.",
                "OK");


            SaveListButton.Text =
                "Saved";


            await LoadGroceryListsAsync();
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to save grocery list: {ex.Message}");
        }
        finally
        {
            _isSaving = false;

            SaveListButton.IsEnabled =
                true;


            if (SaveListButton.Text ==
                "Saving...")
            {
                SaveListButton.Text =
                    "Save Grocery List";
            }
        }
    }


    // ==========================================
    // LOAD ALL SAVED LISTS
    // ==========================================

    private async Task LoadGroceryListsAsync()
    {
        HideError();

        ListsLoadingIndicator.IsVisible =
            true;

        ListsLoadingIndicator.IsRunning =
            true;


        try
        {
            var result =
                await _apiService
                    .GetGroceryListsAsync();


            SavedListsLayout.Children.Clear();


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                ShowNoSavedLists(
                    "Unable to load saved grocery lists.");

                return;
            }


            if (result.Data.Count == 0)
            {
                ShowNoSavedLists(
                    "No saved grocery lists yet.");

                return;
            }


            foreach (GroceryListSummary list
                     in result.Data)
            {
                SavedListsLayout.Children.Add(
                    CreateSavedListView(
                        list));
            }
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to load grocery lists: {ex.Message}");
        }
        finally
        {
            ListsLoadingIndicator.IsRunning =
                false;

            ListsLoadingIndicator.IsVisible =
                false;
        }
    }


    // ==========================================
    // CREATE SAVED LIST CARD
    // ==========================================

    private View CreateSavedListView(
        GroceryListSummary list)
    {
        Border border =
            new()
            {
                BackgroundColor =
                    Color.FromArgb(
                        "#F8FAF9"),

                Stroke =
                    Color.FromArgb(
                        "#E1E5E3"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(14),

                StrokeShape =
                    new Microsoft.Maui
                        .Controls.Shapes
                        .RoundRectangle
                    {
                        CornerRadius = 12
                    }
            };


        Grid grid =
            new()
            {
                ColumnSpacing = 12
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


        VerticalStackLayout information =
            new()
            {
                Spacing = 3
            };


        Label nameLabel =
            new()
            {
                Text =
                    list.ListName,

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#1E1E1E")
            };


        Label retailerLabel =
            new()
            {
                Text =
                    $"{list.Retailer} • " +
                    $"{list.ItemCount} item(s)",

                FontSize = 12,

                TextColor =
                    Color.FromArgb(
                        "#777777")
            };


        Label statusLabel =
            new()
            {
                Text =
                    $"Status: {list.Status}",

                FontSize = 12,

                TextColor =
                    list.Status.Equals(
                        "Completed",
                        StringComparison
                            .OrdinalIgnoreCase)
                    ? Color.FromArgb(
                        "#777777")
                    : Color.FromArgb(
                        "#146B3A")
            };


        information.Children.Add(
            nameLabel);

        information.Children.Add(
            retailerLabel);

        information.Children.Add(
            statusLabel);


        VerticalStackLayout actions =
            new()
            {
                Spacing = 5,

                HorizontalOptions =
                    LayoutOptions.End
            };


        Label totalLabel =
            new()
            {
                Text =
                    $"R {list.Total:N2}",

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb(
                        "#146B3A"),

                HorizontalTextAlignment =
                    TextAlignment.End
            };


        Button openButton =
            new()
            {
                Text = "Open",

                FontSize = 12,

                BackgroundColor =
                    Color.FromArgb(
                        "#146B3A"),

                TextColor =
                    Colors.White,

                CornerRadius = 10,

                HeightRequest = 38,

                CommandParameter =
                    list.Id
            };


        openButton.Clicked +=
            OpenSavedListButton_Clicked;


        actions.Children.Add(
            totalLabel);

        actions.Children.Add(
            openButton);


        Grid.SetColumn(
            information,
            0);

        Grid.SetColumn(
            actions,
            1);


        grid.Children.Add(
            information);

        grid.Children.Add(
            actions);


        border.Content =
            grid;


        return border;
    }


    // ==========================================
    // OPEN SAVED LIST
    // ==========================================

    private async void OpenSavedListButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }


        if (button.CommandParameter
            is not int listId)
        {
            return;
        }


        HideError();

        _savedGroceryListId =
            listId;


        await LoadSavedListAsync(
            listId);
    }


    // ==========================================
    // LOAD ONE SAVED LIST
    // ==========================================

    private async Task LoadSavedListAsync(
        int groceryListId)
    {
        var result =
            await _apiService
                .GetGroceryListAsync(
                    groceryListId);


        if (!result.Success ||
            result.Data == null)
        {
            ShowError(
                result.Message);

            return;
        }


        GroceryListResponse list =
            result.Data;


        _savedGroceryListId =
            list.Id;


        ListNameEntry.Text =
            list.ListName;


        int retailerIndex =
            GetRetailerIndex(
                list.Retailer);


        if (retailerIndex >= 0)
        {
            RetailerPicker.SelectedIndex =
                retailerIndex;
        }


        _items.Clear();


        foreach (GroceryItemResponse item
                 in list.Items)
        {
            _items.Add(
                new LocalGroceryItem
                {
                    LocalId =
                        Guid.NewGuid(),

                    DatabaseId =
                        item.Id,

                    ProductName =
                        item.ProductName,

                    Quantity =
                        item.Quantity,

                    UnitPrice =
                        item.UnitPrice
                });
        }


        DisplayItems();

        UpdateSummary();


        // COMPLETED LISTS CANNOT BE MODIFIED
        bool isActive =
            list.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase);


        SaveListButton.IsEnabled =
            isActive;

        CheckBudgetButton.IsEnabled =
            isActive;

        CheckoutButton.IsEnabled =
            isActive;


        if (isActive)
        {
            SaveListButton.Text =
                "Save Grocery List";

            CheckoutButton.Text =
                "Record Purchase";
        }
        else
        {
            SaveListButton.Text =
                "Completed";

            CheckoutButton.Text =
                "Completed";
        }
    }


    // ==========================================
    // REFRESH SAVED LISTS BUTTON
    // ==========================================

    private async void LoadListsButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadGroceryListsAsync();
    }


    // ==========================================
    // EMPTY SAVED LIST MESSAGE
    // ==========================================

    private void ShowNoSavedLists(
        string message)
    {
        SavedListsLayout.Children.Clear();


        SavedListsLayout.Children.Add(
            new Label
            {
                Text =
                    message,

                TextColor =
                    Color.FromArgb(
                        "#777777"),

                HorizontalOptions =
                    LayoutOptions.Center,

                Margin =
                    new Thickness(
                        0,
                        10)
            });
    }


    // ==========================================
    // NEW GROCERY LIST
    // ==========================================

    private void NewListButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        _savedGroceryListId =
            null;


        _items.Clear();


        ListNameEntry.Text =
            string.Empty;

        RetailerPicker.SelectedIndex =
            0;

        ProductNameEntry.Text =
            string.Empty;

        QuantityEntry.Text =
            "1";

        PriceEntry.Text =
            string.Empty;


        SaveListButton.IsEnabled =
            true;

        SaveListButton.Text =
            "Save Grocery List";


        CheckBudgetButton.IsEnabled =
            true;


        CheckoutButton.IsEnabled =
            true;

        CheckoutButton.Text =
            "Record Purchase";


        DisplayItems();

        UpdateSummary();
    }


    // ==========================================
    // ==========================================
    // CHECK GROCERY BUDGET
    // ==========================================

    private async void CheckBudgetButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        if (!_savedGroceryListId.HasValue)
        {
            await DisplayAlert(
                "Save First",
                "Please save the grocery list before checking your budget.",
                "OK");

            return;
        }


        DateTime today =
            DateTime.Now;


        CheckBudgetButton.IsEnabled =
            false;

        CheckBudgetButton.Text =
            "Checking...";


        try
        {
            var result =
                await _apiService
                    .CheckGroceryBudgetAsync(
                        _savedGroceryListId.Value,
                        today.Month,
                        today.Year);


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                return;
            }


            GroceryBudgetCheckResponse data =
                result.Data;


            string message =
                $"List Total: R {data.ListTotal:N2}\n\n" +
                $"Grocery Budget: R {data.GroceryBudgetLimit:N2}\n" +
                $"Already Spent: R {data.AlreadySpent:N2}\n" +
                $"Available: R {data.AvailableBeforeList:N2}\n\n";


            if (data.WithinBudget)
            {
                message +=
                    "Within Budget\n\n" +
                    "Remaining After List: " +
                    $"R {data.RemainingAfterList:N2}";
            }
            else
            {
                message +=
                    "Over Budget\n\n" +
                    $"Over By: R {data.OverBudgetBy:N2}";
            }


            await DisplayAlert(
                "Budget Check",
                message,
                "OK");
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to check budget: {ex.Message}");
        }
        finally
        {
            CheckBudgetButton.IsEnabled =
                true;

            CheckBudgetButton.Text =
                "Check Grocery Budget";
        }
    }


    // ==========================================
    // RECORD GROCERY PURCHASE
    // ==========================================

    private async void CheckoutButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        // LIST MUST BE SAVED FIRST
        if (!_savedGroceryListId.HasValue)
        {
            await DisplayAlert(
                "Save First",
                "Please save your grocery list before recording the purchase.",
                "OK");

            return;
        }


        // LIST MUST HAVE ITEMS
        if (_items.Count == 0)
        {
            ShowError(
                "Your grocery list is empty.");

            return;
        }


        // CALCULATE TOTAL
        decimal total =
            _items.Sum(
                x => x.Total);


        // CONFIRM RECORDING
        bool confirm =
            await DisplayAlert(
                "Record Grocery Purchase",
                $"Record this grocery purchase of R {total:N2}?\n\n" +
                "SpendWise will add it to your spending history " +
                "and update your grocery budget.\n\n" +
                "No bank or card payment will be processed.",
                "Record Purchase",
                "Cancel");


        if (!confirm)
        {
            return;
        }


        DateTime today =
            DateTime.Now;


        CheckoutButton.IsEnabled =
            false;

        CheckoutButton.Text =
            "Recording...";


        try
        {
            // The service method still has the old
            // CheckoutGroceryAsync name for compatibility.
            // The backend now records spending only.

            var result =
                await _apiService
                    .CheckoutGroceryAsync(
                        _savedGroceryListId.Value,
                        today.Month,
                        today.Year);


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                CheckoutButton.IsEnabled =
                    true;

                CheckoutButton.Text =
                    "Record Purchase";

                return;
            }


            GroceryCheckoutResponse data =
                result.Data;


            string message =
                $"Purchase Recorded: R {data.PurchaseTotal:N2}\n\n" +
                $"Grocery Budget Remaining: " +
                $"R {data.GroceryBudgetRemaining:N2}\n\n" +
                $"Budget Status: {data.BudgetStatus}\n" +
                $"Reference: {data.TransactionReference}\n\n" +
                "No bank or card payment was processed by SpendWise.";


            await DisplayAlert(
                "Purchase Recorded",
                message,
                "OK");


            // MARK LIST COMPLETED
            CheckoutButton.Text =
                "Completed";

            CheckoutButton.IsEnabled =
                false;


            SaveListButton.Text =
                "Completed";

            SaveListButton.IsEnabled =
                false;


            CheckBudgetButton.IsEnabled =
                false;


            await LoadGroceryListsAsync();
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to record purchase: {ex.Message}");

            CheckoutButton.IsEnabled =
                true;

            CheckoutButton.Text =
                "Record Purchase";
        }
    }


    // ==========================================
    // PRICE COMPARISON
    // ==========================================

    private async void ComparePricesButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        // LIST MUST BE SAVED FIRST
        if (!_savedGroceryListId.HasValue)
        {
            await DisplayAlert(
                "Save First",
                "Please save the grocery list before comparing retailer prices.",
                "OK");

            return;
        }


        // LIST MUST HAVE ITEMS
        if (_items.Count == 0)
        {
            ShowError(
                "Your grocery list is empty.");

            return;
        }


        Button? compareButton =
            sender as Button;


        if (compareButton != null)
        {
            compareButton.IsEnabled =
                false;

            compareButton.Text =
                "Comparing...";
        }


        try
        {
            var result =
                await _apiService
                    .GetRetailerPriceComparisonAsync(
                        _savedGroceryListId.Value);


            if (!result.Success ||
                result.Data == null)
            {
                ShowError(
                    result.Message);

                return;
            }


            RetailerPriceComparisonResponse comparison =
                result.Data;


            if (comparison.Retailers == null ||
                comparison.Retailers.Count == 0)
            {
                await DisplayAlert(
                    "Retailer Price Comparison",
                    "No active retailers are currently available for price comparison.",
                    "OK");

                return;
            }


            string message =
                $"{comparison.GroceryListName}\n\n";


            foreach (
                RetailerComparisonResponse retailer
                in comparison.Retailers)
            {
                message +=
                    $"{retailer.RetailerName}\n";


                message +=
                    $"Matched: {retailer.MatchedItemCount}\n";


                message +=
                    $"Missing: {retailer.MissingItemCount}\n";


                if (retailer.MatchedItemCount > 0)
                {
                    message +=
                        $"Estimated Total: R {retailer.EstimatedTotal:N2}\n";
                }
                else
                {
                    message +=
                        "Estimated Total: Not available\n";
                }


                message +=
                    retailer.HasCompletePricing
                        ? "Pricing: Complete\n"
                        : "Pricing: Incomplete\n";


                if (retailer.LatestPriceCheckedAt.HasValue)
                {
                    message +=
                        "Prices Checked: " +
                        retailer.LatestPriceCheckedAt
                            .Value
                            .ToLocalTime()
                            .ToString(
                                "dd MMM yyyy HH:mm") +
                        "\n";
                }


                if (retailer.Items != null &&
                    retailer.Items.Count > 0)
                {
                    message +=
                        "\nItems:\n";


                    foreach (
                        RetailerComparisonItemResponse item
                        in retailer.Items)
                    {
                        if (item.PriceFound &&
                            item.UnitPrice.HasValue)
                        {
                            message +=
                                $"• {item.ProductName} " +
                                $"x{item.Quantity}: " +
                                $"R {item.UnitPrice.Value:N2} each";


                            if (item.TotalPrice.HasValue)
                            {
                                message +=
                                    $" = R {item.TotalPrice.Value:N2}";
                            }


                            if (!string.IsNullOrWhiteSpace(
                                item.Brand))
                            {
                                message +=
                                    $" ({item.Brand}";


                                if (!string.IsNullOrWhiteSpace(
                                    item.Size))
                                {
                                    message +=
                                        $" - {item.Size}";
                                }


                                message +=
                                    ")";
                            }
                            else if (!string.IsNullOrWhiteSpace(
                                item.Size))
                            {
                                message +=
                                    $" ({item.Size})";
                            }


                            message +=
                                "\n";
                        }
                        else
                        {
                            message +=
                                $"• {item.ProductName} " +
                                $"x{item.Quantity}: " +
                                "Price not available\n";
                        }
                    }
                }


                message +=
                    "\n-------------------------\n\n";
            }


            message +=
                "Only verified retailer prices stored in SpendWise are shown. " +
                "Missing prices are not estimated or invented.";


            await DisplayAlert(
                "Retailer Price Comparison",
                message.Trim(),
                "OK");
        }
        catch (Exception ex)
        {
            ShowError(
                $"Unable to compare retailer prices: {ex.Message}");
        }
        finally
        {
            if (compareButton != null)
            {
                compareButton.IsEnabled =
                    true;

                compareButton.Text =
                    "Compare Retailer Prices";
            }
        }
    }


    // ==========================================
    // REFRESH PAGE
    // ==========================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        HideError();


        await LoadGroceryListsAsync();


        if (_savedGroceryListId.HasValue)
        {
            await LoadSavedListAsync(
                _savedGroceryListId.Value);
        }
    }


    // ==========================================
    // UPDATE TOTAL
    // ==========================================

    private void UpdateSummary()
    {
        int quantity =
            _items.Sum(
                x => x.Quantity);


        decimal total =
            _items.Sum(
                x => x.Total);


        ItemCountLabel.Text =
            quantity.ToString();


        EstimatedTotalLabel.Text =
            $"R {total:N2}";


        ListTotalLabel.Text =
            $"R {total:N2}";
    }


    // ==========================================
    // GET RETAILER INDEX
    // ==========================================

    private int GetRetailerIndex(
        string retailer)
    {
        for (int i = 0;
             i < RetailerPicker.Items.Count;
             i++)
        {
            if (RetailerPicker.Items[i]
                .Equals(
                    retailer,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }


        return -1;
    }


    // ==========================================
    // READ QUANTITY
    // ==========================================

    private int GetQuantity(
        string? value)
    {
        if (int.TryParse(
            value,
            out int quantity))
        {
            return quantity;
        }


        return 0;
    }


    // ==========================================
    // READ PRICE
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
            value.Trim()
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
    // LOCAL UI ITEM
    // ==========================================

    private class LocalGroceryItem
    {
        public Guid LocalId { get; set; }

        public int? DatabaseId { get; set; }

        public string ProductName { get; set; } =
            string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Total =>
            Quantity * UnitPrice;
    }
}
// ==========================================