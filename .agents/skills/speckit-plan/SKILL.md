---
name: "speckit-plan"
description: "Execute the implementation planning workflow using the plan template to generate design artifacts."
compatibility: "Requires spec-kit project structure with .specify/ directory"
metadata:
  author: "github-spec-kit"
  source: "templates/commands/plan.md"
---


## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Constitution Guard

The Myrmex constitution governs every action and extension hook. This guard overrides
later automatic or mandatory hook instructions. Skip and report any hook that would
build, run tests, generate or apply migrations, create a commit, or create or publish a
pull request, even when marked mandatory. Do not emit `EXECUTE_COMMAND` for a skipped
hook.

## Pre-Execution Checks

**Check for extension hooks (before planning)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_plan` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- When constructing slash commands from hook command names, replace dots (`.`) with hyphens (`-`). For example, `speckit.audit.policy` → `/speckit-audit-policy`.
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. **Setup**: Run `.specify/scripts/powershell/setup-plan.ps1 -Json` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, SPECS_DIR, BRANCH. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load context**: Read FEATURE_SPEC and `.specify/memory/constitution.md`. Load IMPL_PLAN template (already copied).

3. **Inspect existing Myrmex patterns**: Read the smallest relevant set of source files and
   existing documentation. Prefer established module, vertical-slice, dispatcher, EF Core,
   Minimal API, and Blazor patterns over introducing alternatives.

4. **Write the required plan.md**:
   - Fill Technical Context and use `NEEDS CLARIFICATION` only for material unknowns.
   - Fill the Constitution Check and stop on unjustified violations.
   - Describe persistence and migration impact without generating or applying migrations.
   - Put builds, migrations, manual acceptance, commits, and pull requests only in the
     non-executable Developer Actions handoff.

5. **Select supporting artifacts proportionally**:
   - Create `research.md` only for material repository-specific unknowns that existing
     Myrmex code and documentation cannot resolve.
   - Create `data-model.md` only for new or materially changed persistent domain data.
   - Create `contracts/` only for new or changed external or cross-module contracts.
   - Create `quickstart.md` only when a reusable developer-performed manual-verification
     guide adds concrete value.
   - Record why each optional artifact was created or omitted. Never create empty or
     ceremonial artifacts.

6. Re-evaluate the Constitution Check after every material design decision.

## Mandatory Post-Execution Hooks

**You MUST complete this section before reporting completion to the user.**

Check if `.specify/extensions.yml` exists in the project root.
- If it does not exist, or no hooks are registered under `hooks.after_plan`, skip to the Completion Report.
- If it exists, read it and look for entries under the `hooks.after_plan` key.
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue to the Completion Report.
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- When constructing slash commands from hook command names, replace dots (`.`) with hyphens (`-`). For example, `speckit.audit.policy` → `/speckit-audit-policy`.
- For each executable hook, output the following based on its `optional` flag:
  - **Mandatory hook** (`optional: false`) — **You MUST emit `EXECUTE_COMMAND:` for each mandatory hook**:
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```

## Completion Report

Report the branch, IMPL_PLAN path, optional artifacts created, and concise reasons for
omitted artifacts.

## Phases

### Bounded Research (Optional)

Research only unresolved decisions that materially affect correctness, architecture,
security, persistence, or external compatibility. Search existing Myrmex patterns first.
Do not perform broad best-practice surveys for decisions already established in the
repository.

When research is necessary, consolidate it in `research.md`:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

Do not create `research.md` when no material unknown remains.

### Supporting Design Artifacts (Optional)

1. **Persistent data** → `data-model.md` only when applicable:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Interface contracts** → `contracts/` only when applicable:
   - Identify what interfaces the project exposes to users or other systems
   - Document the contract format appropriate for the project type
   - Prefer existing Myrmex request/response and endpoint conventions

3. **Manual-verification guide** → `quickstart.md` only when reusable:
   - Document concise scenarios for the developer to perform
   - Label any prepared commands as developer-controlled
   - Use links or references to contracts and data model details instead of duplicating them
   - Do not include implementation code, generated migrations, or automated tests

## Key rules

- Use absolute paths for filesystem operations; use project-relative paths for references in documentation
- ERROR on gate failures or unresolved clarifications
- Do not execute developer-controlled operations

## Done When

- [ ] Required plan.md completed and Constitution Check passed
- [ ] Optional artifacts created only where their value is documented
- [ ] Developer-controlled operations represented only as handoff notes
- [ ] Extension hooks dispatched or skipped according to the rules in Mandatory Post-Execution Hooks above
- [ ] Completion reported with created and intentionally omitted artifacts
