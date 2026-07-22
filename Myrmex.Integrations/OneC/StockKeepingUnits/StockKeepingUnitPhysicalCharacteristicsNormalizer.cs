using Myrmex.Integrations.OneC.UnitsOfMeasure;

namespace Myrmex.Integrations.OneC.StockKeepingUnits;

internal static class StockKeepingUnitPhysicalCharacteristicsNormalizer
{
    private const int PersistenceScale = 12;
    private const decimal MaximumPersistedValue = 9999999999999999.999999999999m;

    public static Result Normalize(
        StockKeepingUnitSourceRecord source,
        IReadOnlyDictionary<Guid, UnitOfMeasureSourceRecord> units)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(units);

        List<Issue> issues = [];
        decimal? weightKilograms = Normalize(
            Characteristic.Weight,
            expectedMeasurementType: "Вес",
            source.ВесИспользовать,
            source.ВесЧислитель,
            source.ВесЗнаменатель,
            source.ВесЕдиницаИзмерения_Key,
            units,
            issues);
        decimal? lengthMetres = Normalize(
            Characteristic.Length,
            expectedMeasurementType: "Длина",
            source.ДлинаИспользовать,
            source.ДлинаЧислитель,
            source.ДлинаЗнаменатель,
            source.ДлинаЕдиницаИзмерения_Key,
            units,
            issues);
        decimal? areaSquareMetres = Normalize(
            Characteristic.Area,
            expectedMeasurementType: "Площадь",
            source.ПлощадьИспользовать,
            source.ПлощадьЧислитель,
            source.ПлощадьЗнаменатель,
            source.ПлощадьЕдиницаИзмерения_Key,
            units,
            issues);
        decimal? volumeCubicMetres = Normalize(
            Characteristic.Volume,
            expectedMeasurementType: "Объем",
            source.ОбъемИспользовать,
            source.ОбъемЧислитель,
            source.ОбъемЗнаменатель,
            source.ОбъемЕдиницаИзмерения_Key,
            units,
            issues);

        return new Result(
            weightKilograms,
            lengthMetres,
            areaSquareMetres,
            volumeCubicMetres,
            issues);
    }

    private static decimal? Normalize(
        Characteristic characteristic,
        string expectedMeasurementType,
        bool use,
        decimal? sourceNumerator,
        decimal? sourceDenominator,
        Guid? unitExternalRefKey,
        IReadOnlyDictionary<Guid, UnitOfMeasureSourceRecord> units,
        ICollection<Issue> issues)
    {
        if (!use)
        {
            return null;
        }

        if (!sourceNumerator.HasValue || !sourceDenominator.HasValue)
        {
            issues.Add(new Issue(characteristic, IssueReason.MissingSourceRatio, unitExternalRefKey));
            return null;
        }

        if (sourceDenominator.Value == 0m)
        {
            issues.Add(new Issue(characteristic, IssueReason.ZeroSourceDenominator, unitExternalRefKey));
            return null;
        }

        if (!unitExternalRefKey.HasValue || unitExternalRefKey.Value == Guid.Empty)
        {
            issues.Add(new Issue(characteristic, IssueReason.MissingUnitReference, unitExternalRefKey));
            return null;
        }

        if (!units.TryGetValue(unitExternalRefKey.Value, out UnitOfMeasureSourceRecord? unit))
        {
            issues.Add(new Issue(characteristic, IssueReason.UnitNotFound, unitExternalRefKey));
            return null;
        }

        if (unit.DeletionMark)
        {
            issues.Add(new Issue(characteristic, IssueReason.UnitDeletionMarked, unitExternalRefKey));
            return null;
        }

        if (!string.Equals(
                unit.ТипИзмеряемойВеличины,
                expectedMeasurementType,
                StringComparison.Ordinal))
        {
            issues.Add(new Issue(characteristic, IssueReason.MeasurementTypeMismatch, unitExternalRefKey));
            return null;
        }

        if (!unit.Числитель.HasValue || !unit.Знаменатель.HasValue)
        {
            issues.Add(new Issue(characteristic, IssueReason.MissingUnitRatio, unitExternalRefKey));
            return null;
        }

        if (unit.Числитель.Value == 0m)
        {
            issues.Add(new Issue(characteristic, IssueReason.ZeroUnitNumerator, unitExternalRefKey));
            return null;
        }

        if (unit.Знаменатель.Value == 0m)
        {
            issues.Add(new Issue(characteristic, IssueReason.ZeroUnitDenominator, unitExternalRefKey));
            return null;
        }

        try
        {
            decimal value = checked(
                sourceNumerator.Value / sourceDenominator.Value *
                unit.Числитель.Value / unit.Знаменатель.Value);
            decimal persistedValue = decimal.Round(value, PersistenceScale, MidpointRounding.ToEven);
            if (persistedValue > MaximumPersistedValue || persistedValue < -MaximumPersistedValue)
            {
                issues.Add(new Issue(characteristic, IssueReason.PersistenceOverflow, unitExternalRefKey));
                return null;
            }
            if (value != 0m && persistedValue == 0m)
            {
                issues.Add(new Issue(characteristic, IssueReason.PersistenceScaleUnderflow, unitExternalRefKey));
                return null;
            }
            return persistedValue;
        }
        catch (OverflowException)
        {
            issues.Add(new Issue(characteristic, IssueReason.ArithmeticOverflow, unitExternalRefKey));
            return null;
        }
    }

    internal sealed record Result(
        decimal? WeightKilograms,
        decimal? LengthMetres,
        decimal? AreaSquareMetres,
        decimal? VolumeCubicMetres,
        IReadOnlyList<Issue> Issues);

    internal sealed record Issue(
        Characteristic Characteristic,
        IssueReason Reason,
        Guid? UnitExternalRefKey);

    internal enum Characteristic
    {
        Weight,
        Length,
        Area,
        Volume
    }

    internal enum IssueReason
    {
        MissingSourceRatio,
        ZeroSourceDenominator,
        MissingUnitReference,
        UnitNotFound,
        UnitDeletionMarked,
        MeasurementTypeMismatch,
        MissingUnitRatio,
        ZeroUnitNumerator,
        ZeroUnitDenominator,
        ArithmeticOverflow,
        PersistenceOverflow,
        PersistenceScaleUnderflow
    }
}
