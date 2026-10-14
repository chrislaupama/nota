# Feature Specification: UI Polish Pass

**Feature Branch**: `008-ui-polish`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "UI polish pass after the Studio restyle and colour themes, driven by a screenshot critique of 11 screens: (1) fix real layout defects — a newly added track overlapping the return tracks in the arrangement (to confirm), the Session toolbar overflowing and the inspector clipping track column headers, the Modular view showing an empty Tracks list while the project has tracks; (2) one treatment for in-window overlays (mixer, export dialog) with a header, edge and popover shadow, modelled on the command palette; (3) finish the sentence-case label rule — remove leftover all-caps and Title Case and extend the automated check; (4) one device-card header anatomy across all cards; (5) one icon set — inline icon paths moved into the shared Glyph set; (6) lighter arrangement chrome — fewer header rows above tracks, quieter browser rows, slimmer add-device target, en dash for ranges. Features and behaviour unchanged."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [ ] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Studio)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Depends on**: [`007-colour-themes`](../007-colour-themes/spec.md), which in turn builds
on [`006-saine-visual-style`](../006-saine-visual-style/spec.md). This branch builds on
both.

**Input evidence**: [critique.md](critique.md), findings F1–F12 from a screenshot tour of
11 screens. Every requirement below traces to one or more findings.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Nothing overlaps, overflows or goes missing (Priority: P1)

A producer adds tracks, works in Session view on a laptop-sized window, and opens Modular.
Every track header, toolbar control and track list is fully visible and in the right
place. Nothing is drawn on top of anything else, cut off at the edge, or shown empty when
there is content.

**Why this priority**: These are defects, not taste. A track drawn over a return track or
a cut-off toolbar makes the app look broken (critique F1–F3).

**Independent Test**: At a 1200 × 800 window, add tracks of each type in Arrangement and
check the order and spacing of headers and lanes. Open Session and check that every
toolbar control and column header is fully visible. Open Modular with a project that has
tracks and check the track list.

**Acceptance Scenarios**:

1. **Given** a project with returns, **When** the user adds an instrument, audio or MIDI
   track, **Then** the new track appears above the returns with its full header height,
   and no header or lane overlaps another, both immediately and after the window
   repaints.
2. **Given** Session view at a 1200 × 800 window, **When** it shows, **Then** every
   toolbar control is either fully visible or reachable from an overflow control, and no
   track column header is covered by the inspector.
3. **Given** a project with tracks, **When** the user opens Modular, **Then** the track
   list shows those tracks. If the list is legitimately empty, it shows an empty state
   saying what to do.

---

### User Story 2 - Overlays read as overlays (Priority: P1)

When the mixer or the export dialog opens over the arrangement, it is clearly a separate
layer: it has a header naming it, a defined edge and a shadow, and it closes the same way
every time. The command palette already works like this.

**Why this priority**: The mixer and export are used constantly. Today they blend into
the page and look unfinished (F4).

**Independent Test**: Open the mixer, export and command palette in each built-in theme,
and compare their headers, edges, shadows and close behaviour.

**Acceptance Scenarios**:

1. **Given** any built-in theme, **When** the mixer, export dialog or command palette
   opens, **Then** it shows a header with its name ("Mixer", "Export audio"; the palette
   keeps its search field as its header), a 1 px edge and the floating-layer shadow.
2. **Given** an open overlay, **When** the user presses Escape or clicks its close
   control, **Then** it closes. This works the same in all three.
3. **Given** the mixer overlay, **When** it opens, **Then** its width fits its strips,
   with no dead area inside its edge.

---

### User Story 3 - One labelling voice (Priority: P2)

Every label in the app is in sentence case, with acronyms and units kept as written (MIDI,
LFO, dB). No label shouts in capitals or uses Title Case.

**Why this priority**: The Studio rule is mostly in place already, and the leftovers
stand out precisely because everything else is consistent (F5, F6).

**Independent Test**: Visit the screens in the critique and confirm no label is in all
caps or Title Case. The automated check fails if one is reintroduced.

**Acceptance Scenarios**:

1. **Given** the palette, Drum Rack, mixer, session toolbar, browser menu and freeze
   menu, **When** they show, **Then** the labels read "Suggested · Arrangement",
   "0 pads · 0 choke groups", "Choke", "Pan", "Add to favorites", "Stop all clips",
   "Capture scene", "Session rec" and "Live freeze".
2. **Given** a new label in all caps or Title Case in Nota's own UI, **When** the
   automated check runs, **Then** it fails and names the file and line.

