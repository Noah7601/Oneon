// Installs the downloadable engines the way the app's Engines page does and runs each once. Downloads hundreds of MB,
// so it runs only when THEODROP_INSTALL_ENGINES=1 (the macOS release build sets it).
using Filee.Engines.Infrastructure;

namespace Filee.Engines.Tests;

public sealed class EngineInstallTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("THEODROP_INSTALL_ENGINES") == "1";

    public static TheoryData<string> DownloadablePackages()
    {
        var data = new TheoryData<string>();
        foreach (var package in EngineDownloads.Packages.Where(EngineDownloads.CanDownload))
            data.Add(package.Id);
        return data;
    }

    [Theory]
    [MemberData(nameof(DownloadablePackages))]
    public async Task Downloadable_engine_installs_and_runs(string id)
    {
        Assert.SkipUnless(Enabled, "Set THEODROP_INSTALL_ENGINES=1 to download and install the engines.");
        var package = EngineDownloads.Packages.Single(p => p.Id == id);
        var root = Path.Combine(Path.GetTempPath(), "theodrop-engine-install", id);
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        var installer = new EngineInstaller(http, root);

        await installer.InstallAsync(package, progress: null, TestContext.Current.CancellationToken);

        Assert.True(installer.IsUpToDate(package), $"{id} is not marked as installed");
        var folder = EngineEnvironment.FolderWithPrograms(id, Path.Combine(root, id));
        Assert.True(folder is not null, $"{id}: its programs were not found under {Path.Combine(root, id)}");
        var program = Directory.EnumerateFiles(folder).First(f => Path.GetFileNameWithoutExtension(f) is
            "ebook-convert" or "pandoc" or "soffice" or "gs" or "gswin64c" or "ffmpeg");
        var arguments = id == "ghostscript" ? new[] { "--version" } : id == "ffmpeg" ? ["-version"] : ["--version"];
        var result = await ProcessRunner.RunAsync(program, arguments, TimeSpan.FromMinutes(3), TestContext.Current.CancellationToken);
        Assert.True(result.ExitCode == 0, $"{Path.GetFileName(program)} --version exited {result.ExitCode}: {result.StandardError}");
    }
}
