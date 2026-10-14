# Quickstart: validating device categories

**Feature**: [spec.md](spec.md) · **Contract**: [contracts/ui-contract.md](contracts/ui-contract.md)

## Prerequisites

- A Linux build environment as for 008: .NET 10 SDK, CMake toolchain and Xvfb for
  screenshots.
- `scripts/build-linux.sh` (or `scripts/build.sh` on macOS) builds native and managed
  code and runs the smoke test.

## 1. Automated checks

```bash
scripts/build-linux.sh            # full gate: native + managed + smoke test
```

Expect these to pass:

- **`DesignTokenCheck.RunBrowser`**:
  - every built-in is categorised;
  - no stale names;
  - 18 / 26 / 6;
  - sentence-case names.
- **The browser block in `Program.cs`**:
  - the category order per tab;
  - folding hides rows but not counts;
  - a search inside a folded category shows the hit, and clearing it restores the fold;
  - favourites float within a category;
  - with a fake plug-in catalogue: a VST3 `Fx/Reverb` lands in Reverb & delay after the
    built-ins (Group by source on) or interleaved by short name (off), and an `Effect`
    (AU) or empty category lands in Other, last;
  - `CategoryView = false` gives today's exact sequence (Built-in header first).
- **The existing browser search assertion**, updated so its first header is `Synths`
  instead of `BUILT-IN`.

## 2. Manual / screenshot checks (Xvfb, all four themes)

1. Start with a fresh `HOME` and open the Audio effects tab. Expect the headers EQ &
   filter … Racks, each with a count and an open caret, and every device under exactly
   one of them.
2. Fold Dynamics and Modulation. Their rows disappear, and the headers keep their counts.
   Switch to Instruments and back: they're still folded. Quit and relaunch: still folded.
3. Type "comp" in the search. Compressor shows under an open Dynamics header with no caret.
   Clear the search: Dynamics is folded again.
4. Open ⋮ → untick "Group by category". The flat list shows the Built-in header and the
   alphabetical devices, exactly as before 009. Relaunch: it's still off. Tick it again.
5. Instruments tab: Synths, Keys & physical, Drums, Samplers, Racks. MIDI effects tab:
   Generate, Shape.
6. Double-click, drag onto a track, expand presets, favourite and tag a device inside a
   category. Each behaves as in the flat view.
7. Narrow the browser to its minimum width. The header captions truncate without
   overlapping.
8. Capture the device tabs in Studio Dark, Studio Light and the two other themes. The
   headers match the Built-in header style.

## 3. Plug-ins (macOS, optional)

With at least one VST3 effect installed, scan the plug-ins (Settings → Plug-ins). The
effect appears in its category with its `VST3 · Vendor` tag. An AU-only effect appears
under Other, last (a known limitation, research R2).
