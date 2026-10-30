using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CleanSnap.Database;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.ViewModels;

public partial class ResultsViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystem;
    private readonly IJunkCleanerService _junkCleaner;
    private readonly IDatabaseService _database;

    [ObservableProperty]
    private ObservableCollection<DuplicateGroup> _duplicateGroups = [];

    [ObservableProperty]
    private ObservableCollection<JunkCategory> _junkCategories = [];

    [ObservableProperty]
    private long _totalReclaimableBytes;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _resultStatus = "Review duplicates and junk categories below";

    public ResultsViewModel(
        IFileSystemService fileSystem,
        IJunkCleanerService junkCleaner,
        IDatabaseService database)
    {
        _fileSystem = fileSystem;
        _junkCleaner = junkCleaner;
        _database = database;
    }

    public void LoadResults(ScanResult? scanResult, JunkAnalysis? junkAnalysis)
    {
        DuplicateGroups.Clear();
        JunkCategories.Clear();

        if (scanResult != null)
        {
            foreach (var group in scanResult.Groups)
            {
                DuplicateGroups.Add(group);
            }
        }

        if (junkAnalysis != null)
        {
            foreach (var cat in junkAnalysis.Categories)
            {
                JunkCategories.Add(cat);
            }
        }

        CalculateTotals();
    }

    private void CalculateTotals()
    {
        long dupeBytes = DuplicateGroups.Sum(g => g.ReclaimableBytes);
        long junkBytes = JunkCategories.Where(j => j.IsSelected).Sum(j => j.SizeBytes);
        TotalReclaimableBytes = dupeBytes + junkBytes;
    }

    [RelayCommand]
    public async Task DeleteAllDuplicatesAsync()
    {
        if (IsBusy || DuplicateGroups.Count == 0) return;

        IsBusy = true;
        ResultStatus = "Safely moving duplicate files to Recycle Bin...";

        int deletedCount = 0;
        long freedBytes = 0;

        try
        {
            var groupsSnapshot = DuplicateGroups.ToList();

            foreach (var group in groupsSnapshot)
            {
                if (!_fileSystem.FileExists(group.Original.FullPath))
                    continue;

                foreach (var dupe in group.Duplicates)
                {
                    bool deleted = await _fileSystem.DeleteToTrashAsync(dupe.FullPath);
                    if (deleted)
                    {
                        deletedCount++;
                        freedBytes += dupe.SizeBytes;

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

                DuplicateGroups.Remove(group);
            }

            ResultStatus = $"Cleaned {deletedCount} duplicate files! Freed {freedBytes / (1024.0 * 1024):F1} MB.";
            await _database.LogCleanupAsync(new CleanupHistoryRecord
            {
                CleanedAt = DateTime.UtcNow,
                TotalFreedBytes = freedBytes,
                DuplicatesRemoved = deletedCount,
                Details = $"Cleaned {deletedCount} duplicate files via Results view."
            });
            CalculateTotals();
        }
        catch (Exception ex)
        {
            ResultStatus = $"Error deleting: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CleanSelectedJunkAsync()
    {
        if (IsBusy || JunkCategories.Count == 0) return;

        IsBusy = true;
        ResultStatus = "Cleaning selected temporary caches...";

        try
        {
            var report = await _junkCleaner.CleanJunkAsync(JunkCategories.Where(c => c.IsSelected));
            ResultStatus = $"Junk cleaned! Freed {report.FreedBytes / (1024.0 * 1024):F1} MB ({report.DeletedCount} files).";

            foreach (var cat in JunkCategories.Where(c => c.IsSelected))
            {
                cat.SizeBytes = 0;
                cat.FileCount = 0;
            }

            CalculateTotals();
        }
        catch (Exception ex)
        {
            ResultStatus = $"Error cleaning junk: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

