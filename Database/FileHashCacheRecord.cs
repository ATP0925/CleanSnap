using SQLite;

namespace CleanSnap.Database;

[Table("FileHashCache")]
public class FileHashCacheRecord
{
    [PrimaryKey]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public long LastModifiedTicks { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;

    public string PartialHash { get; set; } = string.Empty;
}
