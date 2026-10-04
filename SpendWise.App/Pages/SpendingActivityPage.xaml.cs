using SpendWise.App.Models;
using SpendWise.App.Services;

namespace SpendWise.App.Pages
{
    public partial class SpendingActivityPage : ContentPage
    {
        private readonly ApiService _apiService;
        private bool _isLoading;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public SpendingActivityPage()
        {
            InitializeComponent();

            _apiService =
                new ApiService();
        }


        // =====================================================
        // PAGE APPEARING
        // =====================================================

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadTransactionsAsync();
        }


        // =====================================================
        // LOAD TRANSACTIONS
        // =====================================================

        private async Task LoadTransactionsAsync()
        {
            if (_isLoading)
            {
                return;
            }

            try
            {
                _isLoading =
                    true;

                SetLoadingState(true);

                HideError();


                var result =
                    await _apiService
                        .GetTransactionsAsync();


                if (!result.Success)
                {
                    ShowError(
                        result.Message);

                    ResetPage();

                    return;
                }


                List<TransactionResponse> transactions =
                    result.Data ??
                    new List<TransactionResponse>();


                // Only display spending transactions.
                var spendingTransactions =
                    transactions
                        .Where(
                            transaction =>
                                string.Equals(
                                    transaction.Type,
                                    "Purchase",
                                    StringComparison.OrdinalIgnoreCase)
                                ||
                                transaction.Amount < 0)
                        .OrderByDescending(
                            transaction =>
                                transaction.CreatedAt)
                        .ToList();


                UpdateSummary(
                    spendingTransactions);


                DisplayTransactions(
                    spendingTransactions);
            }
            catch (Exception ex)
            {
                ShowError(
                    $"Unable to load spending activity: {ex.Message}");

                ResetPage();
            }
            finally
            {
                _isLoading =
                    false;

                SetLoadingState(
                    false);
            }
        }


        // =====================================================
        // UPDATE SUMMARY
        // =====================================================

        private void UpdateSummary(
            List<TransactionResponse> transactions)
        {
            TransactionCountLabel.Text =
                transactions.Count
                    .ToString();


            decimal totalSpent =
                transactions.Sum(
                    transaction =>
                        Math.Abs(
                            transaction.Amount));


            TotalSpentLabel.Text =
                $"R {totalSpent:N2}";
        }


        // =====================================================
        // DISPLAY TRANSACTIONS
        // =====================================================

        private void DisplayTransactions(
            List<TransactionResponse> transactions)
        {
            TransactionsLayout
                .Children
                .Clear();


            if (transactions.Count == 0)
            {
                ShowEmptyState();

                return;
            }


            foreach (
                TransactionResponse transaction
                in transactions)
            {
                View card =
                    CreateTransactionCard(
                        transaction);


                TransactionsLayout
                    .Children
                    .Add(
                        card);
            }
        }


        // =====================================================
        // CREATE TRANSACTION CARD
        // =====================================================

        private View CreateTransactionCard(
            TransactionResponse transaction)
        {
            string title =
                GetTransactionTitle(
                    transaction);


            string category =
                string.IsNullOrWhiteSpace(
                    transaction.Category)
                    ? "Other"
                    : transaction.Category;


            string dateText =
                transaction
                    .CreatedAt
                    .ToLocalTime()
                    .ToString(
                        "dd MMM yyyy • HH:mm");


            decimal amount =
                Math.Abs(
                    transaction.Amount);


            // -------------------------------------------------
            // TITLE
            // -------------------------------------------------

            Label titleLabel =
                new Label
                {
                    Text =
                        title,

                    FontSize =
                        16,

                    FontAttributes =
                        FontAttributes.Bold,

                    TextColor =
                        Color.FromArgb(
                            "#1E1E1E"),

                    LineBreakMode =
                        LineBreakMode.TailTruncation
                };


            // -------------------------------------------------
            // CATEGORY AND DATE
            // -------------------------------------------------

            Label detailsLabel =
                new Label
                {
                    Text =
                        $"{category} • {dateText}",

                    FontSize =
                        12,

                    TextColor =
                        Color.FromArgb(
                            "#777777"),

                    LineBreakMode =
                        LineBreakMode.WordWrap
                };


            VerticalStackLayout detailsLayout =
                new VerticalStackLayout
                {
                    Spacing =
                        4,

                    VerticalOptions =
                        LayoutOptions.Center
                };


            detailsLayout
                .Children
                .Add(
                    titleLabel);


            detailsLayout
                .Children
                .Add(
                    detailsLabel);


            // -------------------------------------------------
            // AMOUNT
            // -------------------------------------------------

            Label amountLabel =
                new Label
                {
                    Text =
                        $"-R {amount:N2}",

                    FontSize =
                        16,

                    FontAttributes =
                        FontAttributes.Bold,

                    TextColor =
                        Color.FromArgb(
                            "#B42318"),

                    VerticalOptions =
                        LayoutOptions.Center,

                    HorizontalTextAlignment =
                        TextAlignment.End
                };


            // -------------------------------------------------
            // TRANSACTION ROW
            // -------------------------------------------------

            Grid transactionGrid =
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(
                            GridLength.Star),

                        new ColumnDefinition(
                            GridLength.Auto)
                    },

