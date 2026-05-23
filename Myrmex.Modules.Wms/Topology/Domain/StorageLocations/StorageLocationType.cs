using Myrmex.Core.Domain;

namespace Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

internal sealed class StorageLocationType : EntityBase
{
    public const int MaxCodeLength = DomainTextLengths.Code;
    public const int MaxNameLength = DomainTextLengths.Name;
    public const int MaxDescriptionLength = DomainTextLengths.Description;

    private StorageLocationType(
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

    private StorageLocationType()
    {
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }

    public int SortOrder { get; private set; }

    public static StorageLocationType CreateSystem(
        string code,
        string name,
        string? description,
        int sortOrder)
    {
        return new StorageLocationType(
            DomainText.NormalizeCode(code),
            DomainText.NormalizeRequiredText(name),
            DomainText.NormalizeOptionalText(description),
            isSystem: true,
            sortOrder);
    }
}