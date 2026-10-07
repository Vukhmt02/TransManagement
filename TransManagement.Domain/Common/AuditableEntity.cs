namespace TransManagement.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }
}

