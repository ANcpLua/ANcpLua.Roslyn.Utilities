using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Meziantou.Framework;
using Meziantou.Framework.Threading;
using Microsoft.Deployment.DotNet.Releases;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Supported .NET SDK versions for testing.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>Each enum value corresponds to a major .NET SDK release</item>
///         <item>Used by <see cref="DotNetSdkHelpers" /> to download specific SDK versions</item>
///     </list>
/// </remarks>
/// <seealso cref="DotNetSdkHelpers" />
public enum NetSdkVersion
{
    /// <summary>
    ///     .NET 10.0 SDK.
    /// </summary>
    Net100
}

/// <summary>
///     Helper for downloading, extracting, and caching .NET SDK versions for integration testing.
/// </summary>
/// <remarks>
///     <para>
///         This class provides functionality to automatically download and cache .NET SDK installations
///         for use in integration tests. SDKs are downloaded from official Microsoft release channels
///         and cached in the user's local application data folder.
///     </para>
///     <list type="bullet">
///         <item>Downloads SDKs from official Microsoft .NET release channels</item>
///         <item>Caches SDKs in <c>%LocalAppData%/ANcpLua/dotnet/{version}</c></item>
///         <item>Thread-safe with keyed async locking to prevent concurrent downloads of the same version</item>
///         <item>Automatically detects the current runtime identifier for cross-platform support</item>
///         <item>Handles both ZIP (Windows) and TAR.GZ (Linux/macOS) archive formats</item>
///         <item>Sets appropriate Unix file permissions on non-Windows platforms</item>
///     </list>
/// </remarks>
/// <seealso cref="NetSdkVersion" />
public static class DotNetSdkHelpers
{
    /// <summary>
    ///     Shared HTTP client for downloading SDK archives.
    /// </summary>
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    ///     Cache of resolved SDK paths indexed by version.
    /// </summary>
    private static readonly ConcurrentDictionary<NetSdkVersion, FullPath> Values = new();

    /// <summary>
    ///     Keyed async lock to prevent concurrent downloads of the same SDK version.
    /// </summary>
    private static readonly KeyedAsyncLock<NetSdkVersion> KeyedAsyncLock = new();

    /// <summary>
    ///     Gets the path to the dotnet executable for the specified SDK version.
    ///     Downloads and caches the SDK if not already present.
    /// </summary>
    /// <param name="version">The .NET SDK version to retrieve.</param>
    /// <returns>
    ///     The full path to the dotnet executable (<c>dotnet.exe</c> on Windows, <c>dotnet</c> on Unix).
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the following operations:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Checks the in-memory cache for a previously resolved path</item>
    ///         <item>Checks if the SDK already exists on disk in the cache directory</item>
    ///         <item>If not cached, downloads the latest SDK release for the specified major version</item>
    ///         <item>Extracts the SDK to a temporary directory, then moves it to the cache location</item>
    ///         <item>On Unix platforms, sets executable permissions on <c>dotnet</c> and <c>csc</c> binaries</item>
    ///     </list>
    ///     <para>
    ///         The SDK is downloaded from the official Microsoft .NET release channels using
    ///         <see cref="ProductCollection" />. The runtime identifier is automatically detected
    ///         to download the appropriate platform-specific archive.
    ///     </para>
    ///     <para>
    ///         Cache location: <c>{LocalApplicationData}/ANcpLua/dotnet/{sdkVersion}</c>
    ///     </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the specified <paramref name="version" /> is not supported.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the SDK download or extraction fails and the dotnet executable is not found.
    /// </exception>
    /// <seealso cref="NetSdkVersion" />
    /// <seealso cref="ClearCache" />
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
    ///     Clears the in-memory cache of SDK paths.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>Clears only the in-memory path cache, not the downloaded SDKs on disk</item>
    ///         <item>Subsequent calls to <see cref="Get" /> will re-check the disk cache</item>
    ///         <item>Useful for testing scenarios where cache behavior needs to be verified</item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="Get" />
    public static void ClearCache()
    {
        Values.Clear();
    }
}