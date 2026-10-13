// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// The single source of truth for palette colours used by C# custom-drawn controls
// (Render overrides can't cheaply resolve a XAML DynamicResource). Every slot here
// MIRRORS a Brush.* key in Theme/NotaTheme.axaml — keep the two in sync.
//
// Themable by construction: a slot hands out one long-lived SolidColorBrush whose
// *Color* is re-pointed when the variant changes. Call sites (and the private static
// readonly fields that alias them all over the view layer) keep the same brush object,
// so switching dark ↔ light needs no rebuild — only a repaint, which
// NotaThemeService triggers. Never wrap a slot in `new SolidColorBrush(slot.Color)`:
// that snapshots the colour and freezes the view in one theme.
//
// Three ways to get a colour:
//   • a named token   — NotaPalette.SurfaceCard        (explicit dark + light pair)
//   • a tinted wash   — NotaPalette.Wash(Accent, 0x24) (tracks its source slot)
//   • a device hue    — NotaPalette.Ink("#D06FB0")     (identity hue of one visualiser;
//                        its light counterpart is derived, or pinned in InkOverrides)

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace Nota.App;

internal enum NotaThemeVariant { Dark, Light }

internal static class NotaPalette
{
    // ---- slot machinery ---------------------------------------------------

    private sealed record Slot(Color Dark, Color Light, SolidColorBrush Brush);

    private static readonly List<Slot> Slots = new();
    private static readonly List<(SolidColorBrush Src, byte Alpha, SolidColorBrush Out)> Washes = new();
    private static readonly Dictionary<(SolidColorBrush, byte), SolidColorBrush> WashCache = new();
    private static readonly List<(SolidColorBrush[] Slots, LinearGradientBrush Out)> Gradients = new();
    private static readonly List<(Func<Color> Compute, SolidColorBrush Out)> Derivations = new();
    private static readonly Dictionary<string, SolidColorBrush> InkCache = new();

    /// <summary>The variant every slot currently holds.</summary>
    public static NotaThemeVariant Variant { get; private set; } = NotaThemeVariant.Dark;

    /// <summary>Raised after <see cref="Apply"/> has re-tinted every slot, so views can
    /// drop colour caches they derived themselves (per-index clip brushes, gradients…).</summary>
    public static event Action? Changed;

    // A named token: the dark value and its light counterpart.
    private static SolidColorBrush T(string dark, string light)
    {
        var d = Color.Parse(dark);
        var slot = new Slot(d, Color.Parse(light), new SolidColorBrush(Variant == NotaThemeVariant.Dark ? d : Color.Parse(light)));
        Slots.Add(slot);
        return slot.Brush;
    }

    /// <summary>Re-tint every slot for <paramref name="variant"/>. Brush objects are kept;
    /// only their Color changes, so every existing reference follows along.</summary>
    public static void Apply(NotaThemeVariant variant)
    {
        Variant = variant;
        bool dark = variant == NotaThemeVariant.Dark;
        foreach (var s in Slots) s.Brush.Color = dark ? s.Dark : s.Light;
        foreach (var (src, alpha, outB) in Washes)
        {
            var c = src.Color;
            outB.Color = Color.FromArgb(alpha, c.R, c.G, c.B);
        }
        foreach (var (src, g) in Gradients)
            for (int i = 0; i < src.Length; i++) g.GradientStops[i].Color = src[i].Color;
        for (int i = 0; i < TrackSlots.Length; i++) TrackColors[i] = TrackSlots[i].Color;
        for (int i = 0; i < ReturnSlots.Length; i++) ReturnColors[i] = ReturnSlots[i].Color;
        // Derivations read the slots above, so they recompute last.
        foreach (var (compute, outB) in Derivations) outB.Color = compute();
        Changed?.Invoke();
    }

