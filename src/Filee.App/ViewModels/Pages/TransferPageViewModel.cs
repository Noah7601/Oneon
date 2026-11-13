// Transfer page: the slices of the transfer ring (where the donut's outer ring sends files).
// Every change is saved at once; the ring reads the settings fresh on every open.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.App.Services;
using Filee.Core.Localization;
using Filee.Core.Settings;

namespace Filee.App.ViewModels.Pages;

public sealed partial class TransferPageViewModel : ObservableObject
{
    private readonly UserDataStore _store;
    private readonly ILocalizer _loc;

    public TransferPageViewModel(UserDataStore store, ILocalizer loc)
    {
        _store = store;
        _loc = loc;
        foreach (var target in store.Settings.Transfers)
            Targets.Add(new TransferTargetViewModel(target, this));
    }

    public ObservableCollection<TransferTargetViewModel> Targets { get; } = [];

    /// <summary>Adds a folder slice; the view asked for the folder first.</summary>
    public void AddFolder(string path)
    {
        var target = new TransferTarget { Kind = TransferKind.Folder, Path = path };
        _store.Settings.Transfers.Add(target);
        Targets.Add(new TransferTargetViewModel(target, this));
        Save();
    }

    internal void Remove(TransferTargetViewModel item)
    {
        _store.Settings.Transfers.Remove(item.Target);
        Targets.Remove(item);
        Save();
    }

    /// <summary>Moves a slice up (-1) or down (+1) in the ring.</summary>
    internal void Move(TransferTargetViewModel item, int delta)
    {
        var index = Targets.IndexOf(item);
        var to = index + delta;
        if (index < 0 || to < 0 || to >= Targets.Count)
            return;
        Targets.Move(index, to);
        _store.Settings.Transfers.RemoveAt(index);
        _store.Settings.Transfers.Insert(to, item.Target);
        Save();
    }

    internal void Save() => _store.SaveSettings();

    internal string KindLabel(TransferKind kind) => _loc[$"transfer.kind.{kind}"];
}

/// <summary>One slice on the page; edits go straight into the settings' <see cref="TransferTarget"/>.</summary>
public sealed partial class TransferTargetViewModel : ObservableObject
{
    private readonly TransferPageViewModel _page;

    public TransferTargetViewModel(TransferTarget target, TransferPageViewModel page)
    {
        Target = target;
        _page = page;
        _enabled = target.Enabled;
        _confirm = target.Confirm;
        _name = target.Name;
        _path = target.Path ?? "";
    }

    public TransferTarget Target { get; }
    public string KindLabel => _page.KindLabel(Target.Kind);
    public bool IsFolder => Target.Kind == TransferKind.Folder;
    public bool IsGoogleDrive => Target.Kind == TransferKind.GoogleDrive;

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private bool _confirm;
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _path;

    partial void OnEnabledChanged(bool value) => Change(t => t.Enabled = value);
    partial void OnConfirmChanged(bool value) => Change(t => t.Confirm = value);
    partial void OnNameChanged(string value) => Change(t => t.Name = value.Trim());
    partial void OnPathChanged(string value) => Change(t => t.Path = value.Trim());

    [RelayCommand] private void MoveUp() => _page.Move(this, -1);
    [RelayCommand] private void MoveDown() => _page.Move(this, +1);
    [RelayCommand] private void Remove() => _page.Remove(this);

    private void Change(Action<TransferTarget> change)
    {
        change(Target);
        _page.Save();
    }
}
