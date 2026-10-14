# Feature Specification: Device categories in the browser

**Feature Branch**: `009-browser-categories`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Better categorise the built-in devices in the browser. Today each browser tab (Instruments, Audio effects, MIDI effects) shows the built-in devices as one long flat list. Give every device a category (for example synths, drums, samplers, keys for instruments; filters, dynamics, reverb/delay, modulation, distortion, EQ, utility for effects) and let the list be grouped/filtered by category, with that category view enabled by default."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Studio)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Builds on**: `008-ui-polish` (browser rows without the "Nota" prefix, quieter type tags,
the shared icon set). Stacked on that branch.

### Today

- The Instruments tab lists 18 built-in instruments, the Audio effects tab 26 built-in
  effects and the MIDI effects tab 6 MIDI effects. Each tab is one alphabetical list under a
  single "Built-in" header, followed by a "Plug-ins" header for installed plug-ins.
- Each row already carries a short type tag on the right ("wavetable synth", "drum
  machine", "envelope / LFO"…), but the tag is not something the list can be grouped or
  narrowed by.
- The view options (⋮) offer "Group by source" (the Built-in / Plug-ins headers),
  "Favorites first" and "Show type tags". The filter chips under the search field are
  Favorites plus the user's own tags.
- Installed plug-ins report their own category (for example a VST3 "Fx|Reverb" or an Audio
  Unit type); the browser keeps it but does not use it for grouping.

## Clarifications

### Session 2026-10-14

- Q: How should the categories appear? → A: As section headers in the list (like today's
  Built-in / Plug-ins headers), each foldable. No category chips.
- Q: Should plug-ins be categorised too? → A: Yes — plug-ins join the same categories from
  the category they report; anything unrecognised goes under "Other".
- Q: How should the 18 instruments be grouped? → A: Synths (Synth, Volt, Bass, Monolith,
  Pentad, Consort, Operator, Aurora, Flux, Pendulum) · Keys & physical (Keys, Physical) ·
  Drums (Rhythm, Drum Rack) · Samplers (Sampler, Mosaic, Grain) · Racks (Instrument Rack).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Browse devices by what they do (Priority: P1)

A producer opens the Audio effects tab looking for "something for space". Instead of
scanning 26 names in alphabetical order, they see the list arranged by category — EQ &
filter, Dynamics, Reverb & delay, Modulation, Distortion & saturation, Pitch & time,
Utility & analysis, Racks — and go straight to Reverb & delay, where Reverb, Chamber, Delay
and the like sit together. The same holds on the Instruments tab (Synths, Drums, Keys,
Samplers, Racks…) and the MIDI effects tab.

**Why this priority**: it is the whole point of the request. The list grows with every
release, and a category is how musicians think about devices ("I need a compressor", not
"I need something starting with C").

**Independent Test**: open each device tab with a fresh profile; the built-in devices
appear arranged by category, each device in exactly one category, in a fixed order, with
no other change needed.

**Acceptance Scenarios**:

1. **Given** a fresh profile, **When** the Audio effects tab is opened, **Then** the
   built-in effects are arranged by category, each category's devices are alphabetical,
   and every one of the 26 built-in effects appears exactly once.
2. **Given** a fresh profile, **When** the Instruments tab or the MIDI effects tab is
   opened, **Then** its devices are arranged by category the same way.
3. **Given** the category view, **When** the user double-clicks or drags a device, expands
   it to see its presets, favourites it or tags it, **Then** everything works exactly as in
   the flat list.

---

### User Story 2 — Turn the category view off (Priority: P2)

A long-time user who knows every device by name prefers the old flat alphabetical list.
They switch the category view off in the browser's view options, and the choice is
remembered the next time Nota starts.

**Why this priority**: the category view is the new default, but nobody should lose the
list they already know.

**Independent Test**: switch the category view off, restart Nota, and the flat list is
still shown; switch it on again and the categories return.

**Acceptance Scenarios**:

1. **Given** the category view is on, **When** the user turns it off in the view options,
   **Then** the tab shows today's flat list (Built-in / Plug-ins headers per "Group by
   source").
2. **Given** the user turned the category view off, **When** Nota restarts, **Then** it is
   still off.

---

### User Story 3 — Fold the categories you don't need (Priority: P2)

A user who mostly reaches for modulation effects folds the other categories on the Audio
effects tab, so the list shows just their headers and the open Modulation category. The
fold state is remembered when they switch tabs and when Nota restarts.

**Why this priority**: headers make a long list scannable; folding makes it short. Together
they cover "browse" and "find" without a second filter control.

**Independent Test**: fold every category but one on the Audio effects tab; only that
category's devices show under the headers; switch tabs and restart; the same categories are
still folded; unfold them again.

**Acceptance Scenarios**:

1. **Given** the Audio effects tab in the category view, **When** the user folds a
   category header, **Then** its devices hide and the header stays, showing its count.
2. **Given** some categories are folded, **When** the user types in the search field,
   **Then** matching devices show even inside folded categories (the search unfolds them for
   as long as it is active), and the fold state returns when the search is cleared.
3. **Given** categories are folded, **When** the user switches tabs or restarts Nota,
   **Then** the same categories are still folded.

---

### User Story 4 — Plug-ins land in the same categories (Priority: P3)

A user with installed plug-ins sees a third-party reverb next to Nota's own reverbs, under
Reverb & delay, instead of only at the bottom under Plug-ins.

**Why this priority**: valuable for users with many plug-ins, but the built-in categories
deliver the core of the request on their own.

**Independent Test**: with a plug-in that reports a reverb category installed, it appears
under Reverb & delay; a plug-in reporting nothing recognisable appears under an "Other"
category.

**Acceptance Scenarios**:

1. **Given** an installed plug-in that reports a category Nota recognises, **When** the
   category view is on, **Then** the plug-in appears in the matching category.
2. **Given** a plug-in whose category Nota does not recognise, **When** the category view
   is on, **Then** it appears under "Other", never hidden.

### Edge Cases

- **Search with the category view on**: matching devices stay under their category
  headers; a category with no matches is not shown; the header count reflects the matches.
- **Empty category** (for example no MIDI effect in a category, or no plug-ins
  installed): the category is not shown at all — no empty headers.
- **Favorites first**: favourites float to the top within their category, not out of it.
- **Group by source together with categories**: built-ins and plug-ins must not be
  scattered — see FR-005.
- **A device that fits two categories** (Auto Filter: filter or modulation? Strata: pitch
  or texture?): it is listed in exactly one, its primary job. No duplicates.
- **Rack devices** (Instrument Rack, Drum Rack, Audio Effect Rack) have a clear place.
- **A new built-in device added in a later release without a category**: it is still
  listed, under "Other", and the design check fails until it gets one.
- **Folded categories**: the fold state survives switching tabs and restarting; a search
  shows matches inside folded categories without changing the saved fold state.
- **A plug-in reporting several categories** (for example VST3 "Fx|Delay|Reverb"): it is
  listed once, under the first category Nota recognises.
- **Narrow browser** (folded to its rail or very narrow): category headers truncate like
  other headers; nothing overlaps.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every built-in instrument, audio effect and MIDI effect MUST belong to
  exactly one category. The categories and their order MUST be fixed per tab (proposed in
  Assumptions), named in sentence case.
- **FR-002**: The device tabs MUST present the list as category section headers, in the
  style of today's Built-in / Plug-ins headers (name and device count), each foldable by
  clicking it. Fold state MUST persist per tab across tab switches and restarts. There is no
  separate category chip row.
- **FR-003**: The category view MUST be on by default for a new profile and for existing
  users, and MUST be switchable off in the browser's view options; the choice MUST persist
  across restarts.
- **FR-004**: Within a category, devices MUST be alphabetical, with "Favorites first" still
  floating favourites to the top of their category.
- **FR-005**: With the category view on, installed plug-ins MUST join the same categories,
  mapped from the category they report; a plug-in whose category is missing or not
  recognised MUST appear under "Other" (always last), never hidden. Inside a category,
  built-in devices come before plug-ins when "Group by source" is on (no separate
  Built-in / Plug-ins headers in the category view); with it off, built-ins and plug-ins
  are mixed alphabetically. Each plug-in row keeps its format/vendor tag, so the source
  stays visible.
- **FR-006**: Search MUST keep working on the full device name and type, and MUST respect
  the category view (matches stay under their headers, empty categories hide).
- **FR-007**: Opening, dragging, previewing, expanding presets, favouriting and tagging a
  device MUST work identically in the category view.
- **FR-008**: Category headers (and any category chips) MUST follow `DESIGN.md`: sentence
  case, the existing group-header style with a count, no new colours or icon styles.
- **FR-009**: The design checks MUST fail if a built-in device has no category, so a device
  added later cannot silently land under "Other".
- **FR-010**: No device, preset, shortcut or saved project changes. Tags and favourites
  are unaffected. The Files, Presets, Projects, Map and History tabs are unchanged.

### Key Entities

- **Device category**: a named group on one tab ("Reverb & delay"), with a fixed position
  in that tab's order. A built-in device has exactly one. Plug-ins may map to one from the
  category they report.
- **Category view setting**: on/off, per user, persisted, default on.
- **Fold state**: per tab, the set of folded categories, per user, persisted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % of built-in devices (18 instruments, 26 audio effects, 6 MIDI effects)
  appear under a category in the category view, each exactly once.
- **SC-002**: Finding a given kind of device (for example "a delay") takes one glance at a
  category header instead of scanning the whole list: the target device is within the
  first screen of its category in a 1200 × 800 window.
- **SC-003**: Turning the category view off restores today's flat list exactly (same
  order, same headers), and the setting survives a restart in 3 of 3 tries.
