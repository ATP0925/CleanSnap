#if ANDROID
using System.Runtime.CompilerServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using CleanSnap.Models;
using CleanSnap.Services.Interfaces;
using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace CleanSnap.Platforms.Android.Services;

/// <summary>
/// Android-specific implementation of IFileSystemService.
/// Supports modern Scoped Storage and All Files Access permission (MANAGE_EXTERNAL_STORAGE).
/// </summary>
public class AndroidFileService : IFileSystemService
{
    public Task<List<string>> GetDefaultScanFoldersAsync()
    {
        var folders = new List<string>();

        var commonDirs = new[]
        {
            Environment.DirectoryDownloads,
            Environment.DirectoryPictures,
            Environment.DirectoryDcim,
            Environment.DirectoryDocuments,
            Environment.DirectoryMusic,
            Environment.DirectoryMovies
        };

        foreach (var dir in commonDirs)
        {
            var javaFile = Environment.GetExternalStoragePublicDirectory(dir);
            if (javaFile != null && javaFile.Exists() && javaFile.IsDirectory)
            {
                folders.Add(javaFile.AbsolutePath);
            }
        }

        // Also add external storage root if available
        var root = Environment.ExternalStorageDirectory;
        if (root != null && root.Exists() && folders.Count == 0)
        {
            folders.Add(root.AbsolutePath);
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

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch
            {
                continue;
            }

            foreach (var subDir in subDirs)
            {
                string dirName = Path.GetFileName(subDir);
                // Skip Android internal app private data if encountered
                if (!dirName.StartsWith(".") && !dirName.Equals("Android", StringComparison.OrdinalIgnoreCase))
                {
                    stack.Push(subDir);
                }
            }

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
                    continue;
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
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<StorageBreakdown> GetStorageInfoAsync()
    {
        var breakdown = new StorageBreakdown();

        try
        {
            var path = Environment.ExternalStorageDirectory?.AbsolutePath ?? "/sdcard";
            var stat = new StatFs(path);

            long blockSize = stat.BlockSizeLong;
            long totalBlocks = stat.BlockCountLong;
            long availableBlocks = stat.AvailableBlocksLong;

            breakdown.TotalBytes = totalBlocks * blockSize;
            breakdown.FreeBytes = availableBlocks * blockSize;
            breakdown.UsedBytes = breakdown.TotalBytes - breakdown.FreeBytes;
        }
        catch
        {
            breakdown.TotalBytes = 64L * 1024 * 1024 * 1024;
            breakdown.FreeBytes = 16L * 1024 * 1024 * 1024;
            breakdown.UsedBytes = breakdown.TotalBytes - breakdown.FreeBytes;
        }

        return Task.FromResult(breakdown);
    }

    public Task<bool> HasFilePermissionsAsync()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            return Task.FromResult(Environment.IsExternalStorageManager);
        }

        return Task.FromResult(true);
    }

    public Task<bool> RequestFilePermissionsAsync()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            if (!Environment.IsExternalStorageManager)
            {
                try
                {
                    var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                    intent.SetData(Uri.Parse($"package:{Application.Context.PackageName}"));
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                }
                catch
                {
                    var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                }
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

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
#endif

