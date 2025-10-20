namespace KnightShift.Services.Helpers;

/// <summary>
/// Utility for formatting byte sizes into human-readable strings
/// </summary>
public static class ByteFormatter
{
    /// <summary>
    /// Formats bytes into human-readable format (B, KB, MB, GB, TB)
    /// </summary>
    public static string Format(long bytes)
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
