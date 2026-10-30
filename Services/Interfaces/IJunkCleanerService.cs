using CleanSnap.Models;

namespace CleanSnap.Services.Interfaces;

/// <summary>
/// Analyzes and cleans junk/temporary files on the device.
/// </summary>
public interface IJunkCleanerService
{
    /// <summary>Discovers junk file categories and calculates their sizes.</summary>
    Task<JunkAnalysis> AnalyzeJunkAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes files in the selected junk categories.</summary>
    Task<CleanupReport> CleanJunkAsync(
        IEnumerable<JunkCategory> selectedCategories,
        CancellationToken cancellationToken = default);
}

