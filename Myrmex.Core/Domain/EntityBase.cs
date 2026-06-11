namespace Myrmex.Core.Domain;

public abstract class EntityBase
{
    protected EntityBase()
    {
        Id = Guid.CreateVersion7();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = null;
    }

    public Guid Id { get; protected init; }

    public DateTimeOffset CreatedAtUtc { get; protected set; }

    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    protected void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