---

### User Story 4 - Every device card has the same header (Priority: P2)

Whatever device a producer adds, its card header has the same parts in the same places:
power, name, preset stepper, and any badges. Their eye and hand learn one layout.

**Why this priority**: Cards are where producers spend most of their time, and three
different header layouts in one chain look accidental (F7).

**Independent Test**: Add one of every built-in device and compare the headers side by
side.

**Acceptance Scenarios**:

1. **Given** every built-in instrument, audio effect and MIDI effect, **When** its card
   shows, **Then** its header has, from left to right: the power key, the name, the
   preset stepper, then device-specific badges. Each part is in the same position and
   size on every card.
2. **Given** a device that has no power state or no presets, **When** its card shows,
   **Then** the missing part's slot is empty, and the other parts do not shift.

---

### User Story 5 - One icon set (Priority: P3)

Every icon in the app comes from one set, drawn on one grid with one stroke weight, so
icons in the rail, browser, Settings, palette and cards look like siblings.

**Why this priority**: The difference is subtle on its own but adds to the feeling of
polish. One set also makes swapping or extending icons later easy (F8).

**Independent Test**: Visit the left rail, browser tabs, Settings sidebar, command
palette, history view and preview player and compare icon weight and size. The automated
check fails if an icon is drawn outside the shared set.

**Acceptance Scenarios**:

1. **Given** any screen, **When** icons show, **Then** they share one stroke weight and
   one grid, and each looks the same as before in shape and meaning.
2. **Given** a view that draws an icon outside the shared set, **When** the automated
   check runs, **Then** it fails and names the file.

---

### User Story 6 - Lighter chrome around the work (Priority: P3)

The arrangement shows more tracks and less furniture. The browser list is quieter. An
empty device chain doesn't fill the dock with a dashed box. Ranges read as ranges.

**Why this priority**: Each item is small, and together they give back space and calm to
the main screen (F9–F12).

**Independent Test**: Compare the arrangement, browser and empty dock against the
critique screenshots. Count the header rows above the first track and the visible track
rows at a 1200 × 800 window.

**Acceptance Scenarios**:

1. **Given** the arrangement at a 1200 × 800 window, **When** it shows, **Then** at most
   two header rows sit above the first track, the zoom controls live with the timeline
   rather than the track-name column, and the space saved goes to tracks.
2. **Given** the browser's built-in list, **When** it shows, **Then** the name leads each
   row. Product branding does not repeat on every row, and the type tag is visually
   quieter than the name.
3. **Given** an empty device chain, **When** the dock shows, **Then** the add-device
   target is a slim element at the end of the chain. A dashed outline appears only while
   something is being dragged.
4. **Given** the loop range or any other range, **When** it shows, **Then** it is set with
   an en dash ("1.1–5.1"), and negative numbers use the true minus sign everywhere,
   including the mixer scale.

---

### Edge Cases

- **Small windows**: at the minimum window size, the session toolbar overflow and the
  arrangement header changes still leave every control reachable.
- **Every theme**: overlays, headers and icons look right in all four built-in themes, and
  every theme still passes the readability bar.
- **Third-party plugin cards** (VST3 / AU) use the same header anatomy for the parts Nota
  draws: power, name and preset where available.
- **Devices with unusual widths** (the documented card-width exceptions) keep the same
  header order. The parts do not stretch or reflow.
- **Labels from data** (user track names, preset names, plugin names, tags) are shown as
  written and are never re-cased.
- **Localised or long labels** must not truncate where they fitted before.

## Requirements *(mandatory)*

### Functional Requirements

**Defects (US1)**

- **FR-001**: Adding a track MUST place its header and lane above the returns at full
  height with no overlap, both immediately and after any repaint (F1). If the overlap
  reproduces on the earlier releases too, the fix still belongs here.
- **FR-002**: The session toolbar MUST keep every control fully visible or reachable
  through an overflow control at a 1200 × 800 window. The inspector MUST NOT cover track
  column headers (F2).
- **FR-003**: Modular MUST list the project's tracks. When the list is legitimately
  empty, it MUST show an empty state naming the next action (F3).

**Overlays (US2)**

- **FR-004**: The mixer, the export dialog and the command palette MUST share one overlay
  treatment: a header with the overlay's name (the palette keeps its search field), a
  1 px edge, the floating-layer shadow, and closing with Escape or a close control (F4).
- **FR-005**: The mixer overlay MUST size to its content, with no dead area inside its
  edge (F4).

**Labels (US3)**