- **SC-004**: Every existing browser action (open, drag, preview, expand presets,
  favourite, tag, search) works in the category view with no regressions in the smoke
  tests.
- **SC-005**: The category view reads correctly in all four built-in themes and passes the
  design checks.

## Assumptions

- **Proposed categories** (to be confirmed in clarify/plan; names sentence case):
  - *Instruments* (confirmed): Synths (Synth, Volt, Bass, Monolith, Pentad, Consort,
    Operator, Aurora, Flux, Pendulum), Keys & physical (Keys, Physical), Drums (Rhythm, Drum
    Rack), Samplers (Sampler, Mosaic, Grain), Racks (Instrument Rack) — in that order.
  - *Audio effects*: EQ & filter (EQ-3, EQ-8, Dynamic EQ-8, Auto Filter), Dynamics
    (Compressor, Ceiling, Level, Auto Gain), Reverb & delay (Reverb, Chamber, Delay, Orbit),
    Modulation (Chorus, Flanger, Phaser, Auto Pan, Shutter), Distortion & saturation (Valve,
    Vintage, Forge, Crush, Prism), Pitch & time (Auto Shift, Beat Repeat, Strata), Utility &
    analysis (Utility, Lens), Racks (Audio Effect Rack).
  - *MIDI effects*: Generate (Arp, Chord, Random), Shape (Scale, Length, Velocity).
  - Exact membership is checked against each device's sound and documentation during
    planning.
- The existing type tag on each row stays; the category is a separate, coarser grouping.
- "Other" exists only for plug-ins (or a future uncategorised device) and is always last.
- Plug-in categories are mapped from the VST3 sub-categories and Audio Unit types the
  plug-in catalogue already stores; the exact mapping table is a planning detail.
- The category view setting is a per-user preference stored with the other browser view
  options; no project-format change.
- Users of other languages see the categories in English, like the rest of the app today.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: none (browser UI and catalogue data only)
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — no new crossing
- [ ] Smoke-test impact assessed: design check for uncategorised built-ins (FR-009); browser grouping covered by the existing browser tests
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned, FEATURES.md (browser section)
- [ ] UI change → follows `DESIGN.md` (Studio design language)
