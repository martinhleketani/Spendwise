
using SpendWise.App.Models;
using SpendWise.App.Services;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpendWise.App.Pages;

public partial class GroceryPage : ContentPage
{
    private readonly ApiService _apiService = new();
    private readonly List<LocalGroceryItem> _items = new();

    private int? _savedGroceryListId;
    private Guid? _editingItemId;
    private bool _isSaving;
    private bool _isCompleted;
    private readonly int? _listIdToOpen;
    private bool _initialListLoaded;

    private static readonly Color Green = Color.FromArgb("#146B3A");
    private static readonly Color Dark = Color.FromArgb("#172B20");
    private static readonly Color Gray = Color.FromArgb("#68776E");
    private static readonly Color Red = Color.FromArgb("#B42318");

    public GroceryPage()
    {
        InitializeComponent();

        RetailerPicker.SelectedIndex = 0;
        UpdateRetailerButton();

        ResetProductForm();
        DisplayItems();
        UpdateSummary();
    }

    public GroceryPage(int groceryListId) : this()
    {
        _listIdToOpen = groceryListId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadGroceryListsAsync();

        if (_listIdToOpen.HasValue && !_initialListLoaded)
        {
            _initialListLoaded = true;
            await LoadSavedListAsync(_listIdToOpen.Value);
        }
    }

    // ==========================================
    // RETAILER SELECTION
    // ==========================================

    private async void SelectRetailerButton_Clicked(
        object sender, EventArgs e)
    {
        if (_isCompleted)
        {
            await DisplayAlert(
                "Completed List",
                "Create a new list to change the retailer.",
                "OK");
            return;
        }

        string[] retailers = RetailerPicker.Items.ToArray();

        string selected = await DisplayActionSheet(
            "Select Retailer",
            "Cancel",
            null,
            retailers);

        if (string.IsNullOrWhiteSpace(selected) ||
            selected == "Cancel")
            return;

        int index = GetRetailerIndex(selected);

        if (index >= 0)
        {
            RetailerPicker.SelectedIndex = index;
            UpdateRetailerButton();
        }
    }

    private void UpdateRetailerButton()
    {
        string retailer =
            RetailerPicker.SelectedItem?.ToString()
            ?? "Select a retailer";

        SelectRetailerButton.Text = retailer + "  ▼";
    }

