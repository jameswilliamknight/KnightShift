using System.Text.RegularExpressions;

namespace KnightShift.Services.Helpers;

/// <summary>
/// Helper for WSL environment detection and Windows drive mount detection
/// </summary>
public static class WslEnvironment
{
    /// <summary>
    /// Checks if the application is running in WSL
    /// </summary>
    public static async Task<bool> IsRunningInWslAsync(ICommandLineProvider commandLine)
    {
        try
        {
            // Check for WSL-specific markers in /proc/version
            if (File.Exists("/proc/version"))
            {
                var version = await File.ReadAllTextAsync("/proc/version");
                return version.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                       version.Contains("WSL", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if PowerShell is available and executable (needed for Windows drive detection)
    /// </summary>
    public static async Task<bool> IsPowerShellAvailableAsync(ICommandLineProvider commandLine)
    {
        try
        {
            // Try to actually execute PowerShell with a simple command
            // This is more reliable than just checking if the file exists
            var result = await commandLine.ExecuteAsync("powershell.exe", "-NoProfile -NonInteractive -Command \"exit 0\"");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a set of mounted Windows drive letters (lowercase)
    /// </summary>
    public static async Task<HashSet<string>> GetMountedWindowsDrivesAsync(ICommandLineProvider commandLine)
    {
        var mounted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var mountOutput = await commandLine.ExecuteAndGetOutputAsync("mount", "");

            // Look for drvfs/9p mounts (Windows drives in WSL)
            // Example WSL1: C: on /mnt/c type drvfs (rw,noatime,uid=1000,gid=1000,case=off)
            // Example WSL2: C:\ on /mnt/c type 9p (rw,noatime,aname=drvfs;path=C:\;...)
            var driveRegex = new Regex(@"^([A-Z]):\\?\s+on\s+/mnt/([a-z])\s+type\s+(drvfs|9p)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var matches = driveRegex.Matches(mountOutput);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 2)
                {
                    mounted.Add(match.Groups[2].Value.ToLower());
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting mounted drives: {ex.Message}");
        }

        return mounted;
    }

    /// <summary>
    /// Gets a dictionary of mounted Windows drives with their mount paths
    /// </summary>
    public static async Task<Dictionary<string, string>> GetMountedWindowsDrivesWithPathsAsync(ICommandLineProvider commandLine)
    {
        var mounted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var mountOutput = await commandLine.ExecuteAndGetOutputAsync("mount", "");

            // Look for drvfs/9p mounts (Windows drives in WSL)
            // Example WSL1: C: on /mnt/c type drvfs (rw,noatime,uid=1000,gid=1000,case=off)
            // Example WSL2: C:\ on /mnt/c type 9p (rw,noatime,aname=drvfs;path=C:\;...)
            var driveRegex = new Regex(@"^([A-Z]):\\?\s+on\s+(/[^\s]+)\s+type\s+(drvfs|9p)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var matches = driveRegex.Matches(mountOutput);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 3)
                {
                    var driveLetter = match.Groups[1].Value.ToLower();
                    var mountPath = match.Groups[2].Value;
                    mounted[driveLetter] = mountPath;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting mounted drives: {ex.Message}");
        }

        return mounted;
    }
}
