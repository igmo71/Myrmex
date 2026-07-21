# Implementation Plan: Normalized SKU Physical Characteristics

**Branch**: `114-add-normalized-sku-physical-characteristics-from-1c` | **Date**: 2026-07-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/114-normalized-sku-physical-characteristics/spec.md`

**Planning Status**: Complete and ready for task generation

## Summary

Extend the existing full and reactive 1C SKU synchronization paths to read four optional physical characteristics and normalize them inside `Myrmex.Integrations`. Use the provisional, evidence-backed source-contract rule `source numerator / source denominator × unit numerator / unit denominator`, then pass only nullable canonical kilograms, metres, square metres, and cubic metres into the WMS-owned SKU aggregate.

Extend the existing SKU response and edit dialog with a read-only display. Do not add list/grid columns, a new screen, import workflow, diagnostics subsystem, persistent integration cache, packaging behavior, performance work, testing infrastructure, or a generalized measurement framework.

Before coding the normalizer, verify the formula and exact measurement-type tokens against additional representative linked 1C records. This is an early implementation prerequisite, not a planning or task-generation blocker. If evidence contradicts the rule, update [research.md](research.md), this plan, and the normalizer design before continuing.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core Minimal APIs, Entity Framework Core SQL Server 10.0.10, the existing 1C OData HTTP transport, Blazor, and MudBlazor 9

**Storage**: Existing SQL Server WMS `StockKeepingUnits` table with four new nullable canonical-value columns planned as `decimal(28,12)`, subject to confirmation during the early representative-record verification; no integration persistence

**Testing**: The repository has no tracked test project. Use proportional verification: additional read-only 1C sample comparison before normalizer coding, then the existing build and application paths for focused API, synchronization, persistence, logging, and WebApp smoke validation. Do not introduce testing infrastructure.

**Target Platform**: Existing ASP.NET Core API and Blazor WebApp, locally orchestrated through the current Aspire host

**Project Type**: Modular-monolith web application with separate WMS, integration, shared-contract, API, and WebApp projects

**Performance Goals**: None added; no benchmark, load-test, or performance-baseline work is required

**Constraints**: Keep raw 1C names, unit references, measurement types, ratios, and arithmetic inside `Myrmex.Integrations`; prefer the existing OData transport and ordinary nullable numeric source fields; WMS stores only four canonical nullable values; absence remains distinct from numeric zero; a zero source numerator is valid, a zero measurement-unit numerator is invalid, and all zero denominators are invalid; one bad characteristic neither rejects the SKU nor blocks other characteristics; the normalizer returns structured issues and the existing synchronization caller logs each once; display values read-only only in the existing SKU edit/details experience

**Scale/Scope**: Four optional characteristics on one existing aggregate and table, the existing full/reactive SKU synchronization paths, one existing response contract, and one existing edit dialog

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-Research Gate

- **I. Clear Warehouse Behavior — PASS**: The feature uses explicit warehouse terms and fixed canonical units, treats each value as applying to one SKU base unit, keeps volume independent, and preserves the difference between absent and zero.
- **II. Explicit Ownership — PASS**: `Myrmex.Integrations` owns source interpretation and normalization; `Myrmex.Modules.Wms` owns canonical SKU state; `Myrmex.Shared` exposes only the read model; `Myrmex.WebApp` only displays it.
- **III. Outcome-First Simplicity — PASS**: The design reuses the existing source readers, full/reactive SKU synchronization, WMS aggregate/table/import command, logger, response, endpoint, and edit dialog.

### Post-Design Gate

- **I. Clear Warehouse Behavior — PASS**: [data-model.md](data-model.md) names every value with its canonical unit, defines independent state transitions, keeps numeric zero distinct from absence, and does not derive volume.
- **II. Explicit Ownership — PASS**: [onec-normalization-contract.md](contracts/onec-normalization-contract.md) confines source DTOs, unit factors, type matching, formula application, and normalization issues to the 1C integration boundary. WMS receives only canonical nullable decimals.
- **III. Outcome-First Simplicity — PASS**: The design adds one SKU-specific normalizer and four ordinary nullable decimal columns, reuses the existing OData transport, request-scoped unit reads, and caller-owned logging, extends the existing response/edit dialog, and adds no custom resilient parser, generalized framework, persistent cache, workflow, screen, diagnostics subsystem, test infrastructure, or performance work.

All constitution gates pass. No exception or complexity justification is required.

## Project Structure

### Documentation (this feature)

```text
specs/114-normalized-sku-physical-characteristics/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── onec-normalization-contract.md
│   └── sku-details-ui-contract.md
└── tasks.md             # Generated separately by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.Integrations/
└── OneC/
    ├── StockKeepingUnits/
    │   ├── StockKeepingUnitSourceRecord.cs
    │   ├── StockKeepingUnitOneCSource.cs
    │   ├── StockKeepingUnitOneCImport.cs
    │   ├── StockKeepingUnitOneCSynchronizer.cs
    │   └── StockKeepingUnitPhysicalCharacteristicsNormalizer.cs  # planned SKU-specific helper
    └── UnitsOfMeasure/
        ├── UnitOfMeasureSourceRecord.cs
        └── UnitOfMeasureOneCSource.cs

Myrmex.Modules.Wms/
├── Catalog/
│   ├── Domain/StockKeepingUnits/StockKeepingUnit.cs
│   └── Features/
│       ├── Imports/ImportStockKeepingUnits.cs
│       └── StockKeepingUnits/StockKeepingUnitDetails.cs
└── Infrastructure/Persistence/
    ├── Configurations/StockKeepingUnitConfiguration.cs
    └── Migrations/

Myrmex.Shared/
└── Wms/Catalog/StockKeepingUnitDetails.cs

Myrmex.WebApp/
├── Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor
└── Resources/Localization/SharedResource*.resx
```

**Structure Decision**: Extend the current vertical slice end to end. Add one feature-specific normalizer in the 1C SKU integration boundary; do not introduce a cross-module units abstraction. The existing SKU response already reaches the existing edit dialog, while grid rendering and lookup contracts remain unchanged.

## Phase 0: Research Output

[research.md](research.md) records the provisional formula, supplied factor evidence, implementation prerequisite, ownership, existing-transport source handling, source-zero/unit-zero rules, caller-owned diagnostics, provisional persistence mapping, refresh, API/UI, and proportional-verification decisions. No planning clarification remains.

## Phase 1: Design & Contracts

- [data-model.md](data-model.md) defines the persistent SKU fields, transient integration records, validation rules, and synchronization state transitions.
- [onec-normalization-contract.md](contracts/onec-normalization-contract.md) defines the exact source fields, provisional arithmetic, resolution behavior, and non-fatal diagnostics contract.
- [sku-details-ui-contract.md](contracts/sku-details-ui-contract.md) defines the existing API response extension and read-only edit-dialog behavior.
- [quickstart.md](quickstart.md) defines the early formula check and focused end-to-end validation scenarios using existing workflows.

The first implementation task MUST validate additional linked 1C records before the normalizer behavior is coded. Contradictory evidence stops formula-dependent implementation and requires updating the research and plan; it does not prevent task generation.

## Complexity Tracking

No constitution violation is present.
