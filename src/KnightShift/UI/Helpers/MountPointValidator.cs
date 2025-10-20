namespace KnightShift.UI.Helpers;

/// <summary>
/// Validates mount point paths for safety and correctness
/// </summary>
public static class MountPointValidator
{
    private static readonly string[] DangerousPaths =
    {
        "/", "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64",
        "/proc", "/root", "/run", "/sbin", "/sys", "/usr", "/var"
    };

    /// <summary>
    /// Validates a mount point path and returns whether it's safe to use
    /// </summary>
    public static (bool IsValid, string ErrorMessage) Validate(string path)
    {
        // Check if empty
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "Path cannot be empty.");
        }

        // Check if absolute path
        if (!path.StartsWith("/"))
        {
            return (false, "Path must be absolute (start with /).");
        }

        var normalizedPath = path.TrimEnd('/');

        // Check for dangerous system directories
        foreach (var dangerous in DangerousPaths)
        {
            if (normalizedPath.Equals(dangerous, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"⚠️  DANGER: Cannot mount to system directory '{dangerous}'. This could damage your system!");
            }
        }

        // Warn about /home (not forbidden but risky)
        if (normalizedPath.Equals("/home", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("/home/", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"⚠️  WARNING: Mounting to /home is not recommended. Use /mnt/ or /media/ instead.");
        }

        // Check for invalid characters
        if (path.Contains("//") || path.Contains(".."))
        {
            return (false, "Path contains invalid sequences (//, ..).");
        }

        // Check if directory exists and is non-empty
        if (Directory.Exists(path))
        {
            try
            {
                var entries = Directory.EnumerateFileSystemEntries(path);
                if (entries.Any())
                {
                    return (false, $"Directory '{path}' already exists and is not empty. Mount point must be empty.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                return (false, $"Cannot access directory '{path}' to verify it's empty. Permission denied.");
            }
        }

        return (true, string.Empty);
    }
}
