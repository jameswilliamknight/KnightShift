using KnightShift.Models;
using KnightShift.Services.Helpers;

namespace KnightShift.Services;

/// <summary>
/// Service for detecting Windows drives from WSL
/// </summary>
public class WindowsDriveDetectionService
{
    private readonly ICommandLineProvider _commandLine;

    public WindowsDriveDetectionService(ICommandLineProvider commandLine)
    {
        _commandLine = commandLine;
    }

    /// <summary>
    /// Detects all Windows drives (both mounted and unmounted in WSL)
    /// </summary>
    public async Task<List<DetectedDrive>> GetAllWindowsDrivesAsync()
    {
        var drives = new List<DetectedDrive>();

        // Check if we're running in WSL
        if (!await WslEnvironment.IsRunningInWslAsync(_commandLine))
        {
            return drives;
        }

        // Check if PowerShell is available (it won't be when running under sudo)
        if (!await WslEnvironment.IsPowerShellAvailableAsync(_commandLine))
        {
            // PowerShell not available - can only detect already mounted drives
            return await GetMountedWindowsDrivesOnlyAsync();
        }

        try
        {
            // Get volumes from PowerShell
            var volumes = await GetWindowsVolumesAsync();

            // Get currently mounted Windows drives in WSL with their mount points
            var mountedDrives = await WslEnvironment.GetMountedWindowsDrivesWithPathsAsync(_commandLine);

            foreach (var volume in volumes)
            {
                // Skip drives with 0 bytes (empty drives, card readers, etc.)
                if (volume.Size == 0)
                {
                    continue;
                }

                var driveLetter = volume.DriveLetter.ToLower();
                var driveTypeLabel = WindowsVolumeParser.FormatDriveType(volume.DriveType);

                // Check if this drive is mounted
                var isMounted = mountedDrives.ContainsKey(driveLetter);
                var mountPoint = isMounted ? mountedDrives[driveLetter] : null;

                drives.Add(new DetectedDrive
                {
                    DevicePath = $"{volume.DriveLetter}:",
                    DeviceName = $"{volume.DriveLetter}:",
                    SizeBytes = volume.Size,
                    FormattedSize = ByteFormatter.Format(volume.Size),
                    FileSystemType = volume.FileSystemType,
                    Label = $"{driveTypeLabel} ({volume.DriveLetter}:)",
                    IsMounted = isMounted,
                    MountPoint = mountPoint,
                    IsWindowsDrive = true
                });
            }
        }
        catch (Exception ex)
        {
            // Only log error if we expected PowerShell to work
            Console.Error.WriteLine($"Error detecting Windows drives via PowerShell: {ex.Message}");
        }

        return drives;
    }

    /// <summary>
    /// Detects Windows removable drives that are not mounted in WSL
    /// </summary>
    public async Task<List<DetectedDrive>> GetUnmountedWindowsDrivesAsync()
    {
        var drives = new List<DetectedDrive>();

        try
        {
            // Check if we're running in WSL and PowerShell is available
            if (!await WslEnvironment.IsRunningInWslAsync(_commandLine))
            {
                return drives;
            }

            if (!await WslEnvironment.IsPowerShellAvailableAsync(_commandLine))
            {
                // PowerShell not available (e.g., running under sudo) - cannot detect unmounted drives
                return drives;
            }

            // Get volumes from PowerShell
            var volumes = await GetWindowsVolumesAsync();

            // Get currently mounted Windows drives in WSL
            var mountedDrives = await WslEnvironment.GetMountedWindowsDrivesAsync(_commandLine);

            foreach (var volume in volumes)
            {
                var driveLetter = volume.DriveLetter;

                // Skip if already mounted in WSL
                if (mountedDrives.Contains(driveLetter.ToLower()))
                {
                    continue;
                }

                // Skip drives with 0 bytes (empty drives, card readers, etc.)
                if (volume.Size == 0)
                {
                    continue;
                }

                var driveTypeLabel = WindowsVolumeParser.FormatDriveType(volume.DriveType);

                drives.Add(new DetectedDrive
                {
                    DevicePath = $"{driveLetter}:",
                    DeviceName = $"{driveLetter}:",
                    SizeBytes = volume.Size,
                    FormattedSize = ByteFormatter.Format(volume.Size),
                    FileSystemType = volume.FileSystemType,
                    Label = $"{driveTypeLabel} ({driveLetter}:)",
                    IsMounted = false,
                    MountPoint = null,
                    IsWindowsDrive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error detecting Windows drives: {ex.Message}");
        }

        return drives;
    }

    private async Task<List<WindowsVolume>> GetWindowsVolumesAsync()
    {
        var powershellCommand = WindowsVolumeParser.GetVolumeListCommand();

        var output = await _commandLine.ExecuteAndGetOutputAsync(
            "powershell.exe",
            $"-NoProfile -NonInteractive -Command \"{powershellCommand}\""
        );

        if (string.IsNullOrWhiteSpace(output))
        {
            return new List<WindowsVolume>();
        }

        return WindowsVolumeParser.ParseJson(output);
    }

    /// <summary>
    /// Detects only already-mounted Windows drives without requiring PowerShell
    /// This is useful when running under sudo where PowerShell may not be accessible
    /// </summary>
    private async Task<List<DetectedDrive>> GetMountedWindowsDrivesOnlyAsync()
    {
        var drives = new List<DetectedDrive>();

        try
        {
            // Get mounted Windows drives from mount output
            var mountedDrives = await WslEnvironment.GetMountedWindowsDrivesWithPathsAsync(_commandLine);

            foreach (var (driveLetter, mountPath) in mountedDrives)
            {
                drives.Add(new DetectedDrive
                {
                    DevicePath = $"{driveLetter.ToUpper()}:",
                    DeviceName = $"{driveLetter.ToUpper()}:",
                    SizeBytes = 0, // Size unknown without PowerShell
                    FormattedSize = "Unknown",
                    FileSystemType = "drvfs",
                    Label = $"Windows Drive ({driveLetter.ToUpper()}:)",
                    IsMounted = true,
                    MountPoint = mountPath,
                    IsWindowsDrive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error detecting mounted Windows drives: {ex.Message}");
        }

        return drives;
    }
}
