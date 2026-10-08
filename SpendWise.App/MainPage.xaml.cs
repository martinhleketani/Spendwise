
namespace SpendWise.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // ============================================
        // OPEN BUDGET NOTIFICATIONS PAGE
        // ============================================

        private async void OnNotificationsClicked(
            object sender,
            EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(
                    nameof(NotificationsPage));
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Navigation Error",
                    ex.Message,
                    "OK");
            }
        }
    }
}
