# UI contract: Device categories in the browser

**Feature**: [../spec.md](../spec.md) · **Data model**: [../data-model.md](../data-model.md)

This is what a user (and the smoke test) can observe on the Instruments, Audio effects and
MIDI effects tabs. The Files, Presets, Projects, Map and History tabs are out of scope
(FR-010).

## C1: Category view on (the default)

Row sequence on a device tab, with no search:

```text
[▾] <Category A>  ─────────────  <n>
    <device>                    <type tag>
    <device>  (▸ presets when expanded, as today)
[▸] <Category B>  ─────────────  <n>        ← folded: no device rows follow
[▾] Other         ─────────────  <n>        ← only if a plug-in maps nowhere; always last
```

- **Category order**: categories appear in the tab's fixed order (data-model). A category
  with no visible device is not shown.
- **Count**: `<n>` is the number of matching devices in the category, folded or not.
- **Order inside a category**:
  - built-ins before plug-ins when "Group by source" is on;
  - favourites first within that run when "Favorites first" is on;
  - otherwise by short name (case-insensitive).
- **No source headers**: there are no Built-in / Plug-ins headers in this view. A
  plug-in row keeps its `VST3 · Vendor` tag.
- **Device rows**: they are the same objects as in the flat view. Double-click, drag,
  preview, preset expansion, favourite and tag behave identically (FR-007).

## C2: Category header row

- **Look**: `GroupLabel` caption (sentence case, through `NotaText.Label`) · hairline
  `Brush.Rule` · mono count `Brush.TextDisabled` · 22 px high. That is today's group row,
  plus a leading 12 px caret (`Glyph` CaretDown when open, CaretRight when folded,
  `Brush.TextTertiary`). It adds no new colour, radius or icon style (FR-008).
- **Click**: a click anywhere on the row toggles the fold, and the hand cursor shows it's
  clickable. The row never becomes the selection.
- **While searching**: no caret is drawn and clicks do nothing.
- **Accessible name**: `"<Category>, <n> devices, open"` or `"…, folded"`.
- **Narrow browser**: the caption truncates; the count and caret stay visible.

## C3: Search (FR-006)

- **Matching**: the rules are as today (name or type-tag substring; preset hits open their
  folders).
- **Grouping**: matches stay under their category headers. Categories without matches
  are hidden, and the count shows the matches.
- **Folds**: saved folds are ignored while a query is active, and every shown category is
  open. Clearing the query restores the saved fold state exactly (US3/AC2).

## C4: View options (⋮)

The order is:

1. Show type tags
2. **Group by category** (new; checked by default)
3. Group by source
4. Favorites first

The Files tab's sample options and "Edit tags…" follow, as today.

- **Turning it off**: today's flat list returns, row for row (`Built-in` / `Plug-ins`
  headers per "Group by source"). That's SC-003.
- **Persistence**: the toggle is saved to `settings.json` (`BrowserCategoryView`), and
  folds are saved as they change (`BrowserFoldedCategories`). Both survive restart.

## C5: Status line

`"<n> audio effects · <b> built-in · <p> plug-ins"` counts the matching devices,
including the ones inside folded categories (R6).

## C6: Design check (FR-009)

`DesignTokenCheck.RunBrowser` fails when:

- a built-in device has no category;
- a category lists a name that isn't in the catalogue;
- a category has no built-ins;
- the per-tab totals stop being 18 / 26 / 6.
