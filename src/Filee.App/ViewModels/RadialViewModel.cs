// State of the live donut toolbar: which files are being converted and which presets are offered.

using CommunityToolkit.Mvvm.ComponentModel;
using Filee.App.Controls;
using Filee.App.Services;
using Filee.Core.Formats;
using Filee.Core.Localization;
using Filee.Core.Presets;
using Filee.Core.Profiles;
using Filee.Core.Settings;

namespace Filee.App.ViewModels;

public sealed partial class RadialViewModel(UserDataStore store, ILocalizer loc, PresetAvailability availability) : ObservableObject
{
    /// <summary>Id of the "Original" slice: it converts nothing, and pausing on it sends the files as they are.</summary>
    public const string OriginalId = "oneon.original";
    [ObservableProperty] private IReadOnlyList<DonutItem> _items = [];

    /// <summary>
    /// Transfer zone slices (Settings → Transfers), shown beyond any enabled slice of the first ring, on the conversion
    /// and the tools wheel.
    /// </summary>
    [ObservableProperty] private IReadOnlyList<DonutItem>? _transferItems;
    [ObservableProperty] private string _centerTitle = "";
    [ObservableProperty] private string? _centerSubtitle;
    [ObservableProperty] private double _outerRadius = 150;
    [ObservableProperty] private double _holeRatio = 0.42;
    [ObservableProperty] private double _sliceOpacity = 0.96;

    /// <summary>
    /// The window keeps room for the ring a group fans out. Dragging, the donut is placed before the files (and so
    /// the donut) are known: room when any donut has a group. In click mode only when this one does
    /// (<see cref="FitToThisDonut"/>), so other donuts stay as small, and as close to the cursor at a screen edge, as
    /// without groups.
    /// </summary>
    [ObservableProperty] private bool _reserveSubRing;

    /// <summary>
    /// 0 while a drag gesture waits for files, 1 otherwise. The gesture starts before anyone knows what is being
    /// dragged (a file, text, or nothing: Ctrl + a rubber-band selection), so the donut only appears when files enter.
    /// </summary>
    [ObservableProperty] private double _donutOpacity = 1;

    /// <summary>
    /// Background of the window while the donut is hidden: practically invisible, but not fully transparent, so
    /// Windows still delivers the drag to this window and the files can arrive.
    /// </summary>
    [ObservableProperty] private string _hitBackground = "Transparent";

    /// <summary>Files the donut will convert (empty while waiting for a drag to enter).</summary>
    public IReadOnlyList<string> Files { get; private set; } = [];

    /// <summary>Presets in slice order.</summary>
    public IReadOnlyList<Preset> Presets { get; private set; } = [];

    /// <summary>For each slice, the presets its group fans out (empty for an ordinary preset).</summary>
    private IReadOnlyList<IReadOnlyList<Preset>> _subPresets = [];
    private WheelMode _wheel = WheelMode.Convert;
    public WheelMode Wheel => _wheel;

    /// <summary>
    /// True when the conversion wheel starts with the "Original" slice (there is somewhere to send to). Slice 0 is then
    /// that slice and slice <c>i</c> shows preset <c>i - 1</c>.
    /// </summary>
    private bool _hasOriginal;

    /// <summary>True when slice <paramref name="index"/> is the "Original" slice (send the files without converting).</summary>
    public bool IsOriginal(int index) => _hasOriginal && index == 0;

    /// <summary>The position in <see cref="Presets"/> of slice <paramref name="index"/>.</summary>
    private int PresetIndexOf(int index) => _hasOriginal ? index - 1 : index;

    /// <summary>
    /// The preset that slice <paramref name="index"/> converts with, or slice <paramref name="sub"/> of its group's ring;
    /// null when there is none or it can't convert these files.
    /// </summary>
    public Preset? PresetAt(int index, int sub)
    {
        var preset = PresetIndexOf(index);
        if (index < 0 || index >= Items.Count || preset < 0 || preset >= Presets.Count)
            return null; // also the "Original" slice: it converts nothing
        if (sub < 0)
            return Items[index] is { IsEnabled: true, IsGroup: false } ? Presets[preset] : null;
        return Items[index].Children is { } children && sub < children.Count && children[sub].IsEnabled
            ? _subPresets[preset][sub]
            : null;
    }

