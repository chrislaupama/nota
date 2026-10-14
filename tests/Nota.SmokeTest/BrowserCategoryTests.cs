// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// The browser's category view (spec 009), driven through BrowserViewModel over the factory
// catalogue and fake plug-ins: headers in each tab's fixed order, every device on its own
// shelf, favourites and Group by source inside a shelf, folding, search, and the flat list
// coming back exactly when the view is off. Run alone with `--browser`.

using System.Text.Json;
using Nota.Infrastructure;
using Nota.Presentation;

namespace Nota.SmokeTest;

internal static class BrowserCategoryTests
{
    public static IEnumerable<(bool Ok, string Label)> Run()
    {
        var lib = new FakeBrowserLibrary();
        var vm = new BrowserViewModel(new EmptyPluginCatalog(), new EmptyPresetLibrary(), new FactoryPresetCatalog(), library: lib);
        var tabs = new (BuiltinDeviceType Type, IList<BrowserItem> Rows)[]
        {
            (BuiltinDeviceType.Instrument, vm.Instruments), (BuiltinDeviceType.AudioEffect, vm.Effects), (BuiltinDeviceType.MidiEffect, vm.MidiEffects),
        };
        foreach (var (type, rows) in tabs)
        {
            var headers = rows.Where(r => r.IsGroup).Select(r => r.Name).ToList();
            yield return (headers.SequenceEqual(DeviceCategories.Order(type)), $"{type}: one header per category, in order ({string.Join(" · ", headers)})");
            var seen = new List<string>();
            bool under = true, alpha = true, counts = true;
            string shelf = "", prev = "";
            int expect = 0, n = 0;
            foreach (var r in rows.Append(null))
            {
                if (r is null || r.IsGroup)
                {
                    if (shelf.Length > 0 && n != expect) counts = false;
                    if (r is null) break;
                    shelf = r.Name; prev = ""; expect = r.GroupCount; n = 0;
                    continue;
                }
                if (r.Depth > 0) continue;
                n++;
                seen.Add(r.Name);
                under &= DeviceCategories.For(BuiltinDeviceCatalog.All.First(d => d.Name == r.Name && d.Type == type)) == shelf;
                alpha &= string.Compare(prev, r.ShortName, StringComparison.OrdinalIgnoreCase) < 0;
                prev = r.ShortName;
            }
            var builtins = BuiltinDeviceCatalog.All.Where(d => d.Type == type).Select(d => d.Name).ToList();
            yield return (seen.Count == builtins.Count && seen.ToHashSet().SetEquals(builtins), $"{type}: every built-in listed exactly once ({seen.Count}/{builtins.Count})");
            yield return (under, $"{type}: each device sits under its own category");
            yield return (alpha, $"{type}: devices alphabetical by the name the row prints, within a category");
            yield return (counts, $"{type}: each header counts the devices beneath it");
        }

        // Favourites float to the top of their category, not out of it (FR-004).
        var delay = vm.Effects.First(r => r.Name == "Nota Delay");
        lib.SetFavorite(delay.LibraryKey, true);
        vm.FilterTree(1, "");
        var space = vm.Effects.SkipWhile(r => r.Name != "Reverb & delay").Skip(1).TakeWhile(r => !r.IsGroup).Select(r => r.ShortName).ToList();
        yield return (space.SequenceEqual(new[] { "Delay", "Chamber", "Reverb" }), $"a favourite leads its category ({string.Join(", ", space)})");
        lib.SetFavorite(delay.LibraryKey, false);
        vm.FilterTree(1, "");

        // Expanding a device shows its presets right beneath it, inside its category (FR-007).
        var synth = vm.Instruments.First(r => r.Name == "Nota Synth");
        vm.ToggleExpand(synth);
        int si = vm.Instruments.IndexOf(synth);
        var header = vm.Instruments.Take(si).Last(r => r.IsGroup);
        yield return (header.Name == "Synths" && vm.Instruments[si + 1].Depth > 0, "expanding a device lists its presets beneath it, in its category");
        vm.ToggleExpand(synth);

        // A search keeps matches under their headers and drops empty categories (FR-006).
        vm.FilterTree(1, "Compressor");
        var hits = vm.Effects.ToList();
        int hc = hits.FindIndex(r => r.Name == "Nota Compressor");
        bool everyHeaderHasRows = hits.Select((r, i) => (r, i)).Where(t => t.r.IsGroup)
            .All(t => hits.Skip(t.i + 1).TakeWhile(r => !r.IsGroup).Count(r => r.Depth == 0) == t.r.GroupCount && t.r.GroupCount > 0);
        yield return (hc > 0 && hits.Take(hc).Last(r => r.IsGroup).Name == "Dynamics" && everyHeaderHasRows,
              $"a search keeps hits under their category and hides the empty ones ({string.Join(" · ", hits.Where(r => r.IsGroup).Select(r => r.Name))})");
        vm.FilterTree(1, "");

        // Off restores today's flat list exactly: one Built-in header, then every built-in by
        // its full name (SC-003); on again brings the categories back.
        vm.CategoryView = false;
        foreach (var (type, rows) in tabs)
        {
            var flat = new[] { "BUILT-IN" }.Concat(BuiltinDeviceCatalog.All.Where(d => d.Type == type).Select(d => d.Name).OrderBy(x => x)).ToList();
            yield return (rows.Select(r => r.Name).SequenceEqual(flat), $"{type}: category view off shows today's flat list");
        }
        vm.CategoryView = true;
        yield return (vm.Effects.Where(r => r.IsGroup).Select(r => r.Name).SequenceEqual(DeviceCategories.Order(BuiltinDeviceType.AudioEffect)),
            "turning the category view back on restores the headers");

        // An existing settings file has neither key: the category view is on, nothing folded (FR-003).
        var old = JsonSerializer.Deserialize<Settings>("{}")!;
        yield return (old.BrowserCategoryView && old.BrowserFoldedCategories.Count == 0,
            "settings without the new keys open in the category view, nothing folded");
    }
}
