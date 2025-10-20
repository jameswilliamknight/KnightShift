namespace KnightShift.Models;

/// <summary>
/// Represents a file or folder in the file system
/// </summary>
public class FileSystemEntry
{
    /// <summary>
    /// Full path to the file or folder
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Name of the file or folder (without path)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this is a directory
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Size in bytes (0 for directories)
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Last modified time
    /// </summary>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// Display string with appropriate icon
    /// </summary>
    public string DisplayString =>
        (IsDirectory ? "📁 " : "📄 ") + Name;

    /// <summary>
    /// Formatted size string
    /// </summary>
    public string FormattedSize => IsDirectory
        ? "<DIR>"
        : FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