    /// <summary>The preset slice <paramref name="index"/> shows, or slice <paramref name="sub"/> of its group's ring (to edit it).</summary>
    public Preset? PresetShownAt(int index, int sub)
    {
        var preset = PresetIndexOf(index);
        return preset < 0 || preset >= Presets.Count ? null
            : sub < 0 ? Presets[preset]
            : sub < _subPresets[preset].Count ? _subPresets[preset][sub] : null;
    }

    /// <summary>True when opened with known files (keyboard, context menu, drop zone): slices are clicked.</summary>
    public bool IsClickMode { get; private set; }

    public bool HasFiles => Files.Count > 0;

    /// <summary>Drag mode before any file entered: an empty ring saying "Bring files here".</summary>
    public void ResetForDrag(WheelMode wheel = WheelMode.Convert)
    {
        _wheel = wheel;
        _hasOriginal = false;
        IsClickMode = false;
        Files = [];
        Presets = [];
        _subPresets = [];
        ApplyAppearance();
        ReserveSubRing = store.Profiles.Any(profile => profile.PresetIds.Any(id => store.FindPreset(id)?.IsGroup == true));
        Items = [];
        CenterTitle = loc["donut.center_drag"];
        CenterSubtitle = null;
        DonutOpacity = 0;
        HitBackground = "#01000000";
    }

    /// <summary>Fills the donut for the given files.</summary>
    public void Load(IReadOnlyList<string> files, bool clickMode, WheelMode wheel = WheelMode.Convert)
    {
        _wheel = wheel;
        _hasOriginal = false;
        IsClickMode = clickMode;
        Files = files;
        DonutOpacity = 1;
        HitBackground = "Transparent";
        ApplyAppearance();

        var profile = ProfileSelector.Select(store.Profiles, files);
        var formats = ProfileSelector.DetectFormats(files);
        // Any slice of the first ring fans out the transfer ring: what it makes is sent on. (The retired AirDrop wheel
        // of 1.13.0 has no slices of its own any more; AirDrop is a place on this ring.)
        TransferItems = _wheel is WheelMode.Convert or WheelMode.Tools ? LoadTransfers() : null;
        var toolIds = BuiltInData.ToolPresetIds.ToHashSet();
        var presetIds = _wheel == WheelMode.Tools ? BuiltInData.ToolPresetIds : (profile?.PresetIds ?? []).Where(id => !toolIds.Contains(id));
        Presets = presetIds
            .Select(store.FindPreset)
            .OfType<Preset>()
            .Where(p => _wheel != WheelMode.Tools || (p.IsGroup
                ? p.SubPresetIds.Select(store.FindPreset).OfType<Preset>().Any(c => availability.Check(c, formats) is null)
                : availability.Check(p, formats) is null))
            .ToList();

        _subPresets = Presets
            .Select(p => (IReadOnlyList<Preset>)(p.IsGroup ? p.SubPresetIds : [])
                .Select(store.FindPreset).OfType<Preset>().Where(c => !c.IsGroup).Take(Preset.MaxSubPresets).ToList())
            .ToList();

        DonutItem ItemOf(Preset p, bool caption = true)
        {
            var reason = availability.Check(p, formats);
            return new DonutItem
            {
                Id = p.Id,
                // On a group's ring the group's name gives the context: "Rotate" → Left / Right / 180°.
                Label = caption ? loc.DisplayName(p) : loc.RingName(p),
                Caption = !caption ? null
                    : p.TargetFormat == BuiltInData.SameAsSource
                        ? string.Join("/", formats.Select(f => f.DisplayName).Take(2))
                        : FormatLabels.NameOf(loc, p.TargetFormat),
                IsEnabled = reason is null,
                DisabledReason = reason,
            };
        }

        Items = Presets.Select((p, i) =>
        {
            if (!p.IsGroup)
                return ItemOf(p);
            // A group: its own name and size, and its presets on the ring it fans out (labels only, they're small).
            var children = _subPresets[i].Select(c => ItemOf(c, caption: false)).ToList();
            return new DonutItem
            {
                Id = p.Id,
                Label = loc.DisplayName(p),
                Caption = loc.Format("donut.group_caption", children.Count),
                IsEnabled = children.Any(c => c.IsEnabled),
                // Its presets were all deleted since it was made: say so rather than show a dead slice.
                DisabledReason = children.Count == 0 ? loc["donut.group_empty"]
                    : children.Select(c => c.DisabledReason).FirstOrDefault(r => r is not null),
                Children = children,
            };
        }).ToList();

        // The conversion wheel starts with "Original": pause on it and the transfer ring sends the files unconverted.
        // Dropped on the slice itself it does nothing (a copy next to the original would be pointless).
        if (_wheel == WheelMode.Convert && TransferItems is { Count: > 0 } && files.Count > 0)
        {
            Items = [new DonutItem { Id = OriginalId, Label = loc["donut.original"], Caption = loc["donut.original_caption"] }, .. Items];
            _hasOriginal = true;
        }

        CenterTitle = formats.Count == 1
            ? loc.Format("donut.files_same", files.Count, formats[0].DisplayName)
            : loc.Format("donut.files_mixed", files.Count);
        CenterSubtitle = clickMode ? loc["donut.close_hint"] : null;
    }

