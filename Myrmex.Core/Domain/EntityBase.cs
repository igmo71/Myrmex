namespace Myrmex.Core.Domain;

public abstract class EntityBase
{
    protected EntityBase()
    {
        Id = Guid.CreateVersion7();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = null;
        IsActive = true;
    }

    public Guid Id { get; protected init; }

    public DateTimeOffset CreatedAtUtc { get; protected set; }

    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    public bool IsActive { get; protected set; }

    protected void MarkDeactivated()
    {
        if (!IsActive)
            return;

        IsActive = false;
        Touch();
    }

    protected void MarkReactivated()
    {
        if (IsActive)
            return;

        IsActive = true;
        Touch();
    }

    protected void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
