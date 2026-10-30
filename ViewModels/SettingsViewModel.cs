using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CleanSnap.Database;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseService _database;

    [ObservableProperty]
    private bool _useRecycleBin = true;

    [ObservableProperty]
    private bool _autoScanOnLaunch = true;

    [ObservableProperty]
    private bool _offlineOnly = true;

    [ObservableProperty]
    private string _appVersion = "1.0.0 (CleanSnap Production)";

    [ObservableProperty]
    private string _developerInfo = "CleanSnap - Privacy-first Device Cleaner";

    [ObservableProperty]
    private ObservableCollection<CleanupHistoryRecord> _cleanupHistory = [];

    [ObservableProperty]
    private ObservableCollection<RecycledFileBackupLog> _backupLogs = [];

    public SettingsViewModel(IDatabaseService database)
    {
        _database = database;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _database.GetCleanupHistoryAsync();
            CleanupHistory.Clear();
            foreach (var h in history)
            {
                CleanupHistory.Add(h);
            }

            var backups = await _database.GetBackupLogsAsync();
            BackupLogs.Clear();
            foreach (var b in backups)
            {
                BackupLogs.Add(b);
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task OpenPrivacyPolicyAsync()
    {
        await Launcher.OpenAsync(new Uri("https://github.com/cleansnap/cleansnap/blob/main/PRIVACY_POLICY.md"));
    }
}
