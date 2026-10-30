using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CleanSnap.Database;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystem;
    private readonly IDuplicateScannerService _scanner;
    private readonly IJunkCleanerService _junkCleaner;
    private readonly IDatabaseService _database;

    [ObservableProperty]
    private long _storageTotalBytes;

    [ObservableProperty]
    private long _storageUsedBytes;

    [ObservableProperty]
    private long _storageFreeBytes;

    [ObservableProperty]
    private double _usedPercentage;

    [ObservableProperty]
    private int _duplicateGroupsCount;

    [ObservableProperty]
    private int _duplicateFilesCount;

    [ObservableProperty]
    private long _duplicateBytes;

    [ObservableProperty]
    private long _junkBytes;

    [ObservableProperty]
    private long _totalCleanableBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanning))]
    private bool _isScanning;

    public bool IsNotScanning => !IsScanning;

    [ObservableProperty]
    private double _storageProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan your device";

    [ObservableProperty]
    private ScanResult? _lastScanResult;

    [ObservableProperty]
    private JunkAnalysis? _lastJunkAnalysis;

    public HomeViewModel(
        IFileSystemService fileSystem,
        IDuplicateScannerService scanner,
        IJunkCleanerService junkCleaner,
        IDatabaseService database)
    {
        _fileSystem = fileSystem;
        _scanner = scanner;
        _junkCleaner = junkCleaner;
        _database = database;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            var storage = await _fileSystem.GetStorageInfoAsync();
            StorageTotalBytes = storage.TotalBytes;
            StorageUsedBytes = storage.UsedBytes;
            StorageFreeBytes = storage.FreeBytes;
            UsedPercentage = storage.UsedPercentage;
            StorageProgress = storage.TotalBytes > 0 ? (double)storage.UsedBytes / storage.TotalBytes : 0.5;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Storage check: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task QuickScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "Analyzing storage, duplicates & temporary cache...";

        try
        {
            // 1. Get folders
            var folders = await _fileSystem.GetDefaultScanFoldersAsync();

            // 2. Scan duplicates in background
            var dupTask = _scanner.ScanAsync(folders);

            // 3. Scan junk in background
            var junkTask = _junkCleaner.AnalyzeJunkAsync();

            await Task.WhenAll(dupTask, junkTask);

            LastScanResult = await dupTask;
            LastJunkAnalysis = await junkTask;

            DuplicateGroupsCount = LastScanResult.TotalDuplicateGroups;
            DuplicateFilesCount = LastScanResult.TotalDuplicateFiles;
            DuplicateBytes = LastScanResult.TotalReclaimableBytes;
            JunkBytes = LastJunkAnalysis.TotalJunkBytes;
            TotalCleanableBytes = DuplicateBytes + JunkBytes;

            StatusMessage = $"Found {DuplicateFilesCount} duplicate files & {JunkBytes / (1024.0 * 1024 * 1024):F2} GB junk cleanable!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public async Task OneTapCleanAsync()
    {
        if (IsScanning) return;

        if (LastScanResult == null || LastJunkAnalysis == null)
        {
            await QuickScanAsync();
        }

        if (TotalCleanableBytes == 0)
        {
            StatusMessage = "Your device is already 100% clean!";
            return;
        }

        IsScanning = true;
        StatusMessage = "Cleaning duplicate copies (safe Recycle Bin) and temporary caches...";

        try
        {
            int deletedDupes = 0;
            long freedDupeBytes = 0;

            // 1. Clean duplicates safely
            if (LastScanResult != null)
            {
                foreach (var group in LastScanResult.Groups)
                {
                    // Verify original still exists
                    if (!_fileSystem.FileExists(group.Original.FullPath))
                        continue;

                    foreach (var dupe in group.Duplicates)
                    {
                        bool success = await _fileSystem.DeleteToTrashAsync(dupe.FullPath);
                        if (success)
                        {
                            deletedDupes++;
                            freedDupeBytes += dupe.SizeBytes;

                            // Log recycled file for data backup and recovery tracking
                            await _database.LogRecycledFileAsync(new RecycledFileBackupLog
                            {
                                OriginalFilePath = group.Original.FullPath,
                                DuplicateFilePath = dupe.FullPath,
                                FileSizeBytes = dupe.SizeBytes,
                                Sha256Hash = group.Hash,
                                RecycledAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            // 2. Clean selected junk
            long freedJunkBytes = 0;
            if (LastJunkAnalysis != null)
            {
                var junkReport = await _junkCleaner.CleanJunkAsync(LastJunkAnalysis.Categories);
                freedJunkBytes = junkReport.FreedBytes;
            }

            long totalFreed = freedDupeBytes + freedJunkBytes;
            StatusMessage = $"Cleanup complete! Freed {totalFreed / (1024.0 * 1024):F1} MB space safely.";

            // Save Cleanup History Record in Database
            await _database.LogCleanupAsync(new CleanupHistoryRecord
            {
                CleanedAt = DateTime.UtcNow,
                TotalFreedBytes = totalFreed,
                DuplicatesRemoved = deletedDupes,
                JunkFilesRemoved = 1,
                Details = $"Cleaned {deletedDupes} duplicates and junk temporary files."
            });

            // Refresh storage stats
            await InitializeAsync();
            DuplicateGroupsCount = 0;
            DuplicateFilesCount = 0;
            DuplicateBytes = 0;
            JunkBytes = 0;
            TotalCleanableBytes = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cleanup error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
