using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Security.Cryptography;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.Services;

/// <summary>
/// Core 4-stage duplicate detection engine.
/// 
/// Pipeline:
///   Stage 1: Async directory walk → collect FileMetadata
///   Stage 2: Size bucketing → discard unique sizes (eliminates 70-90%)
///   Stage 3: Partial hash (xxHash64 of first+last 4KB) → discard mismatches
///   Stage 4: Full SHA-256 hash → confirm duplicates
/// </summary>
public class DuplicateScannerService : IDuplicateScannerService
{
    private readonly IFileSystemService _fileSystem;
    private readonly IDatabaseService? _database;

    /// <summary>Chunk size for partial hash reads (4 KB).</summary>
    private const int PartialHashChunkSize = 4096;

    /// <summary>Buffer size for full hash streaming (64 KB).</summary>
    private const int FullHashBufferSize = 65536;

    /// <summary>File extensions/names to always skip.</summary>
    private static readonly HashSet<string> ExcludedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "desktop.ini", "thumbs.db", ".ds_store", "folder.jpg", "albumart.jpg"
    };

    private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".lnk", ".tmp", ".crdownload", ".part", ".bak", ".log"
    };

    public DuplicateScannerService(IFileSystemService fileSystem, IDatabaseService? database = null)
    {
        _fileSystem = fileSystem;
        _database = database;
    }

    public async Task<ScanResult> ScanAsync(
        IEnumerable<string> folders,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // ═══════════════════════════════════════════════════════
        // STAGE 1: Async directory walk — discover all files
        // ═══════════════════════════════════════════════════════
        progress?.Report(new ScanProgress(1, "Discovering files...", 0, 0, 0, 0, 0));

        var allFiles = new List<FileMetadata>();
        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await foreach (var file in _fileSystem.EnumerateFilesAsync(folder, cancellationToken))
            {
                // Apply exclusion filters
                if (ExcludedNames.Contains(file.FileName))
                    continue;
                if (ExcludedExtensions.Contains(file.Extension))
                    continue;
                if (file.SizeBytes == 0)
                    continue;

                allFiles.Add(file);

                if (allFiles.Count % 100 == 0)
                {
                    progress?.Report(new ScanProgress(1, "Discovering files...",
                        allFiles.Count, 0, 0, 0, 10));
                }
            }
        }

        int totalFiles = allFiles.Count;
        progress?.Report(new ScanProgress(1, $"Found {totalFiles} files",
            totalFiles, totalFiles, 0, 0, 20));

        // ═══════════════════════════════════════════════════════
        // STAGE 2: Size bucketing — group by exact byte size
        // Files with unique sizes cannot be duplicates
        // ═══════════════════════════════════════════════════════
        progress?.Report(new ScanProgress(2, "Grouping by file size...",
            0, totalFiles, 0, 0, 25));

        var sizeGroups = allFiles
            .GroupBy(f => f.SizeBytes)
            .Where(g => g.Count() > 1) // Discard unique sizes
            .ToDictionary(g => g.Key, g => g.ToList());

        int candidateCount = sizeGroups.Values.Sum(g => g.Count);
        int eliminatedBySize = totalFiles - candidateCount;

        progress?.Report(new ScanProgress(2,
            $"Size filter: {eliminatedBySize} unique files eliminated, {candidateCount} candidates remain",
            candidateCount, totalFiles, 0, 0, 40));

        if (candidateCount == 0)
        {
            stopwatch.Stop();
            return new ScanResult
            {
                TotalFilesScanned = totalFiles,
                ScanDuration = stopwatch.Elapsed
            };
        }

        // ═══════════════════════════════════════════════════════
        // STAGE 3: Partial hash — xxHash64 of first + last 4KB
        // Fast non-cryptographic hash for quick elimination
        // ═══════════════════════════════════════════════════════
        progress?.Report(new ScanProgress(3, "Computing quick hashes...",
            0, candidateCount, 0, 0, 45));

        int partialHashProcessed = 0;
        var partialHashGroups = new ConcurrentDictionary<string, ConcurrentBag<FileMetadata>>();

        // Process each size group
        foreach (var (size, filesInGroup) in sizeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var file in filesInGroup)
            {
                try
                {
                    string partialHash = await ComputePartialHashAsync(file.FullPath, size, cancellationToken);
                    file.PartialHash = partialHash;

                    // Composite key: size + partial hash
                    string groupKey = $"{size}:{partialHash}";
                    partialHashGroups.GetOrAdd(groupKey, _ => []).Add(file);
                }
                catch
                {
                    // Skip files that can't be read (locked, permissions, etc.)
                }

                partialHashProcessed++;
                if (partialHashProcessed % 20 == 0)
                {
                    progress?.Report(new ScanProgress(3, "Computing quick hashes...",
                        partialHashProcessed, candidateCount, 0, 0,
                        45 + (30.0 * partialHashProcessed / candidateCount)));
                }
            }
        }

        // Filter out groups where partial hash is unique
        var partialCandidates = partialHashGroups
            .Where(g => g.Value.Count > 1)
            .ToDictionary(g => g.Key, g => g.Value.ToList());

        int candidateAfterPartial = partialCandidates.Values.Sum(g => g.Count);
        progress?.Report(new ScanProgress(3,
            $"Quick hash filter: {candidateCount - candidateAfterPartial} more eliminated, {candidateAfterPartial} remain",
            candidateAfterPartial, totalFiles, 0, 0, 75));

        if (candidateAfterPartial == 0)
        {
            stopwatch.Stop();
            return new ScanResult
            {
                TotalFilesScanned = totalFiles,
                ScanDuration = stopwatch.Elapsed
            };
        }

        // ═══════════════════════════════════════════════════════
        // STAGE 4: Full SHA-256 hash — stream entire file
        // Only runs on the remaining ~5% of candidates
        // ═══════════════════════════════════════════════════════
        progress?.Report(new ScanProgress(4, "Verifying with full hash...",
            0, candidateAfterPartial, 0, 0, 78));

        int fullHashProcessed = 0;
        var fullHashGroups = new ConcurrentDictionary<string, ConcurrentBag<FileMetadata>>();

        foreach (var (_, filesInGroup) in partialCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var file in filesInGroup)
            {
                try
                {
                    string? fullHash = null;
                    if (_database != null)
                    {
                        var cached = await _database.GetCachedHashAsync(file.FullPath, file.SizeBytes, file.ModifiedDate.Ticks);
                        if (cached != null && !string.IsNullOrEmpty(cached.Sha256Hash))
                        {
                            fullHash = cached.Sha256Hash;
                        }
                    }

                    if (string.IsNullOrEmpty(fullHash))
                    {
                        fullHash = await ComputeFullHashAsync(file.FullPath, cancellationToken);
                        if (_database != null)
                        {
                            await _database.SaveCachedHashAsync(file.FullPath, file.SizeBytes, file.ModifiedDate.Ticks, fullHash, file.PartialHash ?? string.Empty);
                        }
                    }

                    file.FullHash = fullHash;
                    fullHashGroups.GetOrAdd(fullHash, _ => []).Add(file);
                }
                catch
                {
                    // Skip unreadable files
                }

                fullHashProcessed++;
                if (fullHashProcessed % 5 == 0)
                {
                    progress?.Report(new ScanProgress(4, "Verifying with full hash...",
                        fullHashProcessed, candidateAfterPartial, 0, 0,
                        78 + (20.0 * fullHashProcessed / candidateAfterPartial)));
                }
            }
        }

        // Build confirmed duplicate groups
        var confirmedGroups = new List<DuplicateGroup>();
        foreach (var (hash, files) in fullHashGroups)
        {
            if (files.Count < 2)
                continue;

            var fileList = files.ToList();
            var sorted = fileList.OrderByDescending(ScoreForOriginal).ToList();

            confirmedGroups.Add(new DuplicateGroup
            {
                Original = sorted[0],
                Duplicates = sorted.Skip(1).ToList(),
                Hash = hash
            });
        }

        // Sort by reclaimable space descending
        confirmedGroups.Sort((a, b) => b.ReclaimableBytes.CompareTo(a.ReclaimableBytes));

        stopwatch.Stop();

        var result = new ScanResult
        {
            TotalFilesScanned = totalFiles,
            TotalDuplicateGroups = confirmedGroups.Count,
            TotalDuplicateFiles = confirmedGroups.Sum(g => g.DuplicateCount),
            TotalReclaimableBytes = confirmedGroups.Sum(g => g.ReclaimableBytes),
            Groups = confirmedGroups,
            ScanDuration = stopwatch.Elapsed
        };

        progress?.Report(new ScanProgress(4,
            $"Scan complete! Found {result.TotalDuplicateGroups} duplicate groups",
            totalFiles, totalFiles, result.TotalDuplicateFiles,
            result.TotalReclaimableBytes, 100));

        return result;
    }

    /// <summary>
    /// Computes xxHash64 of the first 4KB + last 4KB of a file.
    /// For small files (&lt; 8KB), hashes the entire content.
    /// </summary>
    private async Task<string> ComputePartialHashAsync(
        string filePath, long fileSize, CancellationToken ct)
    {
        var hasher = new XxHash64();

        if (fileSize <= PartialHashChunkSize * 2)
        {
            // Small file: hash the entire content
            byte[] allBytes = await File.ReadAllBytesAsync(filePath, ct);
            hasher.Append(allBytes);
        }
        else
        {
            // Read first 4KB
            byte[] firstChunk = await _fileSystem.ReadFileBytesAsync(
                filePath, 0, PartialHashChunkSize, ct);
            hasher.Append(firstChunk);

            // Read last 4KB
            byte[] lastChunk = await _fileSystem.ReadFileBytesAsync(
                filePath, fileSize - PartialHashChunkSize, PartialHashChunkSize, ct);
            hasher.Append(lastChunk);
        }

        return Convert.ToHexString(hasher.GetHashAndReset());
    }

    /// <summary>
    /// Computes SHA-256 hash of the entire file content using 64KB streaming buffers.
    /// </summary>
    private static async Task<string> ComputeFullHashAsync(
        string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            FullHashBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Scoring heuristic to determine which file to keep as the "original".
    /// Higher score = more likely to be kept.
    /// </summary>
    private static double ScoreForOriginal(FileMetadata file)
    {
        double score = 0;
        string pathLower = file.FullPath.ToLowerInvariant();
        string nameLower = file.FileName.ToLowerInvariant();

        // Prefer files in "important" directories
        if (pathLower.Contains("pictures") || pathLower.Contains("photos") || pathLower.Contains("dcim"))
            score += 10;
        else if (pathLower.Contains("documents"))
            score += 8;
        else if (pathLower.Contains("desktop"))
            score += 4;
        else if (pathLower.Contains("downloads"))
            score += 1;

        // Penalize copy indicators in filename
        if (nameLower.Contains(" - copy") || nameLower.Contains("copy"))
            score -= 10;
        if (nameLower.Contains("(1)") || nameLower.Contains("(2)") || nameLower.Contains("(3)"))
            score -= 5;
        if (System.Text.RegularExpressions.Regex.IsMatch(nameLower, @"_\d+\.\w+$"))
            score -= 3; // e.g., file_1.jpg, report_2.docx

        // Prefer older files (likely the original)
        score -= file.ModifiedDate.Ticks / (double)TimeSpan.TicksPerDay * 0.001;

        // Prefer shorter paths (less nesting = more "intentional" location)
        score -= file.FullPath.Length * 0.01;

        return score;
    }
}

