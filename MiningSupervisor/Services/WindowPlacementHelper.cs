using System.Windows;
using MiningSupervisor.Models;

namespace MiningSupervisor.Services;

/// <summary>Restores a window's saved position/size and records it again when the window closes.</summary>
public static class WindowPlacementHelper
{
    // How much of a restored window must remain on a screen for the position to be trusted.
    private const double MinVisible = 50;

    public static void Attach(Window window, AppState state, string key, Action? save = null)
    {
        if (state.WindowPlacements.TryGetValue(key, out var placement) && IsOnScreen(placement))
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = placement.Left;
            window.Top = placement.Top;

            if (window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
            {
                window.Width = placement.Width;

                // With SizeToContent=Height the height is dictated by the content.
                if (window.SizeToContent is SizeToContent.Manual or SizeToContent.Width)
                {
                    window.Height = placement.Height;
                }
            }
        }

        window.Closing += (_, _) =>
        {
            var bounds = window.WindowState == WindowState.Normal
                ? new Rect(window.Left, window.Top, window.Width, window.Height)
                : window.RestoreBounds;

            state.WindowPlacements[key] = new WindowPlacement
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height
            };

            save?.Invoke();
        };
    }

    private static bool IsOnScreen(WindowPlacement p)
    {
        if (double.IsNaN(p.Left) || double.IsNaN(p.Top) || p.Width <= 0 || p.Height <= 0)
        {
            return false;
        }

        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var visible = Rect.Intersect(screen, new Rect(p.Left, p.Top, p.Width, p.Height));
        return !visible.IsEmpty && visible.Width >= MinVisible && visible.Height >= MinVisible;
    }
}
