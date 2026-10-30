using CleanSnap.Database;

namespace CleanSnap.Services.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();

    Task LogCleanupAsync(CleanupHistoryRecord record);

    Task LogRecycledFileAsync(RecycledFileBackupLog log);

    Task<List<CleanupHistoryRecord>> GetCleanupHistoryAsync();

    Task<List<RecycledFileBackupLog>> GetBackupLogsAsync();

    Task<FileHashCacheRecord?> GetCachedHashAsync(string path, long size, long lastModifiedTicks);

    Task SaveCachedHashAsync(string path, long size, long lastModifiedTicks, string sha256, string partialHash);
}
