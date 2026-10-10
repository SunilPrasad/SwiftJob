namespace SwiftJob.Domain.Common;

/// <summary>Base type for persisted entities with an integer surrogate key.</summary>
public abstract class Entity
{
    public int Id { get; set; }
}

/// <summary>Entity that tracks creation and last-modification timestamps (UTC). Set by the persistence layer.</summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
