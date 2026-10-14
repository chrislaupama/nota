// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// The Mixer in its own window (opened from View → Mixer). It hosts the single MixerView
// instance the main window owns (reparented, not rebuilt), so meters/state carry over and
// the tick loop keeps updating it while the window is open. Closing detaches the mixer.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nota.App;

internal sealed class MixerWindow : NotaWindow
{
    public ContentControl Host { get; }
    public event Action? WindowClosed;

    private readonly MainWindow _owner;

    public MixerWindow(MainWindow owner)
    {
        _owner = owner;

        // Route transport keys (Space/Return) in the tunnel phase so a focused control here
        // can't steal them, exactly like the main window does.
        AddHandler(KeyDownEvent, (_, e) => _owner.HandleTransportKeyTunnel(e), RoutingStrategies.Tunnel);
        // Escape closes the mixer, unless a text field is being edited (it owns Escape then).
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape || e.Handled || FocusManager?.GetFocusedElement() is TextBox) return;
            e.Handled = true;
            Close();
        };

        Title = "Mixer";
        Width = 1040;
        Height = 460;
        MinWidth = 560;
        MinHeight = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = NotaPalette.BgApp;

        Host = new ContentControl { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        SetBody(Host);
    }

    /// <summary>Fits the window to <paramref name="stripsWidth"/> (the mixer's strip row), so
    /// strips fill it with no dead area: clamped to [MinWidth, the screen's working width].
    /// A window that grows past the screen's right edge is moved back onto it.</summary>
    public void FitWidth(double stripsWidth)
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        double max = screen is null ? double.PositiveInfinity : screen.WorkingArea.Width / screen.Scaling;
        double w = Math.Clamp(Math.Ceiling(stripsWidth), MinWidth, Math.Max(MinWidth, max));
        if (Math.Abs(w - Width) < 0.5) return;
        Width = w;
        if (screen is not null && IsVisible)
        {
            int right = screen.WorkingArea.Right, px = (int)Math.Ceiling(w * screen.Scaling);
            if (Position.X + px > right) Position = new PixelPoint(Math.Max(screen.WorkingArea.X, right - px), Position.Y);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        Host.Content = null;   // detach the mixer so it can be re-hosted next time
        WindowClosed?.Invoke();
    }
}
