using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.ViewModels;

public partial class ScanViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystem;
    private readonly IDuplicateScannerService _scanner;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _stageTitle = "Ready to start";

    [ObservableProperty]
    private string _statusText = "Press Start Scan to find duplicate files";

    [ObservableProperty]
    private int _stageNumber = 0;

    [ObservableProperty]
    private int _filesProcessed;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _duplicatesFound;

    [ObservableProperty]
    private long _reclaimableBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanning))]
    private bool _isScanning;

    public bool IsNotScanning => !IsScanning;

    [ObservableProperty]
    private ScanResult? _scanResult;

    public ScanViewModel(IFileSystemService fileSystem, IDuplicateScannerService scanner)
    {
        _fileSystem = fileSystem;
        _scanner = scanner;
    }

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        Progress = 0;
        StageNumber = 1;
        StageTitle = "Stage 1: Directory Enumeration";
        StatusText = "Scanning folders...";
        FilesProcessed = 0;
        DuplicatesFound = 0;
        ReclaimableBytes = 0;

        _cts = new CancellationTokenSource();

        var progressHandler = new Progress<ScanProgress>(p =>
        {
            Progress = p.ProgressPercentage / 100.0;
            StageNumber = p.Stage;
            StageTitle = $"Stage {p.Stage}: {p.StageDescription}";
            FilesProcessed = p.FilesProcessed;
            TotalFiles = p.TotalFiles;
            DuplicatesFound = p.DuplicatesFound;
            ReclaimableBytes = p.ReclaimableBytes;
            StatusText = p.StageDescription;
        });

        try
        {
            var folders = await _fileSystem.GetDefaultScanFoldersAsync();
            ScanResult = await _scanner.ScanAsync(folders, progressHandler, _cts.Token);

            Progress = 1.0;
            StatusText = $"Done in {ScanResult.ScanDuration.TotalSeconds:F1}s! Found {ScanResult.TotalDuplicateFiles} duplicates in {ScanResult.TotalDuplicateGroups} groups.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan was cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            StatusText = "Cancelling scan...";
        }
    }
}