    /// <summary>The colour <paramref name="slot"/> holds in <paramref name="variant"/>, whatever
    /// the current one is — for previews of a variant (the theme picker's swatches). Read it
    /// at draw time like any other colour.</summary>
    public static Color ColorIn(SolidColorBrush slot, NotaThemeVariant variant)
    {
        foreach (var s in Slots)
            if (ReferenceEquals(s.Brush, slot)) return variant == NotaThemeVariant.Dark ? s.Dark : s.Light;
        return slot.Color;
    }

    /// <summary>A translucent tint of a palette slot. Shared per (slot, alpha) and
    /// re-tinted with its source, so washes follow the theme like any other token.</summary>
    public static SolidColorBrush Wash(SolidColorBrush src, byte alpha)
    {
        if (WashCache.TryGetValue((src, alpha), out var b)) return b;
        var c = src.Color;
        b = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        WashCache[(src, alpha)] = b;
        Washes.Add((src, alpha, b));
        return b;
    }

    /// <summary>A brush whose colour is computed from other slots — a track hue mixed to a
    /// shade, an alpha-blended clip fill. Recomputed on a variant change, so the brush can be
    /// handed to a control that outlives one. Registration is permanent: cache the result per
    /// key (a palette index, say), never build one per repaint.</summary>
    public static SolidColorBrush Derived(Func<Color> compute)
    {
        var b = new SolidColorBrush(compute());
        Derivations.Add((compute, b));
        return b;
    }

    /// <summary>Ink for text or a glyph on a solid <paramref name="fill"/> — dark ink or white,
    /// whichever has the higher contrast on the fill's current colour, recomputed on a variant
    /// change. Fills are tokens that differ per variant, so the ink on them is derived, never
    /// fixed. Cached per fill.</summary>
    public static SolidColorBrush InkOn(SolidColorBrush fill)
    {
        if (InkOnCache.TryGetValue(fill, out var b)) return b;
        b = Derived(() => ReadableInkOn(fill.Color));
        InkOnCache[fill] = b;
        return b;
    }

    private static readonly Dictionary<SolidColorBrush, SolidColorBrush> InkOnCache = new();

    /// <summary>Dark ink (#17181A) or white, whichever reads better on <paramref name="fill"/>
    /// (WCAG contrast ratio). For views that compute a fill at draw time.</summary>
    public static Color ReadableInkOn(Color fill)
    {
        var dark = Color.FromRgb(0x17, 0x18, 0x1A);
        var white = Color.FromRgb(0xFF, 0xFF, 0xFF);
        return Contrast(dark, fill) >= Contrast(white, fill) ? dark : white;
    }

