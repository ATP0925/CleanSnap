using CleanSnap.Models;

namespace CleanSnap.Services.Interfaces;

/// <summary>
/// Multi-stage duplicate file detection service.
/// Implements the 4-stage pipeline: Size Bucket → Partial Hash → Full Hash → Confirm.
/// </summary>
public interface IDuplicateScannerService
{
    /// <summary>
    /// Runs the full 4-stage duplicate detection pipeline.
    /// </summary>
    Task<ScanResult> ScanAsync(
        IEnumerable<string> folders,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

