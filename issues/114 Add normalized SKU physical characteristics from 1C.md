## Specify execution constraints

* Work in the currently checked-out branch.
* Do not create, rename, or switch Git branches.
* The current branch already corresponds to GitHub issue `#114`.
* Use feature number `114` for the specification directory.
* Create the specification under `specs/114-normalized-sku-physical-characteristics`.
* Do not select the next available specification number.
* Do not derive the specification number from existing `specs` directories.
* Do not run the standard branch-creation workflow if it would create or switch a branch.
* Do not commit or push changes.

## Goal

Add normalized physical characteristics of the base unit of an SKU to Myrmex and populate them through the existing 1C SKU synchronization flow.

After synchronization, a user must be able to see the available physical characteristics of an SKU in Myrmex.

## Scope

Support the following independent, optional characteristics:

* weight in kilograms;
* length in metres;
* area in square metres;
* volume in cubic metres.

The values describe one base unit of the SKU. For this issue, incoming document quantities are assumed to use the SKU base unit.

Use the corresponding 1C fields:

* `ВесИспользовать`, `ВесЧислитель`, `ВесЗнаменатель`, `ВесЕдиницаИзмерения_Key`;
* `ДлинаИспользовать`, `ДлинаЧислитель`, `ДлинаЗнаменатель`, `ДлинаЕдиницаИзмерения_Key`;
* `ПлощадьИспользовать`, `ПлощадьЧислитель`, `ПлощадьЗнаменатель`, `ПлощадьЕдиницаИзмерения_Key`;
* `ОбъемИспользовать`, `ОбъемЧислитель`, `ОбъемЗнаменатель`, `ОбъемЕдиницаИзмерения_Key`.

Resolve units using `Catalog_УпаковкиЕдиницыИзмерения`. Its `ТипИзмеряемойВеличины`, `Числитель`, and `Знаменатель` define the measurement type and conversion factor to the canonical unit.

Normalization must remain inside the 1C integration boundary. The WMS domain must store only normalized values and must not depend on 1C unit identifiers or conversion details.

A characteristic is absent when its `Использовать` flag is false or its source value cannot be validly resolved. Absence must be represented separately from zero.

Volume is an independent source value and must not be derived from length or other dimensions.

Display the available normalized values in the existing SKU WebApp UI. Values synchronized from 1C are read-only in this issue.

## Out of scope

* a new general import or synchronization mechanism;
* manual editing of synchronized characteristics;
* packaging levels or SKU packaging;
* width, height, depth, or packaging dimensions;
* LPN or handling units;
* storage-location capacity validation;
* putaway calculation or automatic location selection;
* a general-purpose physical-units framework;
* importing all fields of `Catalog_УпаковкиЕдиницыИзмерения`;
* changing receipt or inventory processes.

## Acceptance outcomes

1. Existing SKU synchronization updates weight, length, area, and volume when corresponding data is available in 1C.
2. Values are stored in canonical units: kilograms, metres, square metres, and cubic metres.
3. Missing characteristics remain absent and are not stored as known zero values.
4. Volume can be populated when linear dimensions are unavailable.
5. Repeated SKU synchronization updates changed values and clears values no longer enabled in 1C.
6. The SKU WebApp UI displays available characteristics with clear units and handles missing values without errors.
7. The implementation does not introduce packaging support or a separate import subsystem.
