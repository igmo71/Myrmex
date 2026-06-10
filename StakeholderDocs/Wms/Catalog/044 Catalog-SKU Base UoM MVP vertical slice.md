# Catalog/SKU Base UoM MVP vertical slice

## Issue

GitHub issue: #44

## Context

Myrmex WMS already has Catalog MVP slices for:

* SKU
* Unit of Measure
* SKU Barcode

`StockKeepingUnit` and `UnitOfMeasure` currently exist independently. This feature connects them by adding the SKU's base unit of measure.

The current database is development-only. Existing data does not need production-safe preservation.

## Goal

Add required Base Unit of Measure assignment to `StockKeepingUnit`.

A SKU must reference exactly one base `UnitOfMeasure`.

The base UoM defines the unit in which the SKU's base quantity will be expressed in future inventory, receiving, packaging, and operational workflows.

## Scope

* Add required `BaseUnitOfMeasureId` to `StockKeepingUnit`.
* `BaseUnitOfMeasureId` references an existing `UnitOfMeasure`.
* Validate referenced UoM on SKU create/update.
* Require active UoM for assignment unless existing Catalog behavior suggests otherwise.
* Expose `BaseUnitOfMeasureId` in SKU create/update/get/list contracts.
* Update domain, handlers, endpoints, WebApp Catalog API client, persistence mapping, and focused tests according to existing Catalog/SKU and Catalog/UoM patterns.
* Add the required EF relationship/configuration.

## Persistence and migration workflow

This feature requires a schema change.

The implementation should prepare the EF model/configuration, but EF migration generation and database update are developer-controlled steps.

The agent must not run migration generation or database update automatically. When the implementation is ready, it should stop and recommend the exact commands.

## Design decisions

* `BaseUnitOfMeasureId` is required.
* No nullable production-compatibility transition is needed for this MVP.
* Existing development data may be reset or migrated without preserving old SKU rows.
* Seed/demo data is not part of this issue and will be handled separately.

## Out of scope

* Alternative UoM.
* Conversion factors.
* Packaging.
* Inventory.
* Receiving.
* LPN.
* UI implementation.
* Seed/demo data.

## Success criteria

* SKU create requires valid `BaseUnitOfMeasureId`.
* SKU update can change `BaseUnitOfMeasureId`.
* SKU get/list return `BaseUnitOfMeasureId`.
* Referenced UoM existence and active-state rules are validated.
* Existing SKU, UoM, and SKU Barcode behavior remains valid.
* EF model contains the required SKU → UoM relationship.
