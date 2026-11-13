namespace Filee.Core.Settings;

/// <summary>
/// The app's per-user folder name. The app was called "theOdrop" before it became "Oneon": the first time a
/// folder is asked for, an existing "theOdrop" folder is moved to "Oneon", so settings, presets, history and
/// downloaded engines carry over. If the move fails (e.g. a file is in use), the old folder keeps being used.
/// </summary>
public static class AppFolder
{
    public const string Name = "Oneon";
    public const string LegacyName = "theOdrop";

    /// <summary>
    /// Returns <c>parent/Oneon</c>, moving <c>parent/theOdrop</c> there first when only the old one exists.
    /// </summary>
    public static string In(string parent)
    {
        var current = Path.Combine(parent, Name);
        var legacy = Path.Combine(parent, LegacyName);
        try
        {
            if (!Directory.Exists(current) && Directory.Exists(legacy))
                Directory.Move(legacy, current);
        }
        catch (Exception)
        {
            if (!Directory.Exists(current) && Directory.Exists(legacy))
                return legacy;
        }
        return current;
    }

    /// <summary>Shortcut for a <see cref="Environment.SpecialFolder"/> parent.</summary>
    public static string In(Environment.SpecialFolder parent) => In(Environment.GetFolderPath(parent));
}
