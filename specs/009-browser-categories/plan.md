# Implementation Plan: Device categories in the browser

**Branch**: `009-browser-categories` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/009-browser-categories/spec.md`

## Summary

The Instruments, Audio effects and MIDI effects tabs become lists grouped by category, and
this is on by default. Each category is a foldable section header in today's group-header
style. The work splits five ways:

1. **Categories (US1)**: a new pure `DeviceCategories` class in `Nota.Application` holds:
   - the confirmed category lists (Instruments 5, Audio effects 8, MIDI effects 2) with
     their built-in members;
   - a fallback, `Other`;
   - a plug-in mapping from the category a plug-in reports (VST3 sub-categories; AU type
     names map to Other) (R1, R2).

   `BrowserViewModel.RebuildVisible` gains a category branch. It orders devices by
   category, then source, then favourite, then short name, and emits one header per
   non-empty category. Today's flat branch stays untouched (R3).
2. **Off switch (US2)**: "Group by category" in the ⋮ menu, persisted as
   `Settings.BrowserCategoryView` (default true, which also covers existing users) (R5).
3. **Folding (US3)**:
   - headers get a caret and toggle on click;
   - folds are kept per tab in `Settings.BrowserFoldedCategories`;
   - while a search is active, folds are ignored and left untouched (R4);
   - the status-line counts include folded devices (R6).
4. **Plug-ins (US4)**: each plug-in row gets its category at rebuild. Inside a category,
   built-ins come before plug-ins when "Group by source" is on; otherwise they interleave
   by short name (R2, R3).
5. **Guard and docs**:
   - a new `DesignTokenCheck.RunBrowser` (every built-in categorised, no stale names,
     18 / 26 / 6);
   - behavioural smoke checks with a fake plug-in catalogue (R7);
   - CHANGELOG, FEATURES.md, DESIGN.md and the nota-instrument skill (R8).

It is UI and settings only: no engine, C ABI, project-format or shortcut change.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Studio) and the `nota-design` skill.
  Headers reuse `GroupLabel` and the group row. The caret reuses the device-row disclosure
  glyph.
- **A built-in device** → the `nota-instrument` skill. Its registration checklist gains
  "add the device to `DeviceCategories`".
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX headers on new files,
  no new dependency.

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only).

**Primary Dependencies**: Avalonia 12.0.3 (existing). No new dependency.

**Storage**: `settings.json` gains `BrowserCategoryView` (bool, default true) and
`BrowserFoldedCategories` (list of `tab:name` keys). No project-format change.

**Audio-Thread Impact**: None. This is a UI-thread list projection; nothing crosses the
C ABI.

**Testing**: `tests/Nota.SmokeTest`:
- **New**: `DesignTokenCheck.Browser.cs` (`RunBrowser`, wired into `Program.cs`).
- **Behavioural checks** in the existing browser block of `Program.cs`, with a new
  `FakePluginCatalog` in `BrowserTestSupport.cs`.
- **Updated**: the one existing assertion whose first header changes from `BUILT-IN` to
  `Synths`.
- **Screenshots**: the device tabs under Xvfb in all four themes.

**Target Platform**: macOS 13+, Windows 10+, Linux, as today.

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: no measurable change. Ordering at most a few hundred device rows
with a multi-key sort is negligible next to today's rebuild. The palette is unaffected
because it reads the unchanged trees.

**Constraints**:
- The flat view must reproduce today's sequence exactly (SC-003).
- No new colour, radius or icon style (FR-008).
- Device rows, preset expansion, drag, preview, favourites and tags are unchanged
  (FR-007, FR-010).

**Scale/Scope**:
- 50 built-in devices across 15 categories, plus any number of plug-ins;
- about 6 source files touched and 2 new (one app, one test).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-time audio safety | ✅ N/A | UI-only. |
| II. Boundary discipline | ✅ | No C ABI change. The plug-in category string already crosses today, in the plug-in description. No JUCE outside `pluginhost/`. |
| III. Licensing hygiene | ✅ | The two new files carry the SPDX header. No dependency. |
| IV. Verification gate | ✅ | The smoke test gains `RunBrowser` and behavioural browser checks. `scripts/build-linux.sh` must pass. |
| V. One logical change per PR | ✅ | One feature: browser categories. |
| VI. Docs in sync | ✅ | CHANGELOG `[Unreleased]`; FEATURES.md (nota-features); DESIGN.md browser bullet; nota-instrument skill checklist. nota-docs (sibling repo) is noted as a follow-up because it isn't available here. |
| VII. Spec-first | ✅ | Spec plus 5 clarifications; plan now, tasks next. |
| VIII. Safety rules not weakened | ✅ | Checks only get stricter (a new guard). |

**Post-design re-check**: unchanged. The design adds no state beyond two settings keys
and keeps the flat path byte-for-byte.

## Project Structure

### Documentation (this feature)

```text
specs/009-browser-categories/
├── plan.md              # this file
├── research.md          # R1–R8
├── data-model.md        # DeviceCategory, BrowserItem additions, view-model state, settings
├── quickstart.md        # automated + manual verification
├── contracts/
│   └── ui-contract.md   # C1–C6 observable behaviour
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks (not created here)
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/
│   ├── Palette/DeviceCategories.cs        # NEW: category lists, For(), ForPlugin(), Order() (R1, R2)
│   └── Settings.cs                        # + BrowserCategoryView, BrowserFoldedCategories (R5)
├── Nota.Presentation/ViewModels/
│   └── BrowserViewModel.cs                # BrowserItem.DeviceCategory/IsFoldable/IsFolded;
│                                          # CategoryView, folds, category branch in RebuildVisible,
│                                          # pre-fold device counts (R3, R4, R6)
└── Nota.App/
    └── BrowserView.cs                     # GroupRow caret + click-to-fold + accessible name;
                                           # ⋮ "Group by category"; settings load/save (R4, R5)

tests/Nota.SmokeTest/
├── DesignTokenCheck.Browser.cs            # NEW: RunBrowser (R7)
├── BrowserTestSupport.cs                  # + FakePluginCatalog
└── Program.cs                             # wire RunBrowser; category behaviour checks; update BUILT-IN expectation

DESIGN.md · CHANGELOG.md · FEATURES.md · .claude/skills/nota-instrument/SKILL.md
```

**Structure Decision**: the existing three-layer split is kept.
- Pure category data and mapping go in `Nota.Application`, next to
  `BuiltinDeviceCatalog`, so both the view model and the smoke test can reach them.
- List projection goes in the `Nota.Presentation` view model, which already owns
  grouping and search.
- Header drawing and settings I/O go in `Nota.App/BrowserView.cs`, which already owns
  the ⋮ menu and the group row.

## Complexity Tracking

No constitution violations, so nothing to justify.
