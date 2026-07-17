using Myrmex.Modules.Wms.Domain;

namespace Myrmex.Tests.Wms.Domain;

public sealed class ExternalImportStateTests
{
    [Fact]
    public void Create_UsesContentEqualityAndDefensiveVersionCopies()
    {
        byte[] sourceVersion = [1, 2, 3];
        ExternalImportState state = ExternalImportState.Create(
            Guid.NewGuid(),
            sourceVersion,
            DateTimeOffset.Parse("2026-06-27T15:00:00+03:00"));

        sourceVersion[0] = 9;
        byte[] exposedVersion = state.DataVersion!;
        exposedVersion[1] = 9;

        Assert.True(state.HasDataVersion([1, 2, 3]));
        Assert.Equal(new byte[] { 1, 2, 3 }, state.DataVersion);
        Assert.Equal(DateTimeOffset.Parse("2026-06-27T12:00:00Z"), state.ImportedAtUtc);
    }

    [Fact]
    public void Restore_AllowsLegacyUnknownVersionUntilCurrentImportIsRecorded()
    {
        ExternalImportState state = ExternalImportState.Restore(
            Guid.NewGuid(),
            dataVersion: null,
            DateTimeOffset.Parse("2026-06-27T12:00:00Z"));

        Assert.Null(state.DataVersion);
        Assert.False(state.HasDataVersion([1]));

        state.RecordImport([4, 5], DateTimeOffset.Parse("2026-06-27T15:05:00+03:00"));

        Assert.True(state.HasDataVersion([4, 5]));
        Assert.Equal(DateTimeOffset.Parse("2026-06-27T12:05:00Z"), state.ImportedAtUtc);
    }
}
