using Microsoft.Extensions.Logging;
using SpendWise.App.Services;

namespace SpendWise.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            // ==========================================
            // MARTIN SPENDWISE SERVICES
            // ==========================================

            // Handles communication with SpendWise.API
            builder.Services.AddSingleton<ApiService>();

            // Handles JWT and logged-in user storage
            builder.Services.AddSingleton<AuthStorageService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}