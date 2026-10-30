using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.Services;

/// <summary>
/// Cross-platform junk and cache cleaner service.
/// Identifies safely cleanable temporary files, caches, and logs.
/// </summary>
public class JunkCleanerService : IJunkCleanerService
{
    private readonly IFileSystemService _fileSystem;

    public JunkCleanerService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<JunkAnalysis> AnalyzeJunkAsync(CancellationToken cancellationToken = default)
    {
        var analysis = new JunkAnalysis();
        var categories = new List<JunkCategory>();

#if WINDOWS
        // 1. User Temp files
        string tempPath = Path.GetTempPath();
        if (Directory.Exists(tempPath))
        {
            var (size, count) = await CalculateFolderSizeAsync(tempPath, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "Windows Temp Files",
                Description = "Temporary files created by apps and system operations",
                IconGlyph = "🗑️",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [tempPath]
            });
        }

        // 2. Pip Cache
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        string pipCache = Path.Combine(localAppData, "pip", "cache");
        if (Directory.Exists(pipCache))
        {
            var (size, count) = await CalculateFolderSizeAsync(pipCache, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "Python / Pip Cache",
                Description = "Downloaded Python packages and wheel cache (auto-redownloaded when needed)",
                IconGlyph = "🐍",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [pipCache]
            });
        }

        // 3. User .cache directory
        string dotCache = Path.Combine(userProfile, ".cache");
        if (Directory.Exists(dotCache))
        {
            var (size, count) = await CalculateFolderSizeAsync(dotCache, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "Application Caches (.cache)",
                Description = "Temporary application data, font cache, and runtime artifacts",
                IconGlyph = "⚡",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [dotCache]
            });
        }

        // 4. NPM Cache
        string npmCache = Path.Combine(appData, "npm-cache");
        if (Directory.Exists(npmCache))
        {
            var (size, count) = await CalculateFolderSizeAsync(npmCache, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "NPM Package Cache",
                Description = "Cached Node.js packages and tarballs",
                IconGlyph = "📦",
                SizeBytes = size,
                FileCount = count,
                IsSelected = false,
                Paths = [npmCache]
            });
        }

        // 5. Windows Crash Dumps
        string crashDumps = Path.Combine(localAppData, "CrashDumps");
        if (Directory.Exists(crashDumps))
        {
            var (size, count) = await CalculateFolderSizeAsync(crashDumps, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "Crash Dumps & Logs",
                Description = "Diagnostic dump files from previously crashed applications",
                IconGlyph = "📋",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [crashDumps]
            });
        }
#elif ANDROID
        // Android temporary and cache directories
        var context = Android.App.Application.Context;
        if (context.CacheDir != null && context.CacheDir.Exists())
        {
            var (size, count) = await CalculateFolderSizeAsync(context.CacheDir.AbsolutePath, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "App Cache",
                Description = "Temporary files cached by running applications",
                IconGlyph = "⚡",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [context.CacheDir.AbsolutePath]
            });
        }

        var extCache = context.ExternalCacheDir;
        if (extCache != null && extCache.Exists())
        {
            var (size, count) = await CalculateFolderSizeAsync(extCache.AbsolutePath, cancellationToken);
            categories.Add(new JunkCategory
            {
                Name = "External App Cache",
                Description = "Cached media and temporary downloads",
                IconGlyph = "🗑️",
                SizeBytes = size,
                FileCount = count,
                IsSelected = true,
                Paths = [extCache.AbsolutePath]
            });
        }
#endif

        analysis.Categories = categories;
        return analysis;
    }

    public async Task<CleanupReport> CleanJunkAsync(
        IEnumerable<JunkCategory> selectedCategories,
        CancellationToken cancellationToken = default)
    {
        var report = new CleanupReport();

        await Task.Run(() =>
        {
            foreach (var category in selectedCategories.Where(c => c.IsSelected))
            {
                foreach (var folderPath in category.Paths)
                {
                    if (!Directory.Exists(folderPath))
                        continue;

                    CleanDirectoryContents(folderPath, report, cancellationToken);
                }
            }
        }, cancellationToken);

        return report;
    }

    private static void CleanDirectoryContents(
        string dirPath, CleanupReport report, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Enumerate files
        try
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    long size = fileInfo.Length;
                    fileInfo.Delete();

                    report.DeletedCount++;
                    report.FreedBytes += size;
                }
                catch (Exception)
                {
                    // File is locked or in-use by an active app; safely skip
                    report.FailedCount++;
                }
            }
        }
        catch { }

        // Recursively clean subdirectories
        try
        {
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                CleanDirectoryContents(subDir, report, ct);

                // Attempt to delete empty subfolder
                try
                {
                    if (Directory.GetFileSystemEntries(subDir).Length == 0)
                    {
                        Directory.Delete(subDir, false);
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static Task<(long Size, int Count)> CalculateFolderSizeAsync(
        string folderPath, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            long totalSize = 0;
            int totalCount = 0;

            try
            {
                var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(file);
                        totalSize += info.Length;
                        totalCount++;
                    }
                    catch { }
                }
            }
            catch { }

            return (totalSize, totalCount);
        }, ct);
    }
}

