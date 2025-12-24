// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for file operations in source generators.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    ///     Writes content to a file only if the content has changed.
    ///     Avoids unnecessary file system writes and timestamp updates.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="newContent">The content to write.</param>
    /// <returns>True if the file was written, false if unchanged.</returns>
    public static bool WriteIfChanged(this string filePath, string newContent)
    {
        if (File.Exists(filePath))
        {
            var existingContent = File.ReadAllText(filePath);
            if (string.Equals(existingContent, newContent, StringComparison.Ordinal))
            {
                return false; // No change needed
            }
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, newContent);
        return true;
    }
}
