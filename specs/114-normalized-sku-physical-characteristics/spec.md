# Feature Specification: Normalized SKU Physical Characteristics

**Feature Branch**: `114-add-normalized-sku-physical-characteristics-from-1c`

**Created**: 2026-07-21

**Status**: Draft

**Input**: User description: "Add normalized SKU physical characteristics from 1C as defined in issue 114."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Synchronize Physical Characteristics (Priority: P1)

As a warehouse user, I need each SKU's available physical characteristics to be synchronized from 1C in standard units so that Myrmex has consistent values for operational reference.

**Why this priority**: Reliable normalized data is the foundation for every user-facing and future operational use of these characteristics.

**Independent Test**: Synchronize SKUs containing each supported characteristic in different valid source units, then verify that every available value is stored against one base unit of the SKU in its required canonical unit.

**Acceptance Scenarios**:

1. **Given** an SKU whose enabled weight, length, area, and volume values can all be validly resolved, **When** the existing SKU synchronization runs, **Then** all four values are recorded in kilograms, metres, square metres, and cubic metres respectively.
2. **Given** an enabled characteristic expressed in a supported non-canonical unit, **When** the SKU is synchronized, **Then** its value is converted using the referenced 1C unit definition and recorded in the corresponding canonical unit.
3. **Given** an SKU for which only volume is available, **When** the SKU is synchronized, **Then** volume is recorded without requiring any linear dimension.

---

### User Story 2 - View Available Characteristics (Priority: P2)

As a warehouse user viewing an SKU in the existing WebApp SKU details or edit view, I need to see its available normalized physical characteristics with clear units so that I can understand the physical properties of one base unit.

**Why this priority**: Synchronized data delivers immediate user value only when it is visible and unambiguous in the existing SKU experience.

**Independent Test**: Open the existing SKU details or edit view for records with all, some, and none of the supported characteristics and verify that available values and units are clear, missing values cause no error, and no edit action is offered. SKU lists, lookups, and grids do not need to display the characteristics.

**Acceptance Scenarios**:

1. **Given** an SKU with one or more synchronized characteristics, **When** a user opens that SKU's existing details or edit view, **Then** each available value is displayed with its canonical unit and is identified as applying to one base unit.
2. **Given** an SKU with one or more absent characteristics, **When** a user opens that SKU's existing details or edit view, **Then** the page remains usable and does not present an absent value as zero.
3. **Given** a synchronized characteristic displayed on the SKU details or edit view, **When** the user views it, **Then** no control permits the user to edit that value.

---

### User Story 3 - Refresh Changed or Removed Values (Priority: P3)

As a data administrator, I need repeated synchronization to replace changed values and remove characteristics no longer valid in 1C so that users do not rely on stale data.

**Why this priority**: Maintaining correctness over time is essential once the initial synchronization and display flows are available.

**Independent Test**: Synchronize an SKU, change one source value, disable another, make a third unresolvable, and synchronize again; verify that the first is updated while the latter two become absent and unaffected characteristics remain unchanged.

**Acceptance Scenarios**:

1. **Given** an SKU with a previously synchronized characteristic whose source value or unit has changed validly, **When** synchronization runs again, **Then** the stored normalized value is replaced with the newly resolved value.
2. **Given** a previously synchronized characteristic whose 1C `Использовать` flag is now false, **When** synchronization runs again, **Then** that characteristic becomes absent in Myrmex.
3. **Given** a previously synchronized characteristic whose source data can no longer be validly resolved, **When** synchronization runs again, **Then** that characteristic becomes absent without affecting independently valid characteristics.

### Edge Cases

