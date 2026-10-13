# Feature Specification: Selectable Colour Themes

**Feature Branch**: `007-colour-themes`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Selectable colour themes: turn Nota's two fixed palette variants (the dark/light token pairs) into a set of named colour themes the user picks in Preferences → Appearance — e.g. Studio Dark, Studio Light, and a warm 'Ember' colour set — keeping the Studio layout, type and behaviour unchanged; designed so user-editable/importable themes (like Saine's theme editor) can follow later."

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

**Depends on**: [`006-saine-visual-style`](../006-saine-visual-style/spec.md) (the Studio
restyle, PR #1). This feature builds on Studio's token set and branches from it; it lands
after 006 does.

**Current state (for reference)**:

- Every colour the app paints is a named token (surfaces, lines, text, accent,
  semantics, graph chromas, the nine track hues, shadows). Each token has exactly **two
  values**: one for the dark appearance and one for the light.
- Preferences → Appearance offers **Dark / Light / System**. The choice applies live and
  is remembered between launches. System follows the operating system and flips with it.
- Those two value sets are the **Studio** colours (after Saine). The earlier **Ember**
  colours (warm brown neutrals, brass accent; "Ember Graphite" dark, "Ember Paper"
  light) were replaced by 006 and survive only in project history.
- Layout, geometry, type, the "live or chosen" accent rule and the state treatments are
  part of Studio's structure, not its colours. This feature does not touch them.

**Shape of the feature (assumed; see Assumptions)**: a **colour theme** is a named family
that gives every colour token a dark value *and* a light value — exactly the shape the
current two variants already have. The existing Dark / Light / System appearance switch
stays and picks the side of the family; the new choice picks the family.
So "Studio Dark" means *theme Studio, appearance Dark*.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pick a colour theme (Priority: P1)

A producer opens Preferences → Appearance and sees a list of colour themes, each with a
small preview of its colours. They pick "Ember". Every open window recolours at once —
arrangement, mixer, device cards, browser, menus, dialogs — without a restart and without
losing their place. Dark / Light / System still works as before inside the chosen theme.
They quit, relaunch, and Nota opens in Ember.

**Why this priority**: This is the feature. Without it there are no themes, only the two
fixed variants.

**Independent Test**: With a project open, switch theme in Preferences and check that
every surface recolours live. Toggle Dark / Light / System within each theme. Relaunch and
confirm the choice is kept.

**Acceptance Scenarios**:

1. **Given** Nota on theme Studio, appearance Dark, **When** the user picks Ember in
   Preferences → Appearance, **Then** every open window repaints in Ember's dark colours
   within a moment, and nothing else changes: playback, selection, scroll, open panels
   and window layout are the same.
2. **Given** theme Ember, **When** the user switches appearance to Light, **Then** the app
   shows Ember's light colours.
3. **Given** theme Ember, appearance System, **When** the OS flips between dark and
   light, **Then** Nota follows with Ember's matching side.
4. **Given** the user chose Ember and quit, **When** Nota launches again, **Then** it opens
   in Ember with the same appearance, and no frame is first drawn in another theme.
5. **Given** a settings file from before this feature (no theme recorded), **When** Nota
   launches, **Then** it uses Studio with the appearance already stored.
6. **Given** any theme, **When** the user looks at the Preferences theme list, **Then**
   each entry shows its name and a preview of its ground, panel, text and accent for the
   appearance currently showing, and the active theme is marked as chosen.

---

### User Story 2 - Built-in themes that hold up everywhere (Priority: P1)

Whatever theme is chosen, Nota stays fully usable. Text is readable on every surface, live
state (playing, selected, armed, muted, soloed) is still obvious, meters still read
safe / warning / over, and track colours stay distinguishable from each other. No surface
falls back to another theme's colour or to a raw default.

**Why this priority**: A theme that breaks one view (unreadable menu text, an invisible
mute state, a stray Studio grey in an Ember window) is worse than no theme. This story is
the quality bar for every built-in theme.

**Independent Test**: For each built-in theme × appearance, walk the screenshot set used
for 006 (arrangement, mixer, session, device cards, browser, menus, preferences, command
palette, dialogs) and run the automated colour checks.

**Acceptance Scenarios**:

1. **Given** any built-in theme and appearance, **When** the automated check runs,
   **Then** every colour token has a value, and every text-on-surface pairing the design
   names meets its contrast minimum (≥ 4.5:1 for text, ≥ 3:1 for glyphs and state
   fills against their ground).
2. **Given** any built-in theme, **When** a track is muted, soloed or armed, **Then** the
   state is shown with that theme's mute / solo / arm colours, and the ink on them is
   readable.
3. **Given** any built-in theme, **When** the transport is stopped, nothing is selected and
   no toggle is on, **Then** no accent colour is on screen. The Studio "live or chosen"
   rule holds in every theme.
4. **Given** a project with tracks in all nine track colours, **When** the theme changes,
   **Then** each track keeps its colour *role*, with the hue that theme gives the role,
   and the project file is unchanged.

---

### User Story 3 - Ember comes back as a colour set (Priority: P2)

Someone who preferred the old warm look picks "Ember" and gets the warm brown neutrals
and the brass accent back, in both dark ("Graphite") and light ("Paper"). Studio's
structure stays: docked seam panels, sentence-case labels, neutral resting values,
orange only for live or chosen, which in Ember becomes brass only for live or chosen.

**Why this priority**: Ember is the second theme that proves the system works, and it
gives back the old palette without bringing back the old layout. It ranks below US1 and
US2 because those cover any second theme.

**Independent Test**: Pick Ember. Compare screenshots against Ember-era screenshots for
colour only (ground, panel, text, accent hues). Layout must match the Studio screenshots,
not the Ember-era ones.

**Acceptance Scenarios**:

1. **Given** theme Ember, appearance Dark, **When** the main window shows, **Then** its
   ground, panel, raised and text tones and its accent are the Ember Graphite values.
2. **Given** theme Ember, appearance Light, **Then** they are the Ember Paper values.
3. **Given** theme Ember, **When** any view is compared with Studio, **Then** sizes,
   radii, spacing, type and which elements carry the accent are identical. Only the
   colours differ.

---

### User Story 4 - Themes as data, ready for user themes later (Priority: P3)

A contributor adds a third built-in theme by writing one complete theme definition: a
name plus a dark and light value for every colour token. Nothing else in the app changes.
If they leave out a token, the automated check names the missing token before the change
can ship. This is the groundwork a later theme editor or theme import will use. That
editor is **not** part of this feature.

**Why this priority**: The user asked for themes "designed so user-editable themes can
follow". This story keeps that path open at low cost now, without building the editor.

**Independent Test**: Add a throwaway theme definition in a test build. Confirm it shows
in Preferences and applies fully. Remove one token and confirm the check fails and names
it.

**Acceptance Scenarios**:

1. **Given** a new complete theme definition, **When** the app runs, **Then** the theme
   appears in the Preferences list and applies like the built-in ones, with no other code
   or view changes.
2. **Given** a theme definition that lacks a token or has an invalid colour, **When** the
   automated check runs, **Then** it fails and names the theme and the token.

---

### Edge Cases

- **Stored theme no longer exists** (renamed or removed in a later version, or a
  hand-edited settings file): Nota falls back to Studio, keeps the stored appearance, and
  does not crash or show an error dialog.
- **Theme changed while a popup, menu, tooltip or modal is open**: the open surface
  recolours too, or closes cleanly. No half-recoloured popup stays on screen.
- **Theme changed during playback or recording**: audio is not interrupted and no
  dropout is attributable to the switch (the switch is UI-only).
- **Colours derived from tokens** (washes, selection tints, shades of track colours, ink
  computed for contrast on a fill, one-off device hues mapped onto roles): these follow
  the active theme, not Studio's values.
- **Stored per-item colours** (e.g. a custom tag or device hue saved in a project): shown
  through the same theme mapping as today, and never rewritten in the file.
- **Secondary windows** (Preferences, export, plugin windows' Nota chrome, the
  connect-phone view, the welcome screen): all follow the theme, including windows opened
  after the switch.
- **System appearance with a theme** whose dark and light sides are both defined: always
  true for built-in themes, since a theme must define both (US4).

## Requirements *(mandatory)*

### Functional Requirements

**Choosing a theme**

- **FR-001**: Preferences → Appearance MUST offer a **colour theme** choice listing every
  available theme by name, alongside the existing Dark / Light / System appearance
  control, which keeps its current behaviour.
- **FR-002**: Each entry in the theme list MUST show a small preview of that theme's
  colours (at least ground, panel, primary text and accent) for the appearance currently
  showing. The active entry MUST be visibly marked as chosen, following Studio's
  selected-option treatment.
- **FR-003**: Choosing a theme MUST recolour every open window and every colour drawn by
  the app (controls, custom-drawn views, graphs, meters, waveforms, track colours,
  shadows) right away, without a restart, and without changing any other state.
- **FR-004**: The chosen theme MUST be remembered across launches, together with the
  appearance. Nota MUST start in the stored theme without first showing another.
- **FR-005**: If no theme is stored, or the stored theme is unknown, Nota MUST use
  **Studio** and keep the stored appearance.

**What a theme is**

- **FR-006**: A colour theme MUST consist of a unique identifier, a display name, and a
  value for **every** colour token for **both** the dark and the light appearance. The
  colour tokens are the full set the design language defines, including track hues,
  semantic colours (mute, solo, arm/record, warning, success) and shadow colours.
- **FR-007**: A theme MUST NOT change anything except colour: geometry, spacing, radii,
  type, icons, layout, which elements carry the accent, and all behaviour stay those of
  the Studio design language.
- **FR-008**: Colours that the app derives from tokens (tints, shades, contrast-picked
  ink, mappings of stored one-off hues onto roles) MUST be derived from the **active**
  theme's values.
- **FR-009**: Built-in themes MUST be defined in one place per theme, as data in the same
  complete form a future user-supplied theme would take. Adding a built-in theme MUST
  require no change outside its own definition and the list of built-ins.

**Built-in themes**

- **FR-010**: Nota MUST ship at least two built-in themes:
  - **Studio**: today's colours, dark and light, unchanged. This is the default.
  - **Ember**: the pre-Studio Ember Graphite (dark) and Ember Paper (light) colours,
    mapped onto today's token set. For tokens that Studio added and Ember never had
    (seam, rule, faint text, mute, solo, viz ink, viz grid, the inset surface), the
    values are derived in Ember's palette by the same roles Studio uses.
- **FR-011**: Every built-in theme, in both appearances, MUST meet the contrast minimums
  the design language sets (≥ 4.5:1 for text on its surface, ≥ 3:1 for glyphs and state
  fills), and MUST keep the nine track colours and the mute / solo / arm colours
  distinguishable from each other and from the accent.

**Guarding quality**

- **FR-012**: The automated design check MUST verify, for every built-in theme and both
  appearances, that every token has a valid value, that the contrast minimums of FR-011
  hold, and that a theme's preview colours come from that theme. On failure it MUST name
  the theme and the token.
- **FR-013**: The existing Studio checks (no Ember value in Studio's colours, neutral
  resting values, and the others) MUST keep applying to the Studio theme. Checks about
  structure (radii, type, shadows only on overlays, no tracked caps) MUST apply
  regardless of theme. Ember's colour values are exempt from the "no Ember value" rule
  and from nothing else.

**Docs**

- **FR-014**: The design documentation MUST describe colour themes: what a theme
  contains, the built-in themes with their token values, and how to add one. The
  changelog and feature list MUST record the new Preferences option.

### Key Entities

- **Colour theme**: an identifier (stable, used in settings), a display name, and a
  complete set of colour values, one dark and one light per colour token. Built-in themes
  are Studio and Ember. Later, user themes take the same shape.
- **Colour token**: a named colour role in the design language (e.g. ground, panel, seam,
  primary text, accent, mute, track hue 3, popover shadow colour). The set of tokens is
  fixed by the design language; themes only supply values.
- **Appearance**: Dark / Light / System, unchanged. It picks which side of the active
  theme is shown.
- **Appearance settings**: the stored theme identifier plus the stored appearance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can change the colour theme from Preferences in 2 interactions
  (open Appearance, pick a theme), and every open window shows the new colours within
  0.5 s of the click.
- **SC-002**: For 100% of built-in theme × appearance combinations (at least 4), the
  automated check passes with no missing token, invalid value or contrast failure.
- **SC-003**: In a side-by-side screenshot review of the 006 screen set, Studio-theme
  screenshots after this feature are indistinguishable from those before it.
- **SC-004**: In the same review under Ember, zero elements differ from their Studio
  counterpart in size, position, radius or type. Only colour differs.
- **SC-005**: Switching theme during playback causes no audible dropout across 20
  switches.
- **SC-006**: Adding a further built-in theme in a test build touches only that theme's
  definition and the built-in list. No view or control code changes.
- **SC-007**: After relaunch, the stored theme and appearance are in effect on the first
  frame in 100% of launches.

## Assumptions

- **Theme = family with both sides.** A theme supplies dark *and* light values, and the
  existing Dark / Light / System control picks the side. This keeps System working for
  every theme and matches the current two-value tokens. The alternative, single-sided
  themes such as "Studio Dark" and "Studio Light" as separate list entries with
  System pairing two of them, adds a pairing setting for little gain. In the UI the
  combination still reads naturally ("Ember · Dark").
- **User-editable and importable themes are out of scope.** That includes a theme editor,
  import/export of theme files, and per-project themes. FR-009 and US4 keep the data
  shape ready for them.
- **Two built-in themes** (Studio, Ember) are enough to ship. More, e.g. a high-contrast
  theme, can be added later per FR-009 without a new spec.
- **Track colours are themed.** Projects store track colours as palette roles, not raw
  colours, so a theme can give each role its own hue without touching project files.
- **Ember values** come from the last pre-Studio release (0.52.1) palette. Studio-only
  tokens get Ember-palette values by Studio's derivation rules (e.g. seam = Ember's
  gutter tone, rule = Ember's strong border).
- **Settings are per machine**, like the existing appearance setting. They are not stored
  in projects.
- **The phone remote's web page** keeps its own look. Only the in-app connect-phone view
  is themed.
- **Depends on 006** being merged first, or this branch carrying it.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: UI-only change; no engine or audio-thread involvement
- [x] Managed/native crossings unchanged; no JUCE outside `pluginhost/`
- [x] Smoke-test impact assessed: the design-token check is extended per FR-012/FR-013; no engine behaviour change
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry and FEATURES.md planned (FR-014)
- [x] UI change → follows `DESIGN.md` (Studio design language); themes vary colour only (FR-007)
