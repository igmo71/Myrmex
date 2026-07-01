# WebApp Localization

Durable conventions for localization in the Myrmex WebApp.

## Localization Technology

Myrmex WebApp uses ASP.NET Core localization with `.resx` resources. WebApp components use `IStringLocalizer<SharedResource>` with the marker class at `Myrmex.WebApp/Localization/SharedResource.cs`.

The shared resource files are:

- `Myrmex.WebApp/Resources/Localization/SharedResource.resx`
- `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`

The default UI culture is `ru-RU`. Supported UI cultures are `ru-RU` and `en-US`. The neutral `SharedResource.resx` is the English fallback. MudBlazor built-in component labels are provided by `MudBlazor.Translations`.

## Resource Keys

Use stable semantic keys rather than source text as keys. Every key must be present in the neutral, `ru-RU`, and `en-US` resource files. Placeholders must be identical across all three files.

Use an existing prefix where possible:

- `Common.*`
- `Nav.*`
- `Home.*`
- `OneC.*`
- `Warehouse.*`
- `Zone.*`
- `StorageLocation.*`
- `Sku.*`
- `UnitOfMeasure.*`
- `InventoryBalance.*`
- `InventoryLedger.*`
- `InventoryTransfer.*`
- `InventoryCount.*`

## Localization Scope

Localize WebApp user-facing text, including:

- Page titles, headings, and descriptions
- Table and grid column captions
- Form labels, placeholders, and validation messages shown by the WebApp
- Dialog titles, button labels, and menu labels
- Snackbars and empty, loading, and error text
- User-facing display labels for statuses

Do not localize:

- Imported business data from 1C
- Database values such as SKU names, warehouse names, storage location codes, unit-of-measure names, descriptions, and external identifiers
- API routes or API `code` values
- Domain reason codes or integration reason identifiers
- Sorting keys
- Stable enum or string identifiers used in contracts, persistence, or API behavior

## Error and Status Display

Do not change stable API or domain identifiers for localization. When a technical identifier needs a user-facing label, map it to a localized display string in the WebApp layer. Do not localize by changing backend contracts.

## Culture-Aware Formatting

Use the active culture for user-facing number, date, and quantity formatting. Do not use invariant formatting for normal UI display unless the value is a technical identifier, protocol value, or stable machine-readable value.

## Feature Planning

Future WebApp UI features must include localization tasks when they add or change user-facing text. Feature specifications and plans must describe localization impact when UI text is added.

Localization-only work must not change domain logic, API behavior, routes, database schema, migrations, sorting, pagination, or imported data.
