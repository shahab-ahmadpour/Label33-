using Label33.Domain.Common;

namespace Label33.Domain.Entities;

public class SiteSetting : EntityBase
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class AuditLog : EntityBase
{
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
}

public class PageViewLog : EntityBase
{
    public Guid? UserId { get; set; }
    public string Path { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class OnlineUser
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public DateTime LastSeenUtc { get; set; }
    public string? LastPath { get; set; }
    public string? IpAddress { get; set; }
}
