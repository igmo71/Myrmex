using Myrmex.Core.Domain;

namespace Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

internal sealed class StorageLocationStatus : EntityBase, IActivatable
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private StorageLocationStatus(
        string code,
        string name,
        string? description,
        bool isSystem,
        int sortOrder)
    {
        Code = code;
        Name = name;
        Description = description;
        IsSystem = isSystem;
        SortOrder = sortOrder;
    }

    private StorageLocationStatus()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static StorageLocationStatus CreateSystem(
        string code,
        string name,
        string? description,
        int sortOrder)
    {
        return new StorageLocationStatus(
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description),
            isSystem: true,
            sortOrder);
    }
}