                    ColumnSpacing =
                        12
                };


            transactionGrid.Add(
                detailsLayout,
                0,
                0);


            transactionGrid.Add(
                amountLabel,
                1,
                0);


            // -------------------------------------------------
            // CARD CONTENT
            // -------------------------------------------------

            VerticalStackLayout cardContent =
                new VerticalStackLayout
                {
                    Spacing =
                        8
                };


            cardContent
                .Children
                .Add(
                    transactionGrid);


            // -------------------------------------------------
            // REFERENCE
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    transaction.Reference))
            {
                Label referenceLabel =
                    new Label
                    {
                        Text =
                            $"Reference: {transaction.Reference}",

                        FontSize =
                            11,

                        TextColor =
                            Color.FromArgb(
                                "#999999"),

                        LineBreakMode =
                            LineBreakMode.TailTruncation
                    };


                cardContent
                    .Children
                    .Add(
                        referenceLabel);
            }


            // -------------------------------------------------
            // CARD
            // -------------------------------------------------

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
                        new Thickness(
                            14),

                    Margin =
                        new Thickness(
                            0,
                            0,
                            0,
                            10),

                    StrokeShape =
                        new Microsoft.Maui.Controls.Shapes
                            .RoundRectangle
                        {
                            CornerRadius =
                                new CornerRadius(
                                    14)
                        },

                    Content =
                        cardContent
                };


            return card;
        }


        // =====================================================
        // GET TRANSACTION TITLE
        // =====================================================

        private string GetTransactionTitle(
            TransactionResponse transaction)
        {
            if (!string.IsNullOrWhiteSpace(
                    transaction.Retailer))
            {
                return transaction.Retailer;
            }


            if (!string.IsNullOrWhiteSpace(
                    transaction.Description))
            {
                return transaction.Description;
            }


            return "Purchase";
        }


        // =====================================================
        // EMPTY STATE
        // =====================================================

        private void ShowEmptyState()
        {
            TransactionsLayout
                .Children
                .Clear();


            Label emptyLabel =
                new Label
                {
                    Text =
                        "No spending activity recorded yet.",

                    FontSize =
                        14,

                    TextColor =
                        Color.FromArgb(
                            "#777777"),

                    HorizontalOptions =
                        LayoutOptions.Center,

                    HorizontalTextAlignment =
                        TextAlignment.Center,

                    Margin =
                        new Thickness(
                            0,
                            20)
                };


            TransactionsLayout
                .Children
                .Add(
                    emptyLabel);
        }


        // =====================================================
        // RESET PAGE
        // =====================================================

        private void ResetPage()
        {
            TransactionCountLabel.Text =
                "0";


            TotalSpentLabel.Text =
                "R 0.00";


            ShowEmptyState();
        }


        // =====================================================
        // LOADING STATE
        // =====================================================

        private void SetLoadingState(
            bool isLoading)
        {
            LoadingIndicator.IsVisible =
                isLoading;


            LoadingIndicator.IsRunning =
                isLoading;
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
        // REFRESH
        // =====================================================

        private async void RefreshButton_Clicked(
            object sender,
            EventArgs e)
        {
            await LoadTransactionsAsync();
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

                return;
            }


            await Shell.Current.GoToAsync(
                "..");
        }
    }
}