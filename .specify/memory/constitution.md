<!--
Sync Impact Report
- Version: template -> 1.0.0
- Principles: placeholders -> Clear Warehouse Behavior; Explicit Ownership; Outcome-First Simplicity
- Added sections: none
- Removed sections: two unused template sections and two unused principle slots
- Templates requiring updates: none; no new mandatory template structure introduced
- Follow-up TODOs: none
-->
# Myrmex Constitution

## Core Principles

### I. Clear Warehouse Behavior
Myrmex MUST keep warehouse behavior and terminology clear, correct, and consistent with the real
workflow being demonstrated. Names, states, and rules make the warehouse meaning apparent, and
behavior preserves the relevant warehouse invariants.

### II. Explicit Ownership
Every feature and its data MUST have an explicit owning module. Other modules interact through
deliberate, narrow boundaries and do not silently take responsibility for data or rules they do
not own.

### III. Outcome-First Simplicity
Work MUST use the simplest implementation that delivers the current demonstrable warehouse
outcome. Add abstraction, infrastructure, and generalized mechanisms only for a concrete current
need. Verification is proportional to the likelihood and impact of failure, using the smallest
evidence that gives reasonable confidence.

## Governance

This constitution guides scope and implementation decisions for the warehouse MVP. Changes record
their reason and update the semantic version: major for changed or removed principles, minor for
added guidance, and patch for clarification. Feature plans record only relevant conventions.
Review checks the three principles in proportion to scope and risk.

**Version**: 1.0.0 | **Ratified**: 2026-07-21 | **Last Amended**: 2026-07-21
