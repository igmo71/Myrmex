# Myrmex WMS Topology Pattern Memory

This document records WMS Topology as the current reference vertical slice for future Myrmex planning.

## Reference Slice

WMS Topology is the accepted reference slice.

Covered concepts:

- Warehouse.
- Zone.
- StorageLocation.

These concepts are the baseline for comparing future WMS domain language, UI component patterns, API error handling, and testing expectations.

## Boundaries

Issue #30 documents the reference slice only. It must not expand WMS Topology and must not implement Catalog, SKU, Inventory, Receiving, Integration, or other roadmap areas.

## UI Pattern Scope

UI component patterns may be documented as accepted guidance for the current WMS Topology experience. Documentation must not request UI implementation or UI behavior changes for issue #30.

## Future Use

Future vertical slices should compare their naming, handler structure, client behavior, and documentation against this reference slice before adding new patterns.
