using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class DigitalAsset : EntityBase
{
    public string StorageKey { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public int? MaxDownloads { get; set; }
}