- **FR-006**: Every label in Nota's own UI MUST be in sentence case, with acronyms and
  units kept as written. The labels listed in US3/AC1 MUST change accordingly (F5, F6).
- **FR-007**: The automated design check MUST fail on an all-caps or Title Case label
  literal in Nota's own UI and name the file and line. It MUST allow acronyms, units,
  proper names and labels that come from user data.

**Device cards (US4)**

- **FR-008**: Every built-in device card's header MUST follow one anatomy: power key,
  name, preset stepper, then device-specific badges, at fixed positions and sizes. A part
  a device lacks leaves its slot empty (F7).
- **FR-009**: Plugin device cards MUST use the same anatomy for the parts Nota draws.

**Icons (US5)**

- **FR-010**: Every icon MUST come from the shared icon set, drawn on one grid at one
  stroke weight, with no change in shape or meaning (F8).
- **FR-011**: The automated design check MUST fail when a view draws an icon outside the
  shared set.

**Chrome (US6)**

- **FR-012**: The arrangement MUST show at most two header rows above the first track,
  with the zoom controls in the timeline area (F9).
- **FR-013**: Built-in browser rows MUST lead with the device name, without a repeated
  brand prefix. The type tag MUST be visually quieter than the name (F10).
- **FR-014**: An empty device chain MUST show a slim add-device target at the end of the
  chain. A dashed outline appears only during a drag (F11).
- **FR-015**: Ranges MUST be set with an en dash and negative numbers with the true minus
  sign, everywhere numbers are shown (F12).

**General**

- **FR-016**: No feature, shortcut, menu item, gesture or saved format may change. Device
  card frames and arrangement row heights stay as they are, except where FR-012 removes a
  header row.
- **FR-017**: All changes MUST look right in all four built-in themes and keep every
  theme passing the readability bar.
- **FR-018**: DESIGN.md MUST describe the overlay treatment, the device-card header
  anatomy and the icon-set rule. The changelog MUST record the visible changes.

### Key Entities

- **Overlay**: an in-window floating layer (mixer, export, command palette). It has a
  name, a header, an edge, a shadow and a close behaviour.
- **Device-card header**: the strip at the top of a card, with fixed slots for power,
  name, preset stepper and badges.
- **Icon**: a named shape in the shared set. It has one grid, one stroke weight, and a
  filled or stroked style.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A re-run of the critique's screenshot tour shows 0 of the 12 findings left.
  F1 and F3 count as closed if they are fixed or shown not to reproduce.
- **SC-002**: At a 1200 × 800 window, the arrangement shows at least one more track row
  than before, and no control in any main view is cut off.
- **SC-003**: Every built-in device card has its power, name and preset parts
  at the same positions, within 1 px.
- **SC-004**: The automated check finds 0 all-caps or Title Case label literals and 0
  icons drawn outside the shared set. The existing design, theme and appearance checks
  still pass.
- **SC-005**: In a side-by-side review, the mixer, export and palette overlays are judged
  "the same family" on header, edge, shadow and close.
- **SC-006**: Every feature in FEATURES.md is still reachable by the same path and
  shortcut.

## Assumptions

- **Scope is the critique.** New features, new device layouts and engine work are out of
  scope. The two open decisions from 006 are revisited only if the clarify step pulls
  them in: device sections divided by rules instead of framed boxes (T066), and soft
  shadows on menus, which would need menus drawn inside the window (T067).
- **Sentence case** follows the existing Studio rule and acronym list. Proper nouns and
  product names stay capitalised ("Nota Auto Filter" keeps its capitals as a name).
- **The browser's "Nota" prefix** is branding, not data. Removing it from each row is
  allowed; the product name can appear once, as the section heading.
- **Overlay = in-window panel.** OS windows (Preferences, plugin windows) keep their own
  title bars and are out of scope.
- **Arrangement header rows**: "Overview" and "Sections" may merge or become
  collapsible. Which one is a design decision for the plan, within FR-012.
- **Screenshots** come from the same Xvfb harness as 006 and 007, at 1200 × 800, in every
  theme.
- **Depends on 007**, which in turn depends on 006, landing first or this branch carrying
  both.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: UI-only; no engine or audio-thread involvement
- [x] Managed/native crossings unchanged; no JUCE outside `pluginhost/`
- [x] Smoke-test impact assessed: design checks extended (FR-007, FR-011); no engine behaviour change
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned (FR-018); FEATURES.md only if a described behaviour changes
- [x] UI change → follows `DESIGN.md` (Studio design language)