- A characteristic whose `Использовать` flag is false is absent even if its remaining source fields contain values.
- A characteristic is absent when its referenced unit cannot be found, its measurement type does not match the characteristic, or its source or conversion denominator is zero.
- A missing, incomplete, non-numeric, or otherwise invalid source value makes only that characteristic absent; it does not reject the SKU or suppress other valid characteristics, and the normalization problem is reported through existing synchronization diagnostics or logging.
- A valid normalized value of zero remains a known zero and is distinguishable from an absent characteristic.
- Very small or large valid values retain enough precision to represent the source conversion without silently becoming absent or zero.
- Volume remains independent: it can exist without length, and it is never calculated from length or any other dimension.
- Repeated synchronization with unchanged source data leaves the user's displayed values unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support four independent, optional physical characteristics for one base unit of an SKU: weight, length, area, and volume.
- **FR-002**: The existing 1C SKU synchronization MUST be the only import path introduced by this feature.
- **FR-003**: For each characteristic, synchronization MUST honor its corresponding 1C use flag, numerator, denominator, and measurement-unit reference: `ВесИспользовать`, `ВесЧислитель`, `ВесЗнаменатель`, `ВесЕдиницаИзмерения_Key`; `ДлинаИспользовать`, `ДлинаЧислитель`, `ДлинаЗнаменатель`, `ДлинаЕдиницаИзмерения_Key`; `ПлощадьИспользовать`, `ПлощадьЧислитель`, `ПлощадьЗнаменатель`, `ПлощадьЕдиницаИзмерения_Key`; and `ОбъемИспользовать`, `ОбъемЧислитель`, `ОбъемЗнаменатель`, `ОбъемЕдиницаИзмерения_Key`.
- **FR-004**: Synchronization MUST resolve referenced units from `Catalog_УпаковкиЕдиницыИзмерения` and use `ТипИзмеряемойВеличины`, `Числитель`, and `Знаменатель` to validate the measurement type and convert the source value to its canonical unit.
- **FR-005**: The system MUST store weight in kilograms, length in metres, area in square metres, and volume in cubic metres.
- **FR-006**: The WMS domain MUST store only normalized characteristic values and MUST NOT retain or depend on 1C unit identifiers, 1C conversion factors, or other 1C-specific conversion details.
- **FR-007**: Each characteristic MUST be absent when its use flag is false or when its source value, denominator, unit reference, unit definition, measurement type, or conversion factor cannot be validly resolved.
- **FR-008**: The system MUST represent absence separately from a known numeric zero.
- **FR-009**: Each characteristic MUST be resolved independently so invalid or absent data for one does not prevent other valid characteristics from being synchronized.
- **FR-010**: Volume MUST be taken only from its independent 1C volume source and MUST NOT be derived from length or other dimensions.
- **FR-011**: Repeated synchronization MUST replace changed valid values and clear previously stored values that are now disabled or unresolvable.
- **FR-012**: The existing SKU details or edit view in the WebApp MUST display every available normalized characteristic with an unambiguous canonical unit and MUST handle absent characteristics without error or misleading zero values; SKU lists, lookups, and grids are not required to display these characteristics.
- **FR-013**: Characteristics populated from 1C MUST be read-only on the SKU details or edit view.
- **FR-014**: This feature MUST NOT add packaging levels, SKU packaging, width, height, depth, packaging dimensions, LPN or handling-unit behavior, storage-capacity validation, putaway calculations, automatic location selection, a general-purpose units framework, or changes to receipt and inventory processes.
- **FR-015**: This feature MUST limit imported unit data to the information needed to validate and normalize the four supported SKU characteristics.
- **FR-016**: An unresolvable characteristic MUST NOT reject the SKU or block other valid characteristics, and its normalization problem MUST be reported through the existing synchronization diagnostics or logging mechanism without introducing a new diagnostics subsystem or workflow.

### Key Entities

- **SKU Physical Characteristics**: The optional weight, length, area, and volume values associated with one base unit of an SKU; each value is either absent or expressed in its fixed canonical unit.
- **1C Characteristic Source**: The use flag, numerator, denominator, and unit reference that together describe one source characteristic for an SKU.
- **1C Measurement Unit Definition**: The referenced measurement type and conversion ratio used only within the 1C integration boundary to validate and normalize a source characteristic.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Across an acceptance set covering every supported characteristic and source unit, 100% of valid source values match the expected canonical values after synchronization.
- **SC-002**: Across cases with disabled, incomplete, mismatched, unknown, or invalid source data, 100% of affected characteristics are absent rather than represented as zero, while independently valid characteristics remain available.
- **SC-003**: After a repeated synchronization, 100% of tested changed values are refreshed and 100% of tested disabled or newly unresolvable values are cleared.
- **SC-004**: Across SKU details or edit views covering all, some, and none of the supported characteristics, 100% of available values are shown read-only with their canonical units, while absent values cause no error and are not presented as zero.
- **SC-005**: Review of delivered scope finds zero new workflows for packaging, manual characteristic editing, separate imports, receipt processing, inventory processing, capacity validation, diagnostics, or putaway selection.

## Assumptions

- Incoming document quantities continue to use the SKU base unit; packaging-level quantity conversion is outside this feature.
- Existing authorization for viewing SKU details also governs access to the displayed characteristics.
- The established 1C synchronization schedule, triggering behavior, failure reporting, and retry behavior remain unchanged.
- Existing numeric precision and rounding policy for business measurements is sufficient for normalized characteristic values and will be applied consistently.
- A source characteristic is valid only when its referenced unit's measurement type matches that characteristic and both the source and conversion ratios are mathematically valid. Zero numerators and normalized zero values remain valid; absence is distinct from numeric zero.
- The expected, but not yet confirmed, conversion interpretation is `source numerator / source denominator × unit numerator / unit denominator`. The implementation plan MUST validate this interpretation against representative real 1C records before implementation.
- The WebApp's existing SKU details or edit view is the presentation location; this feature does not add a separate physical-characteristics screen or require the characteristics on SKU lists, lookups, or grids.
