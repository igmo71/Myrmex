using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

internal static class ReceivingOrderConcurrencyDiagnostics
{
    public static void LogWarning(
        ILogger logger,
        DbUpdateConcurrencyException exception,
        string path,
        Guid? receivingOrderId)
    {
        if (exception.Entries.Count == 0)
        {
            logger.LogWarning(
                exception,
                "Receiving Draft concurrency conflict in {Path} for receiving order {ReceivingOrderId}. The exception did not include conflicting entries.",
                path,
                receivingOrderId);
            return;
        }

        foreach (var entry in exception.Entries)
        {
            string primaryKeyValues = FormatProperties(
                entry.Metadata.FindPrimaryKey()?.Properties ?? [],
                property => entry.CurrentValues[property.Name]);
            string concurrencyValues = string.Join(
                ", ",
                entry.Metadata.GetProperties()
                    .Where(property => property.IsConcurrencyToken || property.Name == "RowVersion")
                    .Select(property =>
                        $"{property.Name}.Original={FormatValue(entry.OriginalValues[property.Name])}; " +
                        $"{property.Name}.Current={FormatValue(entry.CurrentValues[property.Name])}"));

            logger.LogWarning(
                exception,
                "Receiving Draft concurrency conflict in {Path} for receiving order {ReceivingOrderId}. Conflicting entity {EntityType}; state {EntityState}; primary key {PrimaryKeyValues}; row-version values {ConcurrencyValues}.",
                path,
                receivingOrderId,
                entry.Metadata.ClrType.FullName,
                entry.State,
                primaryKeyValues,
                concurrencyValues);
        }
    }

    private static string FormatProperties<TValue>(
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        Func<Microsoft.EntityFrameworkCore.Metadata.IProperty, TValue> valueSelector) =>
        string.Join(", ", properties.Select(property => $"{property.Name}={FormatValue(valueSelector(property))}"));

    private static string FormatValue(object? value) => value switch
    {
        null => "<null>",
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>"
    };
}
