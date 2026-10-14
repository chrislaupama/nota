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

        // Folding: a header keeps its count, its devices leave the list, the status counts
        // don't change; a search shows hits inside a folded category and leaves the folds alone.
        var counts0 = vm.VisibleDeviceCounts(1);
        var dynamics = vm.Effects.First(r => r.IsGroup && r.Name == "Dynamics");
        vm.ToggleCategoryFold(dynamics);
        var dyn = vm.Effects.First(r => r.IsGroup && r.Name == "Dynamics");
        var dynamicsDevices = new[] { "Nota Compressor", "Nota Prism", "Nota Ceiling", "Nota Shutter" };
        yield return (dyn.IsFolded && dyn.GroupCount == 4 && !vm.Effects.Any(r => dynamicsDevices.Contains(r.Name))
                      && vm.Effects.SkipWhile(r => r != dyn).Skip(1).First().IsGroup,
            "folding a category hides its devices and keeps the header with its count");
        yield return (vm.VisibleDeviceCounts(1) == counts0 && vm.FoldedDeviceCount(1) == 4, "folding leaves the device counts alone");
        yield return (vm.FoldedCategories.SequenceEqual(new[] { "fx:Dynamics" }), "the fold is recorded per tab");
        yield return (vm.Instruments.Where(r => r.IsGroup).All(r => !r.IsFolded) && vm.Instruments.Any(r => r.Name == "Nota Instrument Rack"),
            "a fold on one tab leaves the others open");

        vm.FilterTree(1, "Prism");
        var hit = vm.Effects.ToList();
        var hdr = hit.FirstOrDefault(r => r.IsGroup && r.Name == "Dynamics");
        yield return (hdr is { IsFoldable: false, IsFolded: false } && hit.Any(r => r.Name == "Nota Prism") && vm.FoldedDeviceCount(1) == 0,
            "a search shows hits inside a folded category, its header not foldable meanwhile");
        vm.ToggleCategoryFold(hdr!);
        yield return (vm.FoldedCategories.SequenceEqual(new[] { "fx:Dynamics" }), "folding during a search does nothing");
        vm.FilterTree(1, "");
        yield return (vm.Effects.First(r => r.IsGroup && r.Name == "Dynamics").IsFolded && !vm.Effects.Any(r => r.Name == "Nota Prism"),
            "clearing the search restores the fold");

        vm.ToggleCategoryFold(vm.Effects.First(r => r.IsGroup && r.Name == "Dynamics"));
        yield return (vm.FoldedCategories.Count == 0 && vm.Effects.Any(r => r.Name == "Nota Prism"), "a second click opens it again");

        vm.SetFoldedCategories(new[] { "fx:Dynamics", "fx:Nonexistent", "bogus", "midi:Shape" });
        yield return (vm.FoldedCategories.OrderBy(k => k).SequenceEqual(new[] { "fx:Dynamics", "midi:Shape" })
                      && vm.Effects.First(r => r.IsGroup && r.Name == "Dynamics").IsFolded
                      && vm.MidiEffects.First(r => r.IsGroup && r.Name == "Shape").IsFolded,
            "saved folds restore; unknown keys are dropped");
        vm.SetFoldedCategories(Array.Empty<string>());

        // Plug-ins join the categories from the category they report (FR-005).
        var mapping = new (bool Inst, string Reported, string Expected)[]
        {
            (false, "Fx/Reverb", "Reverb & delay"), (false, "Fx|Dynamics|EQ", "Dynamics"), (false, "Fx/Pitch Shift", "Pitch & time"),
            (false, "Fx/Analyzer", "Utility"), (true, "Instrument/Synth/Sampler", "Synths"), (true, "Instrument/Drum", "Drums"),
            (false, "Effect", DeviceCategories.Other), (false, "", DeviceCategories.Other), (false, "Fx", DeviceCategories.Other),
        };
        var wrong = mapping.Where(m => DeviceCategories.ForPlugin(m.Inst, m.Reported) != m.Expected)
            .Select(m => $"{m.Reported} → {DeviceCategories.ForPlugin(m.Inst, m.Reported)}").ToList();
        yield return (wrong.Count == 0, wrong.Count == 0 ? "plug-in categories map by their first recognised token" : $"plug-in mapping: {string.Join(", ", wrong)}");

        var plugins = new FakePluginCatalog(
            "Ambience | VST3 | fx | Smart | Fx/Reverb",
            "Mystery | AU | fx | Acme | Effect",
            "Ana | VST3 | inst | Sonic | Instrument/Synth");
        var pvm = new BrowserViewModel(plugins, new EmptyPresetLibrary(), new FactoryPresetCatalog());
        List<string> Shelf(IList<BrowserItem> rows, string name)
            => rows.SkipWhile(r => !(r.IsGroup && r.Name == name)).Skip(1).TakeWhile(r => !r.IsGroup).Select(r => r.ShortName).ToList();
        var reverbs = Shelf(pvm.Effects, "Reverb & delay");
        yield return (reverbs.SequenceEqual(new[] { "Chamber", "Delay", "Reverb", "Ambience" }), $"Group by source on: built-ins, then plug-ins, in a category ({string.Join(", ", reverbs)})");
        var fxHeaders = pvm.Effects.Where(r => r.IsGroup).Select(r => r.Name).ToList();
        yield return (fxHeaders.Last() == DeviceCategories.Other && Shelf(pvm.Effects, DeviceCategories.Other).SequenceEqual(new[] { "Mystery" }),
            "an unrecognised plug-in lands under Other, the last header");
        yield return (Shelf(pvm.Instruments, "Synths").Last() == "Ana", "a synth plug-in joins Synths after the built-ins");
        yield return (!pvm.Effects.Concat(pvm.Instruments).Any(r => r.IsGroup && r.Name is "BUILT-IN" or "PLUG-INS"),
            "no Built-in / Plug-ins headers in the category view");
        pvm.GroupBySource = false;
        reverbs = Shelf(pvm.Effects, "Reverb & delay");
        yield return (reverbs.SequenceEqual(new[] { "Ambience", "Chamber", "Delay", "Reverb" }), $"Group by source off: plug-ins interleave by name ({string.Join(", ", reverbs)})");
        yield return (pvm.MidiEffects.Where(r => r.IsGroup).Select(r => r.Name).SequenceEqual(DeviceCategories.Order(BuiltinDeviceType.MidiEffect))
                      && pvm.VisibleDeviceCounts(1) == (26, 2),
            "plug-ins leave the MIDI tab alone; the status line counts them");

        // An existing settings file has neither key: the category view is on, nothing folded (FR-003).
        var old = JsonSerializer.Deserialize<Settings>("{}")!;
        yield return (old.BrowserCategoryView && old.BrowserFoldedCategories.Count == 0,
            "settings without the new keys open in the category view, nothing folded");
    }
}
