// Thin view for the live donut: forwards drag & drop and pointer events to RadialController.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Filee.App.Controls;
using Filee.App.Services;

namespace Filee.App.Views;

public partial class RadialWindow : Window
{
    public RadialWindow()
    {
        InitializeComponent();
        Motion.Track(this);

        DragDrop.AddDragEnterHandler(Root, OnDragEnter);
        DragDrop.AddDragOverHandler(Root, OnDragOver);
        DragDrop.AddDragLeaveHandler(Root, OnDragLeave);
        DragDrop.AddDropHandler(Root, OnDrop);
    }

    public DonutMenu DonutControl => Donut;

    /// <summary>Files entered the window during a drag.</summary>
    public event Action<IReadOnlyList<string>>? FilesEntered;

    /// <summary>A drop landed on the donut: (hit, files).</summary>
    public event Action<DonutHit, IReadOnlyList<string>>? Dropped;

    public event Action? DragLeft;

    /// <summary>
    /// After a drag ends (drop or leave): how many drag-over events came in, where the last one was and how far that
    /// slice was drawn lit up (0 to 1).
    /// </summary>
    public event Action<int, DonutHit, double>? DragSummary;

    private int _dragOverEvents;
    private DonutHit _lastDragHit;
    private DragDropEffects _sourceEffects;

    /// <summary>
    /// True when the drag source allows no effect the ring can report, so the system refuses the drop: on macOS a
    /// Finder drag with ⌘ held only allows "generic", which Avalonia does not map, and no Drop event ever comes.
    /// </summary>
    public bool SourceRefusesDrop => _dragOverEvents > 0 && _sourceEffects == DragDropEffects.None;

    /// <summary>Where the last drag-over event was on the donut (where a refused drop was released).</summary>
    public DonutHit LastDragHit => _lastDragHit;
    private Point? _lastDragAt;

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var files = LocalFiles(e);
        if (files.Count == 0)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }
        _dragOverEvents = 0;
        _lastDragAt = null;
        FilesEntered?.Invoke(files);
        OnDragOver(sender, e);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var at = e.GetPosition(Donut);
        var hit = Donut.HitTest(at);
        _sourceEffects = e.DragEffects;
        _dragOverEvents++;
        _lastDragHit = hit;
        _lastDragAt = at;
        var files = e.DataTransfer.Contains(DataFormat.File);
        Donut.SetExternalHit(hit, at);

        e.DragEffects = files && Donut.Converts(hit) ? AcceptedEffect(e.DragEffects) : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    /// The effect to report for a drop the ring takes, given the effects the drag source allows. COPY whenever it is
    /// allowed: reporting MOVE would make Explorer delete the source files after the drop. On macOS the key held during
    /// the drag narrows what the source allows (Finder: ⌘ = move only, ⌃ = link only, ⌥ = copy), and a drop whose effect
    /// is not allowed is refused, so the ⌘ and ⌃⌘ rings never received their files. There the ring takes the drop as a
    /// link or a move instead: it only reads the files, and Finder does not move or delete them for a drop on another app.
    /// </summary>
    internal static DragDropEffects AcceptedEffect(DragDropEffects allowed)
    {
        if (allowed.HasFlag(DragDropEffects.Copy) || !OperatingSystem.IsMacOS())
            return DragDropEffects.Copy;
        if (allowed.HasFlag(DragDropEffects.Link))
            return DragDropEffects.Link;
        if (allowed.HasFlag(DragDropEffects.Move))
            return DragDropEffects.Move;
        return DragDropEffects.Copy;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        DragSummary?.Invoke(_dragOverEvents, _lastDragHit, Donut.LitAmount(_lastDragHit.Index));
        Donut.ExternalDragLeft(_lastDragAt);
        DragLeft?.Invoke();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var hit = Donut.HitTest(e.GetPosition(Donut));
        DragSummary?.Invoke(_dragOverEvents, hit, Donut.LitAmount(hit.Index));
        Donut.CollapseGroup();
        Donut.SetExternalHighlight(-1);
        e.DragEffects = Donut.Converts(hit) ? AcceptedEffect(e.DragEffects) : DragDropEffects.None;
        Dropped?.Invoke(hit, LocalFiles(e));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Donut.HandleKey(e.Key, e.KeyModifiers))
        {
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Escape)
        {
            EscapePressed?.Invoke();
            e.Handled = true;
        }
    }

    public event Action? EscapePressed;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Hidden, however that happened: a group waiting to open doesn't open on the hidden donut.
        if (change.Property == IsVisibleProperty && !change.GetNewValue<bool>())
            Donut.CollapseGroup();
    }

    /// <summary>Local file paths in the drag data (folders and virtual items are ignored).</summary>
    private static IReadOnlyList<string> LocalFiles(DragEventArgs e) =>
        (e.DataTransfer.TryGetFiles() ?? [])
            .Select(item => item.TryGetLocalPath())
            .OfType<string>()
            .Where(File.Exists)
            .ToList();
}
