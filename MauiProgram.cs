using Microsoft.Extensions.Logging;
using CleanSnap.Services;
using CleanSnap.Services.Interfaces;
using CleanSnap.ViewModels;
using CleanSnap.Views;

namespace CleanSnap;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ══════════════════════════════════════════════════
        // Register Platform & Core Services
        // ══════════════════════════════════════════════════
#if WINDOWS
        builder.Services.AddSingleton<IFileSystemService, CleanSnap.Platforms.Windows.Services.WindowsFileService>();
#elif ANDROID
        builder.Services.AddSingleton<IFileSystemService, CleanSnap.Platforms.Android.Services.AndroidFileService>();
#else
        builder.Services.AddSingleton<IFileSystemService, CleanSnap.Platforms.Windows.Services.WindowsFileService>();
#endif

        builder.Services.AddSingleton<IDuplicateScannerService, DuplicateScannerService>();
        builder.Services.AddSingleton<IJunkCleanerService, JunkCleanerService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        // ══════════════════════════════════════════════════
        // Register ViewModels (Transient / Singletons)
        // ══════════════════════════════════════════════════
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<ResultsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // ══════════════════════════════════════════════════
        // Register Views
        // ══════════════════════════════════════════════════
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<ResultsPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
