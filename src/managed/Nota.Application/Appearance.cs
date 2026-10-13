// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Which colour theme to wear, from what settings.json says (spec 007). Pure: no UI, no
// I/O, so the smoke test can walk every case. Themes themselves live in the app layer;
// this only needs their ids and whether each is dark. Old settings (Dark / Light /
// System) migrate here, and an unknown or wrong-kind id falls back to the Studio theme of
// the kind needed — resolving never throws and never writes.

using System.Collections.Generic;

namespace Nota.Application;

/// <summary>A resolved appearance: one theme, or follow the OS with a theme per mode. Every
/// id is known and of the right kind.</summary>
public sealed record AppearanceChoice(bool FollowSystem, string Single, string Dark, string Light)
{
    /// <summary>The theme to wear now, given whether the OS is in dark mode.</summary>
    public string Active(bool osIsDark) => FollowSystem ? (osIsDark ? Dark : Light) : Single;
}

public static class Appearance
{
    public const string StudioDark = "studio-dark";
    public const string StudioLight = "studio-light";
    /// <summary>The <see cref="Settings.ColourTheme"/> value that means "follow the OS".</summary>
    public const string System = "system";

    /// <summary>Resolve the stored settings against the known themes (id → is it dark).</summary>
    public static AppearanceChoice Resolve(Settings s, IReadOnlyDictionary<string, bool> known)
    {
        var stored = s.ColourTheme;
        if (string.IsNullOrEmpty(stored))
            stored = s.Theme switch
            {
                AppTheme.Light => StudioLight,
                AppTheme.System => System,
                _ => StudioDark,
            };

        string dark = known.TryGetValue(s.SystemDarkTheme ?? "", out var d) && d ? s.SystemDarkTheme! : StudioDark;
        string light = known.TryGetValue(s.SystemLightTheme ?? "", out var l) && !l ? s.SystemLightTheme! : StudioLight;

        if (stored == System) return new AppearanceChoice(true, dark, dark, light);
        string single = known.ContainsKey(stored) ? stored : StudioDark;
        return new AppearanceChoice(false, single, dark, light);
    }

    /// <summary>Write <paramref name="choice"/> into <paramref name="s"/>, keeping the legacy
    /// <see cref="Settings.Theme"/> in step (the active theme's kind, or System when
    /// following) so older versions still open in a sensible variant.</summary>
    public static void Store(Settings s, AppearanceChoice choice, bool singleIsDark)
    {
        s.ColourTheme = choice.FollowSystem ? System : choice.Single;
        s.SystemDarkTheme = choice.Dark;
        s.SystemLightTheme = choice.Light;
        s.Theme = choice.FollowSystem ? AppTheme.System : singleIsDark ? AppTheme.Dark : AppTheme.Light;
    }
}
