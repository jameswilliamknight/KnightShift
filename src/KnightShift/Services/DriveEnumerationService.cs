using System.Text.Json;
using KnightShift.Models;
using KnightShift.Services.Helpers;

namespace KnightShift.Services;

/// <summary>
/// Service for enumerating drives (both Linux block devices and Windows drives)
/// Implements IDriveQueries for read operations
/// </summary>
public class DriveEnumerationService : IDriveQueries
{
    private readonly ICommandLineProvider _commandLine;
    private readonly WindowsDriveDetectionService _windowsDriveDetection;

    public DriveEnumerationService(ICommandLineProvider commandLine)
    {
        _commandLine = commandLine;
        _windowsDriveDetection = new WindowsDriveDetectionService(commandLine);
    }

    public DriveEnumerationService()
        : this(new CommandLineProvider())
    {
    }

    /// <summary>
    /// Gets a list of all drives (both mounted and unmounted)
    /// </summary>
    public async Task<List<DetectedDrive>> GetAllDrivesAsync()
    {
        var drives = new List<DetectedDrive>();

        // Get both mounted and unmounted Linux drives
        var linuxDrives = await GetLinuxDrivesAsync(includeMounted: true);
        drives.AddRange(linuxDrives);

        // Get both mounted and unmounted Windows drives
        var windowsDrives = await _windowsDriveDetection.GetAllWindowsDrivesAsync();
        drives.AddRange(windowsDrives);

        return drives;
    }

    /// <summary>
    /// Gets a list of all unmounted drives (Linux block devices and Windows drives)
    /// </summary>
    public async Task<List<DetectedDrive>> GetUnmountedDrivesAsync()
    {
        var drives = new List<DetectedDrive>();

        // Get Linux block devices
        var linuxDrives = await GetLinuxDrivesAsync(includeMounted: false);
        drives.AddRange(linuxDrives);

        // Get Windows removable drives (if running in WSL)
        var windowsDrives = await _windowsDriveDetection.GetUnmountedWindowsDrivesAsync();
        drives.AddRange(windowsDrives);

        return drives;
    }

    private async Task<List<DetectedDrive>> GetLinuxDrivesAsync(bool includeMounted)
    {
        var drives = new List<DetectedDrive>();

        try
        {
            // Use lsblk with JSON output to get drive information
            var lsblkOutput = await _commandLine.ExecuteAndGetOutputAsync(
                "lsblk",
                "-J -o NAME,SIZE,FSTYPE,LABEL,MOUNTPOINT,TYPE -b"
            );

            if (string.IsNullOrWhiteSpace(lsblkOutput))
                return drives;

            var lsblkData = JsonSerializer.Deserialize<LsblkOutput>(lsblkOutput,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (lsblkData?.blockdevices == null)
                return drives;

            // Process all devices and their children
            LinuxDeviceProcessor.ProcessDevicesRecursively(lsblkData.blockdevices, drives, includeMounted);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - return empty list
            Console.Error.WriteLine($"Error enumerating drives: {ex.Message}");
        }

        return drives;
    }
}