    private static double Contrast(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color c)
    {
        static double Ch(byte v) { double x = v / 255.0; return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4); }
        return 0.2126 * Ch(c.R) + 0.7152 * Ch(c.G) + 0.0722 * Ch(c.B);
    }

    /// <summary>A top-to-bottom gradient over palette slots. A LinearGradientBrush snapshots
    /// its stop Colors, so unlike a fill it cannot ride a slot on its own — this registers it
    /// for re-tinting. Registration is permanent: only ever assign the result to a static
    /// field, never build one per control instance.</summary>
    public static LinearGradientBrush VGradient(params (SolidColorBrush Slot, double Offset)[] stops)
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        foreach (var (slot, off) in stops) g.GradientStops.Add(new GradientStop(slot.Color, off));
        Gradients.Add((Array.ConvertAll(stops, t => t.Slot), g));
        return g;
    }

    /// <summary>The identity hue of one visualiser or picker swatch — a colour that means
    /// "this band / this quadrant / this tag", not a role in the design system. Give the
    /// dark value; the light counterpart is derived (same hue, darkened so it reads on the
    /// light grey ground) unless <see cref="InkOverrides"/> pins one.</summary>
    public static SolidColorBrush Ink(string darkHex)
    {
        var key = darkHex.ToUpperInvariant();
        if (InkCache.TryGetValue(key, out var b)) return b;
        // A hex that already is a role token resolves to that token, so it gets the designed
        // light value instead of a derived one. Hexes that were Ember role tokens (views still
        // name a few) resolve to the role's current slot, so no Ember hue survives the switch.
        if (LegacyRole(key) is { } role) { InkCache[key] = role; return role; }
        var dark = Color.Parse(darkHex);
        foreach (var slot in Slots)
            if (slot.Dark == dark) { InkCache[key] = slot.Brush; return slot.Brush; }
        b = T(darkHex, InkOverrides.TryGetValue(key, out var pin) ? pin : DeriveLight(Color.Parse(darkHex)));
        InkCache[key] = b;
        return b;
    }

    // Ember role hexes that views pass to Ink() → the slot that plays that role now. A method,
    // not a table: the slots it names are initialised further down this file.
    private static SolidColorBrush? LegacyRole(string hex) => hex switch
    {
        "#D8A03D" => Accent,      // brass
        "#5B9E9C" => Teal,
        "#B57286" => Rose,
        "#6D8FB5" => Steel,
        "#B8862E" => Threshold,   // brass-adjacent data fill → amber
        "#2A2317" => AccentSubtle, // brass wash band
        _ => null,
    };

    /// <summary>The Color of an <see cref="Ink"/> hue, for the call sites that mix or
    /// interpolate rather than fill. Read it at draw time — it changes with the theme.</summary>
    public static Color InkColor(string darkHex) => Ink(darkHex).Color;

    // Light counterparts that the generic derivation gets wrong — a hue whose job
    // is to be *pale* on the dark ground (a highlight) or whose saturation collapses when darkened.
    private static readonly Dictionary<string, string> InkOverrides = new()
    {
        ["#8A8F98"] = "#5A6068",   // tag "slate": derivation leaves it near-neutral mud
        ["#CDEEF8"] = "#0E5470",   // resize-edge / bracket highlight: hottest ink on light is darkest
        ["#12181B"] = "#DCE6E8",   // BPM chip ground — a surface, so it lightens rather than darkens
        ["#1A2B31"] = "#C8DEE4",   // …and its hot state
    };

    // Same hue and saturation, lightness reflected onto the light ground: the brighter a
    // hue reads on dark, the darker it must read on light to carry the same emphasis.
    private static string DeriveLight(Color c)
    {
        var (h, s, l) = ToHsl(c);
        double nl = Math.Clamp(73.3 - 0.545 * l, 18, 58);
        double ns = Math.Clamp(s * 1.12, 0, 100);
        var o = FromHsl(h, ns, nl);
        return $"#{o.R:X2}{o.G:X2}{o.B:X2}";
    }

    private static (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        double l = (max + min) / 2, s = d == 0 ? 0 : d / (1 - Math.Abs(2 * l - 1));
        double h = d == 0 ? 0
            : max == r ? 60 * (((g - b) / d) % 6)
            : max == g ? 60 * ((b - r) / d + 2)
            : 60 * ((r - g) / d + 4);
        return ((h + 360) % 360, s * 100, l * 100);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        s /= 100; l /= 100;
        double cc = (1 - Math.Abs(2 * l - 1)) * s, x = cc * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - cc / 2;
        (double r, double g, double b) = h < 60 ? (cc, x, 0.0) : h < 120 ? (x, cc, 0.0) : h < 180 ? (0.0, cc, x)
            : h < 240 ? (0.0, x, cc) : h < 300 ? (x, 0.0, cc) : (cc, 0.0, x);
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    // =======================================================================
    // Tokens — the Studio palette (after Saine): neutral greys, one orange accent.
    // dark = Saine's dark preset, light = Saine's light preset (same roles).
    // =======================================================================

    // ---- Surfaces: Saine's grey ladder, far to near -----------------------
    // Well (recess) · Ground (bg-base) · Panel · Raised · Raised hover. Ember's nine
    // steps collapse onto these; the extra slots sit between two of them. Depth is
    // tone plus a 1 px seam; only floating layers cast a shadow.
    public static readonly SolidColorBrush SurfaceAbyss  = T("#17181A", "#D6D8DB"); // Brush.SurfaceAbyss — well: the transport console recess
    public static readonly SolidColorBrush BgApp         = T("#1E1F22", "#C9CBCF"); // Brush.BgApp — ground: window, arrangement, plugin body
    public static readonly SolidColorBrush Gutter        = T("#141517", "#9DA0A6"); // Brush.Gutter — the seam the docked panels meet on
    public static readonly SolidColorBrush BgSunken      = T("#17181A", "#D6D8DB"); // Brush.BgSunken / ChromeBg — Well
    public static readonly SolidColorBrush CanvasBg      = T("#1E1F22", "#C9CBCF"); // modular canvas, just above the well
    public static readonly SolidColorBrush SurfaceDeep   = T("#1B1C1F", "#CFD1D4"); // step-sequencer ground
    public static readonly SolidColorBrush LaneB         = T("#222326", "#C4C6CA"); // Brush.LaneB (alternating lane)
    public static readonly SolidColorBrush Panel         = T("#2B2D31", "#E4E5E7"); // Brush.Panel — browser, headers, inspector
    public static readonly SolidColorBrush SurfaceCard   = T("#2B2D31", "#E4E5E7"); // Brush.SurfaceCard — a section inside a device (panel tone; divided by rules)
    public static readonly SolidColorBrush SurfaceInset  = T("#232427", "#DCDEE0"); // Brush.SurfaceInset — rail, sunken chip, hovered well
    public static readonly SolidColorBrush SurfaceRaised = T("#34363B", "#EEEFF1"); // Brush.SurfaceRaised — button at rest
    public static readonly SolidColorBrush SurfaceHover  = T("#393C41", "#F7F8F9"); // Brush.SurfaceHover
    public static readonly SolidColorBrush TrackOff      = T("#17181A", "#D6D8DB"); // Brush.TrackOff — track of an off toggle
    // Pressed goes *into* the recess rather than to a fourth raised step.
    public static readonly SolidColorBrush SurfaceActive = T("#17181A", "#D6D8DB"); // Brush.SurfaceActive

    // ---- Grid + graph lines (kept under ~12% contrast against their lane) -
    public static readonly SolidColorBrush GridSubBeat = T("#242528", "#C3C5C9"); // finer than a beat
    public static readonly SolidColorBrush GridBeat    = T("#2A2C30", "#BDC0C4"); // Brush.GridBeat — grid inside graphs, zero axis
    public static readonly SolidColorBrush GridRow     = T("#1B1C1F", "#C5C7CB"); // piano-roll row divider
    public static readonly SolidColorBrush GridBar     = T("#3A3C41", "#ADB0B5"); // Brush.GridBar
    public static readonly SolidColorBrush GraphBorder = T("#45484E", "#B3B6BB"); // inner border of a visualiser well
    public static readonly SolidColorBrush WellGrid    = T("#2E2F32", "#C0C3C6"); // grid drawn inside a well
    public static readonly SolidColorBrush PadGrid     = T("#2A2C30", "#C8CACD"); // XY-pad grid

    // ---- Text: eight steps, strictly by descending importance ------------
    // Cool off-white on dark, near-black on light. Muted text takes the
    // next Ink step down — never opacity, which makes contrast unpredictable.
    public static readonly SolidColorBrush TextHeading   = T("#F4F5F6", "#0E0F11"); // Ink 0 — page titles, project name. Rare.
    public static readonly SolidColorBrush TextPrimary   = T("#E8E9EB", "#16171A"); // Ink 1 — device/track/preset names, values
    public static readonly SolidColorBrush TextStrong    = T("#D4D6DA", "#282A2E"); // Ink 2 — button text, transport readouts
    public static readonly SolidColorBrush TextSecondary = T("#C0C4C9", "#3A3D42"); // Ink 3 — inactive but readable, metadata
    public static readonly SolidColorBrush TextMuted     = T("#A4A9B0", "#4F535A"); // Ink 4 — explanations, units, hints
    public static readonly SolidColorBrush TextTertiary  = T("#92969D", "#65696F"); // Ink 5 — caps labels above parameters
    public static readonly SolidColorBrush TextDisabled  = T("#82868C", "#797C81"); // Ink 6 — placeholder, disabled action
    public static readonly SolidColorBrush TextAxis      = T("#82868C", "#797C81"); // Ink 7 — axis labels inside graphs only
    public static readonly SolidColorBrush TextOnAccent  = T("#17181A", "#17181A"); // Brush.TextOnAccent
    public static readonly SolidColorBrush TextFaint     = T("#82868C", "#797C81"); // Brush.TextFaint — disabled controls only
    // The neutral ink a value is drawn in at rest: knob arcs, fader fills, graph curves.
    // The accent replaces it only while the control is live (dragged, hovered) or chosen.
    public static readonly SolidColorBrush VizInk        = T("#D7D8DA", "#252629"); // Brush.VizInk
    public static readonly SolidColorBrush VizGrid       = T("#2E2F32", "#C0C3C6"); // Brush.VizGrid — grid inside a well

    // ---- Borders ----------------------------------------------------------
    public static readonly SolidColorBrush BorderDefault = T("#141517", "#9DA0A6"); // Brush.BorderDefault
    public static readonly SolidColorBrush BorderStrong  = T("#45484E", "#B3B6BB"); // Brush.BorderStrong — knob cap, hovered border
    public static readonly SolidColorBrush BorderBrass   = T("#BF7D22", "#ECA54E"); // Brush.BorderBrass — accent line: 70% accent over panel
    // Saine's two lines: the dark seam between panels and around controls, and the
    // lighter rule that divides inside a panel (toolbar separators, field edges).
    public static readonly SolidColorBrush Seam          = T("#141517", "#9DA0A6"); // Brush.Seam
    public static readonly SolidColorBrush Rule          = T("#45484E", "#B3B6BB"); // Brush.Rule

    // ---- Accent: Studio orange. Live or chosen only — playing, selected, looping,
    //      toggled on, or a control while it is dragged. Never a resting value. ---
    public static readonly SolidColorBrush Accent       = T("#FF9F1C", "#EF8A0C"); // Brush.Accent — playhead, selection, toggled-on fill
    public static readonly SolidColorBrush AccentBright = T("#FFB547", "#C46A00"); // Brush.AccentBright — accent glow: the line/ink variant
    public static readonly SolidColorBrush AccentHover  = T("#FFB547", "#C46A00"); // Brush.AccentHover — accent text on hover
    public static readonly SolidColorBrush AccentDeep   = T("#C77612", "#D47800"); // Brush.AccentDeep — press, left half of the loop
    public static readonly SolidColorBrush AccentDim    = T("#C77612", "#F7C27A"); // Brush.AccentDim — the soft/pressed accent
    public static readonly SolidColorBrush AccentEdge   = T("#BF7D22", "#ECA54E"); // Brush.AccentEdge — chip border, link underline
    public static readonly SolidColorBrush AccentMute   = T("#7A5F3D", "#C9A77A"); // a muted accent; nothing at rest uses it any more
    public static readonly SolidColorBrush AccentPale   = T("#FFD08A", "#8A4A00"); // hot edge highlight
    // Accent-soft: 16 % accent over the panel, opaque so it reads the same on any ground.
    public static readonly SolidColorBrush AccentSubtle = T("#4D3F2E", "#E6D6C4"); // Brush.AccentSubtle — accent-soft wash

    // ---- Status -----------------------------------------------------------
    // Red belongs to recording. Everything else red is an alert about level.
    public static readonly SolidColorBrush Record      = T("#F06A6E", "#B3262C"); // Brush.Record — active recording, the only claim on red
    public static readonly SolidColorBrush RecordInk   = T("#FFFFFF", "#FFFFFF"); // Brush.RecordInk — disc on an engaged record button
    public static readonly SolidColorBrush Success     = T("#6CC673", "#22692B"); // Brush.Success — Signal, working meter zone
    public static readonly SolidColorBrush SuccessDim  = T("#3F8A45", "#79B880"); // Brush.SuccessDim — transport input, metronome
    public static readonly SolidColorBrush Warning     = T("#F2B53C", "#7A4F00"); // Brush.Warning — Caution, −6..0 dB
    // Track toggles: mute is amber, solo is blue, arm is Record. Each fills solid when on.
    public static readonly SolidColorBrush Mute        = T("#F2B53C", "#7A4F00"); // Brush.Mute
    public static readonly SolidColorBrush Solo        = T("#6A9FD8", "#3F78B8"); // Brush.Solo
    public static readonly SolidColorBrush Danger      = T("#F06A6E", "#B3262C"); // Brush.Danger — Alert, overload / clipping
    public static readonly SolidColorBrush DangerHover = T("#F48386", "#C2343A"); // Brush.DangerHover
    public static readonly SolidColorBrush DangerBright= T("#F58F92", "#9C1F24"); // Brush.DangerBright — Alert Light, readout text
    public static readonly SolidColorBrush DangerDeep  = T("#C4474B", "#8E1C21"); // over-threshold fill, below Alert
    public static readonly SolidColorBrush DangerPale  = T("#F7A5A7", "#7F161B"); // live-take waveform / label

    // ---- Data + modulation ------------------------------------------------
    // Role chromas live inside visualisers only. When two or three sources share
    // one graph, VizInk takes the primary and these follow in order. They never
    // reach a button and they never mean state.
    public static readonly SolidColorBrush Teal       = T("#5FB3A8", "#3A8A7F"); // Brush.ChromaTeal — 2nd layer; also the modulation accent
    public static readonly SolidColorBrush TealBright = T("#86C9C0", "#2E7067"); // Brush.ChromaTealLight
    public static readonly SolidColorBrush Rose       = T("#D77A8F", "#B4566B"); // Brush.ChromaRose — 3rd layer: right channel, highs, tail
    public static readonly SolidColorBrush RoseBright = T("#E49EAE", "#93404F"); // Brush.ChromaRoseLight
    public static readonly SolidColorBrush Steel      = T("#6A9FD8", "#3F78B8"); // Brush.ChromaSteel — 4th layer, used rarely
    public static readonly SolidColorBrush Sage       = T("#7FB08A", "#4A7F56"); // standalone hue (no longer a track slot)
    public static readonly SolidColorBrush Threshold  = T("#E0A84A", "#8A5F14"); // threshold markers
    public static readonly SolidColorBrush Marker     = T("#7FC4E0", "#1B6E8C"); // clip markers / brackets
    public static readonly SolidColorBrush MarkerHot  = T("#CDEEF8", "#0E5470"); // resize-edge affordance
    public static readonly SolidColorBrush Frozen     = T("#7FC7EC", "#1C6E96"); // frozen-track ice
    public static readonly SolidColorBrush SignalIn   = T("#5A6170", "#7A818C"); // ghost "input" trace label
    public static readonly SolidColorBrush SignalInFill = T("#3A3E46", "#C9CDD3");
    public static readonly SolidColorBrush HandleSel  = T("#FFFFFF", "#16171A"); // selected graph handle

    // ---- Tinted chrome ----------------------------------------------------
    public static readonly SolidColorBrush SelHeaderBg    = T("#393C41", "#F7F8F9"); // selected track header — raised hover (the accent tick marks it)
    public static readonly SolidColorBrush FrozenHeaderBg = T("#24323B", "#D3E2EA"); // frozen track header
    public static readonly SolidColorBrush MuteTint       = T("#3B3426", "#BDB49F"); // muted band
    public static readonly SolidColorBrush Veil           = T("#1E1F22", "#E4E5E7"); // deactivated-clip scrim base
    public static readonly SolidColorBrush TooltipBg      = T("#34363B", "#EEEFF1"); // tooltip: a raised popover
    public static readonly SolidColorBrush TooltipText    = T("#E8E9EB", "#16171A");

    // ---- Piano keys (the clip editor keyboard is dark: keys recede, notes lead) ---
    public static readonly SolidColorBrush KeyWhite     = T("#2B2D31", "#EEEFF1"); // piano-roll keys: dark, never the brightest thing
    public static readonly SolidColorBrush KeyBlack     = T("#17181A", "#C9CBCF"); // black keys and their lanes
    public static readonly SolidColorBrush MiniKeyWhite = T("#3A3C41", "#F7F8F9"); // device-card mini keyboards
    public static readonly SolidColorBrush MiniKeyBlack = T("#17181A", "#3A3D42");

    // ---- Track-shade mix targets (TrackColorForIndex) ---------------------
    public static readonly SolidColorBrush ShadeUp   = T("#FFFFFF", "#FFFFFF"); // "lighter" shade
    public static readonly SolidColorBrush ShadeDown = T("#17181A", "#16171A"); // "darker" shade

    // ---- Track palette (Brush.Track1..8) + return buses --------------------
    // Nine roles in Saine's muted clip-colour style: its five clip hues (teal, blue,
    // amber, violet, rose) plus four at matching chroma. A group sets the hue and its
    // tracks take neighbouring shades of it. Light values are Saine's light rendition.
    //
    // Saved projects are NOT migrated: a stored colour index keeps working but now
    // points at the hue that sits at that place in this table.
    private static readonly SolidColorBrush[] TrackSlots =
    {
        T("#7FB87A", "#4A7F46"), // Drums
        T("#5FB3A8", "#3A8A7F"), // Perc
        T("#5AA6C4", "#357C99"), // Bass
        T("#6A9FD8", "#3F78B8"), // Keys
        T("#E0A84A", "#B8862F"), // Texture
        T("#9D90C9", "#7363A8"), // FX
        T("#D77A8F", "#B4566B"), // Brass
        T("#B3B45E", "#7E7F2F"), // Vox
        T("#8E9DB0", "#5D6C80"), // Return
    };
    // The master bus: Saine's muted grey, not a hue — it is the sum, not a track.
    public static readonly SolidColorBrush Master = T("#A4A9B0", "#4F535A"); // Brush.Master
    // Return buses draw from the ninth role rather than a palette of their own.
    private static readonly SolidColorBrush[] ReturnSlots = { T("#8E9DB0", "#5D6C80"), T("#7A889A", "#4E5B6D") };

    /// <summary>Brush form of the 8-track cycle — index it for fills that follow the theme.</summary>
    public static readonly SolidColorBrush[] TrackBrushes = TrackSlots;
    public static readonly SolidColorBrush[] ReturnBrushes = ReturnSlots;

    /// <summary>The 8-track colour cycle + the two return-bus tints, as Colors for the views
    /// that mix or alpha-blend them. Rewritten in place by <see cref="Apply"/> — read at draw
    /// time (the array reference is stable, its contents are not).</summary>
    public static readonly Color[] TrackColors = { TrackSlots[0].Color, TrackSlots[1].Color, TrackSlots[2].Color, TrackSlots[3].Color, TrackSlots[4].Color, TrackSlots[5].Color, TrackSlots[6].Color, TrackSlots[7].Color, TrackSlots[8].Color };
    public static readonly Color[] ReturnColors = { ReturnSlots[0].Color, ReturnSlots[1].Color };

    // ---- Lookup by XAML key ------------------------------------------------

    // Code-built views that read a token by its XAML key resolve it here instead of
    // through TryFindResource: a resource read is a one-time snapshot, while the slot
    // brush follows the variant. (Bind-style reads — ControlExtensions.BindResource —
    // are already live and should keep using the resource observable.)
    private static readonly Dictionary<string, SolidColorBrush> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Brush.SurfaceAbyss"] = SurfaceAbyss,   ["Brush.Gutter"] = Gutter,
        ["Brush.BgSunken"] = BgSunken,           ["Brush.ChromeBg"] = BgSunken,
        ["Brush.BgApp"] = BgApp,                 ["Brush.LaneB"] = LaneB,
        ["Brush.Panel"] = Panel,                 ["Brush.TrackOff"] = TrackOff,
        ["Brush.SurfaceCard"] = SurfaceCard,     ["Brush.SurfaceRaised"] = SurfaceRaised,
        ["Brush.SurfaceInset"] = SurfaceInset,
        ["Brush.SurfaceHover"] = SurfaceHover,   ["Brush.SurfaceActive"] = SurfaceActive,
        ["Brush.SurfaceSelected"] = SelHeaderBg,
        ["Brush.GridBeat"] = GridBeat,           ["Brush.GridBar"] = GridBar,
        ["Brush.Hairline"] = GraphBorder,
        ["Brush.TextHeading"] = TextHeading,     ["Brush.TextPrimary"] = TextPrimary,
        ["Brush.TextStrong"] = TextStrong,       ["Brush.TextSecondary"] = TextSecondary,
        ["Brush.TextMuted"] = TextMuted,         ["Brush.TextTertiary"] = TextTertiary,
        ["Brush.TextDisabled"] = TextDisabled,   ["Brush.TextAxis"] = TextAxis,
        ["Brush.TextOnAccent"] = TextOnAccent,
        ["Brush.BorderDefault"] = BorderDefault, ["Brush.BorderStrong"] = BorderStrong,
        ["Brush.BorderBrass"] = BorderBrass,
        ["Brush.Accent"] = Accent,               ["Brush.AccentHover"] = AccentHover,
        ["Brush.AccentBright"] = AccentBright,   ["Brush.AccentDeep"] = AccentDeep,
        ["Brush.AccentDim"] = AccentDim,         ["Brush.AccentEdge"] = AccentEdge,
        ["Brush.AccentSubtle"] = AccentSubtle,
        ["Brush.Record"] = Record,               ["Brush.RecordInk"] = RecordInk,
        ["Brush.Success"] = Success,
        ["Brush.SuccessDim"] = SuccessDim,       ["Brush.Warning"] = Warning,
        ["Brush.Danger"] = Danger,               ["Brush.DangerHover"] = DangerHover,
        ["Brush.DangerBright"] = DangerBright,
        ["Brush.ChromaTeal"] = Teal,             ["Brush.ChromaTealLight"] = TealBright,
        ["Brush.ChromaRose"] = Rose,             ["Brush.ChromaRoseLight"] = RoseBright,
        ["Brush.ChromaSteel"] = Steel,
        ["Brush.Seam"] = Seam,                   ["Brush.Rule"] = Rule,
        ["Brush.TextFaint"] = TextFaint,         ["Brush.Mute"] = Mute,
        ["Brush.Solo"] = Solo,                   ["Brush.VizInk"] = VizInk,
        ["Brush.VizGrid"] = VizGrid,
    };

    /// <summary>The slot behind a <c>Brush.*</c> XAML key, or null if the key is unknown.</summary>
    public static SolidColorBrush? ByKey(string key) => KeyMap.GetValueOrDefault(key);

    // ---- Raw Colors — properties, never cached: they move with the theme --
    public static Color AccentColor => Accent.Color;
    public static Color AccentBrightColor => AccentBright.Color;
    public static Color SuccessColor => Success.Color;
    public static Color TealColor => Teal.Color;
}