    /// <summary>Updates the centre hint for the emphasised slice (and slice <paramref name="sub"/> of its group's ring).</summary>
    public void ShowHint(int index, int sub, bool centerHot)
    {
        if (centerHot)
            CenterSubtitle = loc["donut.cancel"];
        else if (IsOriginal(index) && sub < 0)
            CenterSubtitle = loc["donut.original_hint"];
        else if (index >= 0 && index < Items.Count && !Items[index].IsGroup && sub >= 0 &&
                 TransferItems is { } transfers && sub < transfers.Count)
            CenterSubtitle = loc.Format("donut.send_to", Items[index].Label, transfers[sub].Label);
        else if (index >= 0 && index < Items.Count && Items[index].Children is { Count: > 0 } children &&
                 (sub >= 0 || Items[index].IsEnabled))
            CenterSubtitle = sub >= 0 && sub < children.Count
                ? children[sub].IsEnabled
                    ? loc.Format(IsClickMode ? "donut.click_to" : "donut.drop_to", $"{Items[index].Label} · {children[sub].Label}")
                    : children[sub].DisabledReason
                : loc["donut.pick_in_group"];
        else if (index >= 0 && index < Items.Count)
            CenterSubtitle = Items[index].IsEnabled
                ? loc.Format(IsClickMode ? "donut.click_to" : "donut.drop_to", Items[index].Label)
                : Items[index].DisabledReason;
        else
            CenterSubtitle = IsClickMode ? loc["donut.close_hint"] : null;
    }

    /// <summary>Click mode, before the window is placed: room for a group's ring only if this donut has a group.</summary>
    public void FitToThisDonut() => ReserveSubRing = Items.Any(item => item.IsGroup) || TransferItems is { Count: > 0 };

    /// <summary>The enabled transfer targets as donut slices, in order; "Google Drive" is greyed when it isn't set up.</summary>
    private IReadOnlyList<DonutItem>? LoadTransfers()
    {
        var targets = store.Settings.Transfers.Where(t => t.Enabled).ToList();
        if (targets.Count == 0)
            return null;
        return targets.Select(t =>
        {
            var missing = t.Kind == TransferKind.GoogleDrive && Filee.Core.Transfer.TransferFiles.FindMyDrive() is null;
            return new DonutItem
            {
                Id = t.Id,
                Label = !string.IsNullOrWhiteSpace(t.Name) ? t.Name : loc[$"transfer.kind.{t.Kind}"],
                IsEnabled = !missing,
                DisabledReason = missing ? loc["transfer.no_drive"] : null,
            };
        }).ToList();
    }

    /// <summary>The transfer target behind slice <paramref name="sub"/> of the transfer zone, or null.</summary>
    public TransferTarget? TransferAt(int sub)
    {
        var targets = store.Settings.Transfers.Where(t => t.Enabled).ToList();
        return sub >= 0 && sub < targets.Count ? targets[sub] : null;
    }

    private void ApplyAppearance()
    {
        OuterRadius = Math.Clamp(store.Settings.Donut.OuterRadius, 100, 240);
        HoleRatio = store.Settings.Donut.HoleRatio;
        SliceOpacity = store.Settings.Donut.Opacity;
    }
}
