using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Meziantou.Framework;
using Meziantou.Framework.Threading;
using Microsoft.Deployment.DotNet.Releases;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// Supported .NET SDK versions for testing.
/// </summary>
public enum NetSdkVersion
{
    /// <summary>.NET 10.0 SDK</summary>
    Net100
}

/// <summary>
/// Helper for downloading and caching .NET SDK versions for integration testing.
/// </summary>
public static class DotNetSdkHelpers
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ConcurrentDictionary<NetSdkVersion, FullPath> Values = new();
    private static readonly KeyedAsyncLock<NetSdkVersion> KeyedAsyncLock = new();

    /// <summary>
    /// Gets the path to the dotnet executable for the specified SDK version.
    /// Downloads and caches the SDK if not already present.
    /// </summary>
    public static async Task<FullPath> Get(NetSdkVersion version)
    {
        if (Values.TryGetValue(version, out var result))
            return result;

        using (await KeyedAsyncLock.LockAsync(version))
        {
            if (Values.TryGetValue(version, out result))
                return result;

            var versionString = version switch
            {
                NetSdkVersion.Net100 => "10.0",
                _ => throw new NotSupportedException($"SDK version {version} is not supported")
            };

            var products = await ProductCollection.GetAsync();
            var product = products.Single(a => a.ProductName == ".NET" && a.ProductVersion == versionString);
            var releases = await product.GetReleasesAsync();
            var latestRelease = releases.Single(r => r.Version == product.LatestReleaseVersion);
            var latestSdk = latestRelease.Sdks.MaxBy(static sdk => sdk.Version);

            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            var file = latestSdk!.Files.Single(file =>
                file.Rid == runtimeIdentifier && Path.GetExtension(file.Name) is ".zip" or ".gz");
            var finalFolderPath = FullPath.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "ANcpLua" /
                                  "dotnet" / latestSdk.Version.ToString();
            var finalDotnetPath = finalFolderPath / (OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(finalDotnetPath))
            {
                Values[version] = finalDotnetPath;
                return finalDotnetPath;
            }

            var tempFolder = FullPath.GetTempPath() / "dotnet" / Guid.NewGuid().ToString("N");

            var bytes = await HttpClient.GetByteArrayAsync(file.Address);
            if (Path.GetExtension(file.Name) is ".zip")
            {
                using var ms = new MemoryStream(bytes);
                var zip = new ZipArchive(ms);
                await zip.ExtractToDirectoryAsync(tempFolder, true);
            }
            else
            {
                using var ms = new MemoryStream(bytes);
                await using var gz = new GZipStream(ms, CompressionMode.Decompress);
                await using var tar = new TarReader(gz);
                while (await tar.GetNextEntryAsync() is { } entry)
                {
                    var destinationPath = tempFolder / entry.Name;
                    switch (entry.EntryType)
                    {
                        case TarEntryType.Directory:
                            Directory.CreateDirectory(destinationPath);
                            break;
                        case TarEntryType.RegularFile:
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            var entryStream = entry.DataStream;
                            await using var outputStream = File.Create(destinationPath);
                            if (entryStream is not null) await entryStream.CopyToAsync(outputStream);
                            break;
                        }
                    }
                }
            }

            if (!OperatingSystem.IsWindows())
            {
                var tempDotnetPath = tempFolder / "dotnet";

                Console.WriteLine("Updating permissions of " + tempDotnetPath);
                File.SetUnixFileMode(tempDotnetPath,
                    UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.GroupRead |
                    UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

                foreach (var cscPath in Directory.GetFiles(tempFolder, "csc", SearchOption.AllDirectories))
                {
                    Console.WriteLine("Updating permissions of " + cscPath);
                    File.SetUnixFileMode(cscPath,
                        UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.GroupRead |
                        UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
            }

            finalFolderPath.CreateParentDirectory();
            Directory.Move(tempFolder, finalFolderPath);

            if (!File.Exists(finalDotnetPath))
                throw new InvalidOperationException($"SDK download failed. Expected dotnet at: {finalDotnetPath}");

            Values[version] = finalDotnetPath;
            return finalDotnetPath;
        }
    }

    /// <summary>
    /// Clears the cached SDK paths. Useful for testing.
    /// </summary>
    public static void ClearCache()
    {
        Values.Clear();
    }
}
