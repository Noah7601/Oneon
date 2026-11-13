// File side of the transfer zone: finding the Google Drive folder and copying files into a folder.
// No UI and no platform calls here, so it can be unit tested and used by the CLI later.

using Filee.Core.Settings;

namespace Filee.Core.Transfer;

public static class TransferFiles
{
    /// <summary>Sub-folder used inside My Drive when the target names none.</summary>
    public const string DefaultDriveFolder = "Oneon";

    /// <summary>Names Google Drive for desktop gives "My Drive" in different languages.</summary>
    private static readonly string[] MyDriveNames = ["My Drive", "내 드라이브", "我的云端硬盘", "マイドライブ"];

    /// <summary>
    /// The local "My Drive" folder of Google Drive for desktop, or null when it is not installed or signed in.
    /// macOS: ~/Library/CloudStorage/GoogleDrive-&lt;account&gt;/My Drive (the first account found).
    /// Windows: a mounted drive letter (usually G:) holding My Drive.
    /// </summary>
    public static string? FindMyDrive()
    {
        if (OperatingSystem.IsMacOS())
        {
            var cloud = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "CloudStorage");
            if (!Directory.Exists(cloud))
                return null;
            foreach (var account in Directory.EnumerateDirectories(cloud, "GoogleDrive-*").Order())
                foreach (var name in MyDriveNames)
                {
                    var myDrive = System.IO.Path.Combine(account, name);
                    if (Directory.Exists(myDrive))
                        return myDrive;
                }
            return null;
        }
        if (OperatingSystem.IsWindows())
        {
            foreach (var drive in DriveInfo.GetDrives())
                foreach (var name in MyDriveNames)
                {
                    var myDrive = System.IO.Path.Combine(drive.RootDirectory.FullName, name);
                    if (Directory.Exists(myDrive))
                        return myDrive;
                }
        }
        return null;
    }

    /// <summary>The folder a folder-like target copies into, or null when it cannot be found.</summary>
    public static string? FolderOf(TransferTarget target) => target.Kind switch
    {
        TransferKind.Folder => string.IsNullOrWhiteSpace(target.Path) ? null : target.Path,
        TransferKind.GoogleDrive => FindMyDrive() is { } myDrive
            ? System.IO.Path.Combine(myDrive, string.IsNullOrWhiteSpace(target.Path) ? DefaultDriveFolder : target.Path)
            : null,
        _ => null,
    };

    /// <summary>
    /// Copies <paramref name="files"/> into <paramref name="folder"/> (created if missing). An existing name gets
    /// " (2)", " (3)"… instead of being overwritten. Returns the new paths.
    /// </summary>
    public static IReadOnlyList<string> CopyInto(string folder, IEnumerable<string> files)
    {
        Directory.CreateDirectory(folder);
        var copied = new List<string>();
        foreach (var source in files.Where(File.Exists))
        {
            var target = UniquePath(folder, System.IO.Path.GetFileName(source));
            File.Copy(source, target);
            copied.Add(target);
        }
        return copied;
    }

    private static string UniquePath(string folder, string fileName)
    {
        var path = System.IO.Path.Combine(folder, fileName);
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var extension = System.IO.Path.GetExtension(fileName);
        for (var n = 2; File.Exists(path) || Directory.Exists(path); n++)
            path = System.IO.Path.Combine(folder, $"{stem} ({n}){extension}");
        return path;
    }
}
