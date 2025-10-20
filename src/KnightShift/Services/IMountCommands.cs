namespace KnightShift.Services;

/// <summary>
/// Command interface for mounting and unmounting drives
/// </summary>
public interface IMountCommands
{
    /// <summary>
    /// Mounts a drive to a specified mount point
    /// </summary>
    /// <param name="devicePath">Device path (e.g., /dev/sdb1 or J:)</param>
    /// <param name="mountPoint">Optional mount point. If not specified, uses /mnt/{devicename}</param>
    /// <param name="fileSystemType">Optional filesystem type for mount command</param>
    /// <param name="isWindowsDrive">Whether this is a Windows drive (uses drvfs)</param>
    Task<MountService.MountResult> MountDriveAsync(string devicePath, string? mountPoint = null, string? fileSystemType = null, bool isWindowsDrive = false);

    /// <summary>
    /// Unmounts a drive from its mount point
    /// </summary>
    Task<MountService.MountResult> UnmountDriveAsync(string mountPoint);

    /// <summary>
    /// Checks if user has sudo privileges
    /// </summary>
    Task<bool> HasSudoPrivilegesAsync();
}
