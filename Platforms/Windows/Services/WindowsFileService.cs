using System.Runtime.CompilerServices;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.Platforms.Windows.Services;

/// <summary>
/// Windows-specific implementation of IFileSystemService.
/// Supports safe Recycle Bin deletions and standard Windows user folders.
/// </summary>
public class WindowsFileService : IFileSystemService
{
    private static readonly HashSet<string> ExcludedFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Recycle.Bin", "System Volume Information", "Windows", "Program Files",
        "Program Files (x86)", "node_modules", ".git", ".vs", "AppData"
    };

    public Task<List<string>> GetDefaultScanFoldersAsync()
    {
        var folders = new List<string>();
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var candidateFolders = new[]
        {
            Path.Combine(userProfile, "Downloads"),
            Path.Combine(userProfile, "Documents"),
            Path.Combine(userProfile, "Desktop"),
            Path.Combine(userProfile, "Pictures"),
            Path.Combine(userProfile, "Videos"),
            Path.Combine(userProfile, "OneDrive", "Desktop"),
            Path.Combine(userProfile, "OneDrive", "Documents"),
            Path.Combine(userProfile, "OneDrive", "Pictures")
        };

        foreach (var folder in candidateFolders)
        {
            if (Directory.Exists(folder) && !folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(folder);
            }
        }

        return Task.FromResult(folders);
    }

    public async IAsyncEnumerable<FileMetadata> EnumerateFilesAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
            yield break;

        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string currentDir = stack.Pop();

            // Try enumerating subdirectories
            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch
            {
                continue; // Skip inaccessible directories
            }

            foreach (var subDir in subDirs)
            {
                string dirName = Path.GetFileName(subDir);
                if (!ExcludedFolderNames.Contains(dirName))
                {
                    stack.Push(subDir);
                }
            }

            // Enumerate files in current directory
            string[] files;
            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileMetadata? metadata = null;
                try
                {
                    var fileInfo = new FileInfo(file);
                    if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                        continue; // Skip symlinks/junctions

                    metadata = new FileMetadata
                    {
                        FullPath = fileInfo.FullName,
                        FileName = fileInfo.Name,
                        SizeBytes = fileInfo.Length,
                        ModifiedDate = fileInfo.LastWriteTime,
                        Extension = fileInfo.Extension
                    };
                }
                catch
                {
                    continue; // Skip unreadable files
                }

                if (metadata != null)
                {
                    yield return metadata;
                }
            }

            await Task.Yield();
        }
    }

    public Task<bool> DeleteToTrashAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return Task.FromResult(false);

            // Move to Windows Recycle Bin safely using VB FileSystem
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                filePath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public Task<StorageBreakdown> GetStorageInfoAsync()
    {
        var breakdown = new StorageBreakdown();

        try
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(systemDrive);

            if (drive.IsReady)
            {
                breakdown.TotalBytes = drive.TotalSize;
                breakdown.FreeBytes = drive.AvailableFreeSpace;
                breakdown.UsedBytes = drive.TotalSize - drive.AvailableFreeSpace;
            }
        }
        catch
        {
            // Fallback default metrics if inaccessible
            breakdown.TotalBytes = 512L * 1024 * 1024 * 1024;
            breakdown.FreeBytes = 128L * 1024 * 1024 * 1024;
            breakdown.UsedBytes = breakdown.TotalBytes - breakdown.FreeBytes;
        }

        return Task.FromResult(breakdown);
    }

    public Task<bool> HasFilePermissionsAsync() => Task.FromResult(true);

    public Task<bool> RequestFilePermissionsAsync() => Task.FromResult(true);

    public async Task<byte[]> ReadFileBytesAsync(
        string filePath, long offset, int count,
        CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[count];
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

        stream.Seek(offset, SeekOrigin.Begin);
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), cancellationToken);
            if (read == 0) break;
            totalRead += read;
        }

        if (totalRead < count)
        {
            Array.Resize(ref buffer, totalRead);
        }

        return buffer;
    }

    public bool FileExists(string filePath) => File.Exists(filePath);
}

