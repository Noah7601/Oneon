// Runs one slice of the transfer ring: asks first (when the target wants it), then copies the files to a folder or
// Google Drive, puts them on the clipboard, or opens AirDrop. Never moves or deletes anything.
//
// The donut calls SendAsync with the converted outputs (convert → send) or with the dropped files themselves
// (centre → transfer ring, no conversion).

using Avalonia.Controls;
using Filee.Core.Localization;
using Filee.Core.Platform;
using Filee.Core.Settings;
using Filee.Core.Transfer;
using Microsoft.Extensions.Logging;

namespace Filee.App.Services;

public sealed class TransferService(
    UserDataStore store,
    ConversionService conversions,
    WindowService windows,
    IPlatformServices platform,
    ILocalizer loc,
    ILogger<TransferService> log)
{
    /// <summary>The enabled slices of the transfer ring, in order.</summary>
    public IReadOnlyList<TransferTarget> Targets => store.Settings.Transfers.Where(t => t.Enabled).ToList();

    /// <summary>Label of a slice: its own name, else the kind's.</summary>
    public string NameOf(TransferTarget target) =>
        !string.IsNullOrWhiteSpace(target.Name) ? target.Name : loc[$"transfer.kind.{target.Kind}"];

    /// <summary>
    /// Convert → send: waits for <paramref name="job"/> to finish, then sends whatever it produced. A job that made
    /// nothing (failed or cancelled) sends nothing; its own error is already shown by the toast.
    /// </summary>
    public async Task<bool> SendAfterAsync(ViewModels.JobViewModel job, TransferTarget target, Window? owner = null)
    {
        var finished = await conversions.WhenFinishedAsync(job);
        var outputs = finished.Outputs.Where(File.Exists).ToList();
        if (outputs.Count == 0)
            return false;
        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => SendAsync(target, outputs, owner));
    }

    /// <summary>
    /// Sends <paramref name="files"/> to <paramref name="target"/>. Returns true when they were sent (or the
    /// AirDrop sheet opened); false when the user cancelled or it failed (the user has been told why).
    /// </summary>
    public async Task<bool> SendAsync(TransferTarget target, IReadOnlyList<string> files, Window? owner = null)
    {
        var existing = files.Where(File.Exists).ToList();
        if (existing.Count == 0)
        {
            await windows.MessageAsync(owner, loc["transfer.nothing"]);
            return false;
        }

        // Find the folder before asking, so the question can't be answered "OK" for a place that isn't there.
        string? folder = null;
        if (target.Kind is TransferKind.Folder or TransferKind.GoogleDrive)
        {
            folder = TransferFiles.FolderOf(target);
            if (folder is null)
            {
                await windows.MessageAsync(owner,
                    loc[target.Kind == TransferKind.GoogleDrive ? "transfer.no_drive" : "transfer.no_folder"]);
                return false;
            }
        }

        if (target.Confirm && !await windows.AskAsync(owner, loc.Format("transfer.confirm", existing.Count, NameOf(target))))
            return false;

        try
        {
            switch (target.Kind)
            {
                case TransferKind.Folder:
                case TransferKind.GoogleDrive:
                    var copied = TransferFiles.CopyInto(folder!, existing);
                    log.LogInformation("Copied {Count} file(s) to {Folder}", copied.Count, folder);
                    return true;
                case TransferKind.Clipboard:
                    if (platform.CopyFilesToClipboard(existing))
                        return true;
                    break;
                case TransferKind.AirDrop:
                    if (platform.ShareFilesViaAirDrop(existing))
                        return true;
                    break;
            }
            await windows.MessageAsync(owner, loc["transfer.unsupported"]);
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            log.LogWarning(ex, "Transfer to {Kind} failed", target.Kind);
            await windows.MessageAsync(owner, loc.Format("transfer.failed", ex.Message));
            return false;
        }
    }
}