    private int GetRetailerIndex(string retailer)
    {
        for (int i = 0; i < RetailerPicker.Items.Count; i++)
        {
            if (string.Equals(
                RetailerPicker.Items[i],
                retailer,
                StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    // ==========================================
    // ADD OR EDIT ITEM
    // ==========================================

    private void AddItemButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        if (_isCompleted)
        {
            ShowError("This list is completed. Create a new list.");
            return;
        }

        string name = ProductNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Enter a product name.");
            return;
        }

        if (!int.TryParse(
                QuantityEntry.Text,
                out int quantity) || quantity <= 0)
        {
            ShowError("Enter a valid quantity greater than zero.");
            return;
        }

        decimal price = GetDecimal(PriceEntry.Text);

        if (price <= 0)
        {
            ShowError("Enter a valid price greater than R0.");
            return;
        }

        try
        {
            checked
            {
                _ = quantity * price;
            }

            if (_editingItemId.HasValue)
            {
                var item = _items.FirstOrDefault(
                    x => x.LocalId == _editingItemId.Value);

                if (item == null)
                {
                    ShowError("Product not found.");
                    ResetProductForm();
                    return;
                }

                item.ProductName = name;
                item.Quantity = quantity;
                item.UnitPrice = price;

                if (item.DatabaseId.HasValue)
                {
                    item.PendingOldDatabaseId ??= item.DatabaseId;
                    item.DatabaseId = null;
                }
            }
            else
            {
                _items.Add(new LocalGroceryItem
                {
                    LocalId = Guid.NewGuid(),
                    ProductName = name,
                    Quantity = quantity,
                    UnitPrice = price
                });
            }

            ResetProductForm();
            DisplayItems();
            UpdateSummary();
        }
        catch (OverflowException)
        {
            ShowError("The quantity or price is too large.");
        }
    }

    private void EditItemButton_Clicked(
        object? sender, EventArgs e)
    {
        if (_isCompleted ||
            sender is not Button button ||
            button.CommandParameter is not Guid id)
            return;

        var item = _items.FirstOrDefault(
            x => x.LocalId == id);

        if (item == null)
            return;

        HideError();

        _editingItemId = item.LocalId;

        ProductNameEntry.Text = item.ProductName;
        QuantityEntry.Text = item.Quantity.ToString(
            CultureInfo.InvariantCulture);
        PriceEntry.Text = item.UnitPrice.ToString(
            "0.00", CultureInfo.InvariantCulture);

        ProductFormTitleLabel.Text = "Edit Product";
        AddItemButton.Text = "Save Changes";
        CancelEditButton.IsVisible = true;

        ProductNameEntry.Focus();
    }

    private void CancelEditButton_Clicked(
        object sender, EventArgs e)
    {
        ResetProductForm();
        HideError();
    }

    private void ResetProductForm()
    {
        _editingItemId = null;

        ProductNameEntry.Text = "";
        QuantityEntry.Text = "1";
        PriceEntry.Text = "";

        ProductFormTitleLabel.Text = "Add a Product";
        AddItemButton.Text = "+ Add to Basket";
        CancelEditButton.IsVisible = false;
    }

    // ==========================================
    // DISPLAY BASKET
    // ==========================================

    private void DisplayItems()
    {
        ItemsLayout.Children.Clear();

        if (_items.Count == 0)
        {
            ItemsLayout.Children.Add(new Label
            {
                Text = "Your basket is empty.\nAdd a product to get started.",
                FontSize = 13,
                TextColor = Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 20)
            });

            return;
        }

        foreach (var item in _items)
        {
            ItemsLayout.Children.Add(CreateItemView(item));
        }
    }

    private View CreateItemView(LocalGroceryItem item)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F7FAF8"),
            Stroke = Color.FromArgb("#E4EAE6"),
            StrokeThickness = 1,
            Padding = new Thickness(12),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            }
        };

        var layout = new VerticalStackLayout { Spacing = 10 };

        var header = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var information = new VerticalStackLayout { Spacing = 4 };

        information.Children.Add(new Label
        {
            Text = item.ProductName,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Dark
        });

        information.Children.Add(new Label
        {
            Text = $"{item.Quantity} × R {item.UnitPrice:N2}",
            FontSize = 12,
            TextColor = Gray
        });

        var total = new Label
        {
            Text = $"R {item.Total:N2}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Green,
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(information, 0);
        Grid.SetColumn(total, 1);

        header.Children.Add(information);
        header.Children.Add(total);

        layout.Children.Add(header);

        if (!_isCompleted)
        {
            var actions = new HorizontalStackLayout { Spacing = 8 };

            var editButton = new Button
            {
                Text = "Edit",
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#E8F3EC"),
                TextColor = Green,
                CornerRadius = 8,
                HeightRequest = 38,
                Padding = new Thickness(18, 0),
                CommandParameter = item.LocalId
            };

            editButton.Clicked += EditItemButton_Clicked;

            var removeButton = new Button
            {
                Text = "Remove",
                FontSize = 12,
                BackgroundColor = Color.FromArgb("#FCEDEA"),
                TextColor = Red,
                CornerRadius = 8,
                HeightRequest = 38,
                Padding = new Thickness(18, 0),
                CommandParameter = item.LocalId
            };

            removeButton.Clicked += RemoveItemButton_Clicked;

            actions.Children.Add(editButton);
            actions.Children.Add(removeButton);

            layout.Children.Add(actions);
        }

        border.Content = layout;
        return border;
    }

    // ==========================================
    // REMOVE ITEM
    // ==========================================

    private async void RemoveItemButton_Clicked(
        object? sender, EventArgs e)
    {
        HideError();

        if (_isCompleted ||
            sender is not Button button ||
            button.CommandParameter is not Guid id)
            return;

        var item = _items.FirstOrDefault(x => x.LocalId == id);

        if (item == null)
            return;

        button.IsEnabled = false;

        try
        {
            if (_savedGroceryListId.HasValue)
            {
                var ids = new[]
                {
                    item.DatabaseId,
                    item.PendingOldDatabaseId
                }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct();

                foreach (int databaseId in ids)
                {
                    var result = await _apiService.DeleteGroceryItemAsync(
                        _savedGroceryListId.Value,
                        databaseId);

                    if (!result.Success)
                    {
                        ShowError(result.Message);
                        return;
                    }
                }
            }

            _items.Remove(item);

            if (_editingItemId == item.LocalId)
                ResetProductForm();

            DisplayItems();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            ShowError($"Unable to remove product: {ex.Message}");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    // ==========================================
    // CLEAR BASKET
    // ==========================================

    private async void ClearItemsButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        if (_isCompleted)
        {
            ShowError("Completed lists cannot be changed.");
            return;
        }

        if (_items.Count == 0)
        {
            ShowError("Your basket is already empty.");
            return;
        }

        bool confirm = await DisplayAlert(
            "Clear Basket",
            "Remove all products from this basket?",
            "Clear All",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            foreach (var item in _items.ToList())
            {
                if (_savedGroceryListId.HasValue)
                {
                    var ids = new[]
                    {
                        item.DatabaseId,
                        item.PendingOldDatabaseId
                    }
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .Distinct();

                    foreach (int id in ids)
                    {
                        var result = await _apiService.DeleteGroceryItemAsync(
                            _savedGroceryListId.Value, id);

                        if (!result.Success)
                        {
                            ShowError(result.Message);
                            DisplayItems();
                            UpdateSummary();
                            return;
                        }
                    }
                }

                _items.Remove(item);
            }

            ResetProductForm();
            DisplayItems();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            ShowError($"Unable to clear basket: {ex.Message}");
        }
    }

    // ==========================================
    // SAVE GROCERY LIST
    // ==========================================

    private async void SaveListButton_Clicked(
        object sender, EventArgs e)
    {
        if (_isSaving || _isCompleted)
            return;

        HideError();

        string listName = ListNameEntry.Text?.Trim() ?? "";
        string retailer = RetailerPicker.SelectedItem?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(listName))
        {
            ShowError("Enter a grocery list name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(retailer))
        {
            ShowError("Select a retailer.");
            return;
        }

        if (_items.Count == 0)
        {
            ShowError("Add at least one product before saving.");
            return;
        }

        _isSaving = true;
        SaveListButton.IsEnabled = false;
        SaveListButton.Text = "Saving...";

        try
        {
            if (!_savedGroceryListId.HasValue)
            {
                var request = new CreateGroceryListRequest
                {
                    ListName = listName,
                    Retailer = retailer
                };

                var result = await _apiService.CreateGroceryListAsync(request);

                if (!result.Success || result.Data == null)
                {
                    ShowError(result.Message);
                    return;
                }

                _savedGroceryListId = result.Data.Id;
            }

            foreach (var item in _items)
            {
                if (item.PendingOldDatabaseId.HasValue)
                {
                    var result = await _apiService.DeleteGroceryItemAsync(
                        _savedGroceryListId.Value,
                        item.PendingOldDatabaseId.Value);

                    if (!result.Success)
                    {
                        ShowError(result.Message);
                        return;
                    }

                    item.PendingOldDatabaseId = null;
                }

                if (item.DatabaseId.HasValue)
                    continue;

                var request = new AddGroceryItemRequest
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                var addResult = await _apiService.AddGroceryItemAsync(
                    _savedGroceryListId.Value,
                    request);

                if (!addResult.Success || addResult.Data == null)
                {
                    ShowError(addResult.Message);
                    return;
                }

                item.DatabaseId = addResult.Data.Item.Id;
            }

            await DisplayAlert(
                "Saved",
                "Your grocery list was saved successfully.",
                "OK");

            await LoadGroceryListsAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Unable to save list: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
            SaveListButton.Text = "Save List";
            SaveListButton.IsEnabled = !_isCompleted;
        }
    }

    // ==========================================
    // LOAD SAVED LISTS
    // ==========================================

    private async Task LoadGroceryListsAsync()
    {
        ListsLoadingIndicator.IsVisible = true;
        ListsLoadingIndicator.IsRunning = true;

        try
        {
            var result = await _apiService.GetGroceryListsAsync();

            SavedListsLayout.Children.Clear();

            if (!result.Success || result.Data == null)
            {
                ShowNoSavedLists("Unable to load saved lists.");
                return;
            }

            if (result.Data.Count == 0)
            {
                ShowNoSavedLists("No saved grocery lists yet.");
                return;
            }

            foreach (GroceryListSummary list in result.Data)
            {
                SavedListsLayout.Children.Add(CreateSavedListView(list));
            }
        }
        catch (Exception ex)
        {
            ShowNoSavedLists($"Unable to load lists: {ex.Message}");
        }
        finally
        {
            ListsLoadingIndicator.IsRunning = false;
            ListsLoadingIndicator.IsVisible = false;
        }
    }

    private View CreateSavedListView(GroceryListSummary list)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F7FAF8"),
            Stroke = Color.FromArgb("#E4EAE6"),
            Padding = new Thickness(12),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            }
        };

        var layout = new VerticalStackLayout { Spacing = 8 };

        layout.Children.Add(new Label
        {
            Text = list.ListName,
            FontAttributes = FontAttributes.Bold,
            TextColor = Dark
        });

        layout.Children.Add(new Label
        {
            Text = $"{list.Retailer} • {list.ItemCount} items",
            FontSize = 12,
            TextColor = Gray
        });

        layout.Children.Add(new Label
        {
            Text = $"R {list.Total:N2}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Green
        });

        var openButton = new Button
        {
            Text = "Open List",
            BackgroundColor = Green,
            TextColor = Colors.White,
            CornerRadius = 8,
            CommandParameter = list.Id
        };

        openButton.Clicked += OpenSavedListButton_Clicked;

        layout.Children.Add(openButton);
        border.Content = layout;

        return border;
    }

    private async void OpenSavedListButton_Clicked(
        object? sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is int listId)
        {
            await LoadSavedListAsync(listId);
        }
    }

    private async Task LoadSavedListAsync(int groceryListId)
    {
        HideError();

        try
        {
            var result = await _apiService.GetGroceryListAsync(groceryListId);

            if (!result.Success || result.Data == null)
            {
                ShowError(result.Message);
                return;
            }

            GroceryListResponse list = result.Data;

            _savedGroceryListId = list.Id;
            ListNameEntry.Text = list.ListName;

            int retailerIndex = GetRetailerIndex(list.Retailer);

            RetailerPicker.SelectedIndex =
                retailerIndex >= 0 ? retailerIndex : 0;

            UpdateRetailerButton();

            _items.Clear();

            foreach (GroceryItemResponse item in list.Items)
            {
                _items.Add(new LocalGroceryItem
                {
                    LocalId = Guid.NewGuid(),
                    DatabaseId = item.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            _isCompleted = !string.Equals(
                list.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase);

            SaveListButton.IsEnabled = !_isCompleted;
            CheckBudgetButton.IsEnabled = !_isCompleted;
            SelectRetailerButton.IsEnabled = !_isCompleted;

            ResetProductForm();
            DisplayItems();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            ShowError($"Unable to open list: {ex.Message}");
        }
    }

    private async void LoadListsButton_Clicked(
        object sender, EventArgs e)
    {
        await LoadGroceryListsAsync();
    }

    private void ShowNoSavedLists(string message)
    {
        SavedListsLayout.Children.Clear();

        SavedListsLayout.Children.Add(new Label
        {
            Text = message,
            TextColor = Gray,
            FontSize = 12
        });
    }

    // ==========================================
    // NEW GROCERY LIST
    // ==========================================

    private void NewListButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        _savedGroceryListId = null;
        _isCompleted = false;
        _items.Clear();

        ListNameEntry.Text = "";
        RetailerPicker.SelectedIndex = 0;

        UpdateRetailerButton();

        SelectRetailerButton.IsEnabled = true;
        SaveListButton.IsEnabled = true;
        CheckBudgetButton.IsEnabled = true;

        ResetProductForm();
        DisplayItems();
        UpdateSummary();
    }

    // ==========================================
    // CHECK BUDGET
    // ==========================================

    private async void CheckBudgetButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        if (_items.Count == 0)
        {
            await DisplayAlert(
                "Empty Basket",
                "Add products before checking your budget.",
                "OK");
            return;
        }

        if (!_savedGroceryListId.HasValue ||
            _items.Any(x =>
                !x.DatabaseId.HasValue ||
                x.PendingOldDatabaseId.HasValue))
        {
            await DisplayAlert(
                "Save First",
                "Save your current grocery list before checking the budget.",
                "OK");
            return;
        }

        CheckBudgetButton.IsEnabled = false;
        CheckBudgetButton.Text = "Checking...";

        try
        {
            DateTime today = DateTime.Now;

            var result = await _apiService.CheckGroceryBudgetAsync(
                _savedGroceryListId.Value,
                today.Month,
                today.Year);

            if (!result.Success || result.Data == null)
            {
                ShowError(result.Message);
                return;
            }

            GroceryBudgetCheckResponse data = result.Data;

            var message = new StringBuilder();

            message.AppendLine($"Basket Total: R {data.ListTotal:N2}");
            message.AppendLine();
            message.AppendLine($"Grocery Budget: R {data.GroceryBudgetLimit:N2}");
            message.AppendLine($"Already Spent: R {data.AlreadySpent:N2}");
            message.AppendLine($"Available: R {data.AvailableBeforeList:N2}");
            message.AppendLine();

            if (data.WithinBudget)
            {
                message.AppendLine("Within Budget");
                message.AppendLine($"Remaining: R {data.RemainingAfterList:N2}");
            }
            else
            {
                message.AppendLine("Over Budget");
                message.AppendLine($"Over By: R {data.OverBudgetBy:N2}");
            }

            await DisplayAlert("Budget Check", message.ToString(), "OK");
        }
        catch (Exception ex)
        {
            ShowError($"Budget check failed: {ex.Message}");
        }
        finally
        {
            CheckBudgetButton.IsEnabled = !_isCompleted;
            CheckBudgetButton.Text = "Check Budget";
        }
    }

    // ==========================================
    // REVIEW CHECKOUT
    // ==========================================

    private async void CheckoutButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        if (_items.Count == 0)
        {
            await DisplayAlert(
                "Empty Basket",
                "Add products before reviewing checkout.",
                "OK");
            return;
        }

        decimal total;

        try
        {
            total = _items.Sum(x => x.Total);
        }
        catch (OverflowException)
        {
            ShowError("The basket total is too large.");
            return;
        }

        string retailer = RetailerPicker.SelectedItem?.ToString()
            ?? "Not selected";

        string listName = ListNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(listName))
            listName = "My Grocery Basket";

        var review = new StringBuilder();

        review.AppendLine(listName);
        review.AppendLine();
        review.AppendLine($"Retailer: {retailer}");
        review.AppendLine($"Products: {_items.Count}");
        review.AppendLine();
        review.AppendLine("YOUR PRODUCTS");
        review.AppendLine("------------------------");

        foreach (var item in _items)
        {
            review.AppendLine(item.ProductName);
            review.AppendLine(
                $"{item.Quantity} × R {item.UnitPrice:N2} = R {item.Total:N2}");
            review.AppendLine();
        }

        review.AppendLine("------------------------");
        review.AppendLine($"ESTIMATED TOTAL: R {total:N2}");
        review.AppendLine();
        review.AppendLine("Checkout preview only.");
        review.AppendLine("No payments are processed.");
        review.AppendLine("No purchases are recorded.");

        await DisplayAlert(
            "SpendWise Checkout Review",
            review.ToString(),
            "OK");
    }

    private void UpdateCheckoutState()
    {
        CheckoutButton.IsEnabled = true;

        CheckoutButton.Text = _isCompleted
            ? "Review Completed List"
            : "Review Checkout";
    }

    // ==========================================
    // REFRESH
    // ==========================================

    private async void RefreshButton_Clicked(
        object sender, EventArgs e)
    {
        HideError();

        await LoadGroceryListsAsync();

        await DisplayAlert(
            "Refreshed",
            "Saved lists refreshed. Your current basket was kept.",
            "OK");
    }

    // ==========================================
    // UPDATE SUMMARY
    // ==========================================

    private void UpdateSummary()
    {
        try
        {
            int quantity = _items.Sum(x => x.Quantity);
            decimal total = _items.Sum(x => x.Total);

            ItemCountLabel.Text = quantity.ToString();
            EstimatedTotalLabel.Text = $"R {total:N2}";
            ListTotalLabel.Text = $"R {total:N2}";
        }
        catch (OverflowException)
        {
            ItemCountLabel.Text = "—";
            EstimatedTotalLabel.Text = "Invalid total";
            ListTotalLabel.Text = "Invalid total";

            ShowError("The basket total is too large.");
        }

        UpdateCheckoutState();
    }

    // ==========================================
    // PRICE PARSING
    // ==========================================

    private decimal GetDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        string cleaned = value.Trim()
            .Replace("R", "", StringComparison.OrdinalIgnoreCase)
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
    // ERROR HANDLING
    // ==========================================

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        MessageBorder.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.Text = "";
        MessageBorder.IsVisible = false;
    }

    // ==========================================
    // BACK BUTTON
    // ==========================================

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

    // ==========================================
    // LOCAL GROCERY ITEM MODEL
    // ==========================================

    private class LocalGroceryItem
    {
        public Guid LocalId { get; set; }

        public int? DatabaseId { get; set; }

        public int? PendingOldDatabaseId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Total => Quantity * UnitPrice;
    }
}
