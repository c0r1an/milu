using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Milu.Web.Infrastructure.Updates;

public sealed class WindowsMiluUpdateInstaller(
    IHttpClientFactory clients,
    IWebHostEnvironment environment,
    IHostApplicationLifetime lifetime) : IMiluUpdateInstaller
{
    public async Task PrepareAndStartAsync(MiluReleaseInfo release, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Die automatische Installation ist derzeit für Windows verfügbar.");
        if (!release.UpdateAvailable) throw new InvalidOperationException("Es ist kein neueres Release verfügbar.");
        if (release.PackageUrl is null || release.ChecksumUrl is null)
            throw new InvalidOperationException("Das Release enthält kein vollständiges Milu-Installationspaket mit SHA-256-Prüfsumme.");
        ValidateGitHubUrl(release.PackageUrl); ValidateGitHubUrl(release.ChecksumUrl);
        var executable = Environment.ProcessPath;
        if (executable is null || Path.GetFileName(executable).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Automatische Updates sind nur in einer veröffentlichten Milu-Installation möglich, nicht während 'dotnet run'.");

        var version = release.LatestVersion!.Trim().TrimStart('v', 'V');
        var updateDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "updates", version);
        Directory.CreateDirectory(updateDirectory);
        var packagePath = Path.Combine(updateDirectory, "milu-win-x64.zip");
        var checksumPath = packagePath + ".sha256";
        var client = clients.CreateClient("MiluReleaseAssets");
        await DownloadAsync(client, release.PackageUrl, packagePath, cancellationToken);
        await DownloadAsync(client, release.ChecksumUrl, checksumPath, cancellationToken);
        var expected = (await File.ReadAllTextAsync(checksumPath, cancellationToken)).Trim().Split(' ')[0];
        await using var package = File.OpenRead(packagePath);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(package, cancellationToken));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Die SHA-256-Prüfsumme des Updatepakets ist ungültig.");

        var scriptPath = Path.Combine(updateDirectory, "install-update.ps1");
        await File.WriteAllTextAsync(scriptPath, UpdateScript, cancellationToken);
        var backupDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "update-backups", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        var restartArguments = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join('\0', Environment.GetCommandLineArgs().Skip(1))));
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ProcessId {Environment.ProcessId} -Package \"{packagePath}\" -Target \"{environment.ContentRootPath}\" -Executable \"{executable}\" -Backup \"{backupDirectory}\" -RestartArguments \"{restartArguments}\"";
        _ = Process.Start(new ProcessStartInfo("powershell.exe", arguments) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })
            ?? throw new InvalidOperationException("Der externe Updateprozess konnte nicht gestartet werden.");
        lifetime.StopApplication();
    }

    private static async Task DownloadAsync(HttpClient client, string url, string target, CancellationToken token)
    {
        await using var source = await client.GetStreamAsync(url, token);
        await using var destination = File.Create(target);
        await source.CopyToAsync(destination, token);
    }

    private static void ValidateGitHubUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Das Release-Asset stammt nicht von der erlaubten GitHub-Domain.");
    }

    private const string UpdateScript = """
param([int]$ProcessId,[string]$Package,[string]$Target,[string]$Executable,[string]$Backup,[string]$RestartArguments)
$ErrorActionPreference = 'Stop'
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
    Start-Sleep -Seconds 1
}
if (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) { throw 'Milu konnte nicht innerhalb von 60 Sekunden beendet werden.' }
$decodedArguments = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($RestartArguments))
$argumentList = if ($decodedArguments) { $decodedArguments.Split([char]0) } else { @() }
$extract = Join-Path ([System.IO.Path]::GetTempPath()) ('milu-update-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $extract -Force | Out-Null
New-Item -ItemType Directory -Path $Backup -Force | Out-Null
try {
    Expand-Archive -LiteralPath $Package -DestinationPath $extract -Force
    Get-ChildItem -LiteralPath $Target -Force | Where-Object { $_.Name -notin @('App_Data','update-backups') } | Copy-Item -Destination $Backup -Recurse -Force
    Get-ChildItem -LiteralPath $extract -Force | Copy-Item -Destination $Target -Recurse -Force
    Remove-Item -LiteralPath $extract -Recurse -Force
    Start-Process -FilePath $Executable -ArgumentList $argumentList -WorkingDirectory $Target -WindowStyle Hidden
} catch {
    Get-ChildItem -LiteralPath $Backup -Force | Copy-Item -Destination $Target -Recurse -Force
    Start-Process -FilePath $Executable -ArgumentList $argumentList -WorkingDirectory $Target -WindowStyle Hidden
    throw
}
""";
}
