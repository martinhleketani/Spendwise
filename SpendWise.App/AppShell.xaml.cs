
using SpendWise.App.Pages;

namespace SpendWise.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(
                nameof(NotificationsPage),
                typeof(NotificationsPage));

            Routing.RegisterRoute(
                nameof(HelpPage),
                typeof(HelpPage));
        }
    }
}
