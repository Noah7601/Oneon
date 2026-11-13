// Transfer zone ("neon zone") of the donut: the ring a preset slice fans out with the places its result can be sent.
// It reuses the group ring's layout, hit-testing and open/close logic (DonutMenu.Groups.cs); only its look differs:
// dark glass slices with a neon green outline, and a lit slice glows neon green, like the green slice of the Oneon icon.

using Avalonia;
using Avalonia.Media;

namespace Filee.App.Controls;

public sealed partial class DonutMenu
{
    /// <summary>Neon green of the transfer zone (the green slice of the app icon).</summary>
    private static readonly Color Neon = Color.Parse("#39FF14");

    /// <summary>Text on a lit neon slice: near-black reads best on the bright green.</summary>
    private static readonly IBrush NeonInk = new SolidColorBrush(Color.Parse("#0B1A07"));

    private static readonly IBrush NeonBase = new SolidColorBrush(Color.FromArgb(232, 22, 22, 26));
    private static readonly IBrush NeonFill = new SolidColorBrush(Neon);
    private static readonly Pen NeonEdge = new(new SolidColorBrush(Neon, 0.85), 1.5);

    /// <summary>Soft glow: a few wide, faint strokes under the outline (no blur effect, so it costs nothing).</summary>
    private static readonly Pen[] NeonGlow =
    [
        new(new SolidColorBrush(Neon, 0.10), 10, lineJoin: PenLineJoin.Round),
        new(new SolidColorBrush(Neon, 0.16), 6, lineJoin: PenLineJoin.Round),
        new(new SolidColorBrush(Neon, 0.24), 3, lineJoin: PenLineJoin.Round),
    ];

    /// <summary>One slice of the transfer zone: dark glass outlined in neon; lit, it fills with neon and glows.</summary>
    private static void DrawNeonSlice(DrawingContext context, Geometry wedge, bool hot, bool enabled)
    {
        using (context.PushOpacity(enabled ? 1 : 0.45))
        {
            if (hot)
            {
                foreach (var glow in NeonGlow)
                    context.DrawGeometry(null, glow, wedge);
                context.DrawGeometry(NeonFill, null, wedge);
                return;
            }
            context.DrawGeometry(NeonBase, NeonEdge, wedge);
        }
    }
}
