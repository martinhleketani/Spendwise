
namespace SpendWise.App.Pages;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    // =====================================================
    // SEARCH
    // =====================================================

    private void HelpSearchBar_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ShowAllCards();
            return;
        }

        GettingStartedCard.IsVisible = MatchesSearch(
            "Getting Started",
            GettingStartedContent.Text,
            searchText);

        BudgetsCard.IsVisible = MatchesSearch(
            "Managing Budgets",
            BudgetsContent.Text,
            searchText);

        PurchasesCard.IsVisible = MatchesSearch(
            "Purchases Transactions Spending History",
            PurchasesContent.Text,
            searchText);

        NotificationsCard.IsVisible = MatchesSearch(
            "Budget Notifications Alerts",
            NotificationsContent.Text,
            searchText);

        GroceriesCard.IsVisible = MatchesSearch(
            "Grocery Management Shopping Lists Products",
            GroceriesContent.Text,
            searchText);

        InsightsCard.IsVisible = MatchesSearch(
            "Financial Insights Charts Spending Analysis",
            InsightsContent.Text,
            searchText);

        ReportsCard.IsVisible = MatchesSearch(
            "Financial Reports Reporting Period",
            ReportsContent.Text,
            searchText);

        AccountCard.IsVisible = MatchesSearch(
            "Account Security Profile Settings Password",
            AccountContent.Text,
            searchText);

        FaqCard.IsVisible = MatchesSearch(
            "Frequently Asked Questions",
            "How do I create a monthly budget? " +
            "How do I record a purchase? " +
            "Why do I receive budget notifications? " +
            "Can I compare grocery prices? " +
            "Why are my financial reports empty?",
            searchText);
    }

    private bool MatchesSearch(
        string title,
        string? description,
        string searchText)
    {
        return title.Contains(
                   searchText,
                   StringComparison.OrdinalIgnoreCase)
               ||
               (description?.Contains(
                   searchText,
                   StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowAllCards()
    {
        GettingStartedCard.IsVisible = true;
        BudgetsCard.IsVisible = true;
        PurchasesCard.IsVisible = true;
        NotificationsCard.IsVisible = true;
        GroceriesCard.IsVisible = true;
        InsightsCard.IsVisible = true;
        ReportsCard.IsVisible = true;
        AccountCard.IsVisible = true;
        FaqCard.IsVisible = true;
    }

    // =====================================================
    // EXPANDABLE HELP TOPICS
    // =====================================================

    private void ToggleContent(Label content)
    {
        content.IsVisible = !content.IsVisible;
    }

    private void GettingStartedButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(GettingStartedContent);
    }

    private void BudgetsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(BudgetsContent);
    }

    private void PurchasesButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(PurchasesContent);
    }

    private void NotificationsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(NotificationsContent);
    }

    private void GroceriesButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(GroceriesContent);
    }

    private void InsightsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(InsightsContent);
    }

    private void ReportsButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(ReportsContent);
    }

    private void AccountButton_Clicked(
        object? sender,
        EventArgs e)
    {
        ToggleContent(AccountContent);
    }

    // =====================================================
    // FAQ
    // =====================================================

    private void FaqButton_Clicked(
        object? sender,
        EventArgs e)
    {
        FaqContent.IsVisible = !FaqContent.IsVisible;
    }

    // =====================================================
    // CONTACT SUPPORT
    // =====================================================

    private async void ContactSupportButton_Clicked(
        object? sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Contact Support",
            "If you need assistance with SpendWise, please contact your system administrator.",
            "OK");
    }
}
