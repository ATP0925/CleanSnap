using SQLite;
using CleanSnap.Database;
using CleanSnap.Services.Interfaces;

namespace CleanSnap.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "cleansnap.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _database.CreateTableAsync<CleanupHistoryRecord>();
        await _database.CreateTableAsync<RecycledFileBackupLog>();
        await _database.CreateTableAsync<FileHashCacheRecord>();
    }

    public async Task LogCleanupAsync(CleanupHistoryRecord record)
    {
        await InitializeAsync();
        await _database!.InsertAsync(record);
    }

    public async Task LogRecycledFileAsync(RecycledFileBackupLog log)
    {
        await InitializeAsync();
        await _database!.InsertAsync(log);
    }

    public async Task<List<CleanupHistoryRecord>> GetCleanupHistoryAsync()
    {
        await InitializeAsync();
        return await _database!.Table<CleanupHistoryRecord>()
                               .OrderByDescending(r => r.CleanedAt)
                               .Take(50)
                               .ToListAsync();
    }

    public async Task<List<RecycledFileBackupLog>> GetBackupLogsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<RecycledFileBackupLog>()
                               .OrderByDescending(l => l.RecycledAt)
                               .Take(100)
                               .ToListAsync();
    }

    public async Task<FileHashCacheRecord?> GetCachedHashAsync(string path, long size, long lastModifiedTicks)
    {
        await InitializeAsync();
        return await _database!.Table<FileHashCacheRecord>()
                               .Where(c => c.FilePath == path && c.FileSize == size && c.LastModifiedTicks == lastModifiedTicks)
                               .FirstOrDefaultAsync();
    }

    public async Task SaveCachedHashAsync(string path, long size, long lastModifiedTicks, string sha256, string partialHash)
    {
        await InitializeAsync();
        var record = new FileHashCacheRecord
        {
            FilePath = path,
            FileSize = size,
            LastModifiedTicks = lastModifiedTicks,
            Sha256Hash = sha256,
            PartialHash = partialHash
        };
        await _database!.InsertOrReplaceAsync(record);
    }
}
