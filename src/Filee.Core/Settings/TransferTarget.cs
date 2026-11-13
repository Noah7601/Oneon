// Transfer zone: where the donut's outer ring sends files after (or instead of) converting them.
// Files are always COPIED, never moved: the original and the converted output stay where they were.

namespace Filee.Core.Settings;

/// <summary>What kind of place a transfer slice sends to.</summary>
public enum TransferKind
{
    /// <summary>A folder the user picked (an external drive, a NAS share, a project folder…).</summary>
    Folder,

    /// <summary>
    /// The Google Drive for desktop sync folder. Copying into it is the whole upload: the Drive app syncs it, so
    /// no sign-in or API is needed here. <see cref="TransferTarget.Path"/> is a sub-folder inside the drive.
    /// </summary>
    GoogleDrive,

    /// <summary>The files on the clipboard, ready to paste anywhere (⌘V / Ctrl+V).</summary>
    Clipboard,

    /// <summary>The system AirDrop sheet (macOS only).</summary>
    AirDrop,
}

/// <summary>One slice of the transfer ring.</summary>
public sealed class TransferTarget
{
    /// <summary>Stable id, so the order and settings survive renames.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public TransferKind Kind { get; set; }

    /// <summary>Label on the slice. Empty: a default name for the kind.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// <see cref="TransferKind.Folder"/>: the folder. <see cref="TransferKind.GoogleDrive"/>: a sub-folder inside
    /// My Drive (empty = <c>Oneon</c>). Unused for the other kinds.
    /// </summary>
    public string? Path { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Ask before sending (on by default: a transfer cannot be undone).</summary>
    public bool Confirm { get; set; } = true;

    /// <summary>The transfer ring of a fresh install.</summary>
    public static List<TransferTarget> Defaults() =>
    [
        new() { Kind = TransferKind.GoogleDrive },
        new() { Kind = TransferKind.Clipboard, Confirm = false }, // nothing leaves the computer
        .. OperatingSystem.IsMacOS() ? [new TransferTarget { Kind = TransferKind.AirDrop, Confirm = false }] : Array.Empty<TransferTarget>(),
    ];
}
