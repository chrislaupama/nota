# Data model: Device categories in the browser

**Feature**: [spec.md](spec.md) · **Research**: [research.md](research.md)

These are in-memory and settings-level entities only. Nothing changes in the project
format, the C ABI or the engine.

## DeviceCategory (static, `Nota.Application/Palette/DeviceCategories.cs`)

| Field | Type | Rule |
|---|---|---|
| `Name` | string | Sentence case, unique within its tab. `NotaText.Label(Name) == Name`. |
| `Type` | `BuiltinDeviceType` | The tab it belongs to: Instrument, AudioEffect or MidiEffect. |
| `Position` | int | Its order on the tab. `Other` is always last (`int.MaxValue`). |
| `Builtins` | string[] | The catalogue names (`"Nota Reverb"`) of its built-in members. Must not be empty. |

The fixed contents come from the spec's Assumptions, in this order:

- **Instrument**: Synths · Keys & physical · Drums · Samplers · Racks
- **AudioEffect**: EQ & filter · Dynamics · Reverb & delay · Modulation · Distortion ·
  Pitch & time · Utility · Racks
- **MidiEffect**: Generate · Shape

`Other` is not a declared category. It is the fallback name a plug-in (or an unmapped
built-in) gets, and it sorts last.

**Invariants** (checked by `DesignTokenCheck.RunBrowser`, FR-009 / SC-001):

- Every `BuiltinDeviceCatalog.All` entry appears in exactly one category of its own
  `Type`.
- Every name listed in `Builtins` exists in the catalogue with that `Type` (no stale
  entries).
- Members per tab add up to 18 / 26 / 6.

**Operations**:

- `For(BuiltinDevice d) → string`: the device's category, or `Other`.
- `ForPlugin(bool instrument, string reported) → string`: maps the reported category using
  the R2 token table, or returns `Other`.
- `Order(BuiltinDeviceType t) → IReadOnlyList<string>`: the category names in tab order.
- `Position(BuiltinDeviceType t, string name) → int`: the index in `Order(t)`, or
  `int.MaxValue` for `Other` or an unknown name.

## BrowserItem (existing, `Nota.Presentation/ViewModels/BrowserViewModel.cs`)

New members:

| Member | Type | Rule |
|---|---|---|
| `DeviceCategory` | string (init) | Set on top-level device rows in `Rebuild()`. Built-ins use `DeviceCategories.For`; plug-ins use `ForPlugin(isInstrument, Category)`. Empty on presets, folders and groups. |
| `IsFoldable` | bool (init) | True only on category header rows (`Kind == Group`), and false on them while a search is active (no caret, clicks ignored). |
| `IsFolded` | bool (init) | For a category header: whether it is drawn folded. Always false while a search is active. |

How a category header row is set up:

- `Kind = Group`;
- `Name` = the category name;
- `GroupCount` = the number of matching devices in that category, folded or not;
- `Path = "cat:<tabKey>:<name>"`, where `tabKey` is `instr`, `fx` or `midi`.

The existing `Category` (the raw string the plug-in reports) is unchanged and still
feeds the mapping.

## Category view state (`BrowserViewModel`)

| Member | Type | Default | Notes |
|---|---|---|---|
| `CategoryView` | bool | `true` | The setter rebuilds the device tabs. It mirrors `Settings.BrowserCategoryView`. |
| `_folded` | `HashSet<string>` | empty | Keys are `<tabKey>:<name>`. `SetFoldedCategories(IEnumerable<string>)` loads them, and `FoldedCategories` exposes them for saving. |
| `ToggleCategoryFold(BrowserItem header)` | method | — | Does nothing unless `IsFoldable` is set and the tab's query is empty. It flips the key, rebuilds that tab and raises `CategoryFoldsChanged`. |
| `_deviceCounts[3]` | `(int builtin, int plugins)` | — | Recorded before folding is applied. `VisibleDeviceCounts` returns them (R6). |

**State transitions** (one tab):

```text
open ──click header──▶ folded ──click header──▶ open
  │                       │
  └──query typed──▶ searching (all matching categories open, caret hidden, clicks ignored)
                          │
              query cleared ──▶ back to the saved state (open/folded as before)
```

## Settings (existing, `Nota.Application/Settings.cs`)

| Property | Type | Default | Notes |
|---|---|---|---|
| `BrowserCategoryView` | bool | `true` | New. A missing key in an older `settings.json` resolves to `true` (FR-003). |
| `BrowserFoldedCategories` | `List<string>` | empty | New. Keys are `instr:Synths`, `fx:Dynamics` and so on. Unknown keys are ignored when loaded. |

There are no migrations. The existing `BrowserGroupBySource` and `BrowserFavoritesFirst`
keep their meaning: inside a category, the source run comes first, then favourites.
