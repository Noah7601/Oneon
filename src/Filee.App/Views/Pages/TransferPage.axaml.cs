using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Filee.App.ViewModels.Pages;

namespace Filee.App.Views.Pages;

public partial class TransferPage : UserControl
{
    public TransferPage() => InitializeComponent();

    private async void OnAddFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TransferPageViewModel page && await PickFolderAsync() is { } path)
            page.AddFolder(path);
    }

    private async void OnBrowseFolder(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is TransferTargetViewModel target && await PickFolderAsync() is { } path)
            target.Path = path;
    }

    private async Task<string?> PickFolderAsync()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return null;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }
}
