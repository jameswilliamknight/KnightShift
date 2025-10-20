using System.Diagnostics;

namespace KnightShift.Services;

/// <summary>
/// Service for mounting and unmounting drives (both Linux and Windows)
/// Implements IMountCommands for write operations
/// </summary>
public class MountService : IMountCommands
{
    private const string DefaultMountBasePath = "/mnt";
    private readonly ICommandLineProvider _commandLine;

    public MountService(ICommandLineProvider commandLine)
    {
        _commandLine = commandLine;
    }

    public MountService()
        : this(new CommandLineProvider())
    {
    }

    /// <summary>
    /// Result of a mount operation
    /// </summary>
    public class MountResult
    {
        public bool Success { get; init; }
        public string? MountPoint { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Mounts a drive to a specified mount point
    /// </summary>
    /// <param name="devicePath">Device path (e.g., /dev/sdb1 or J:)</param>
    /// <param name="mountPoint">Optional mount point. If not specified, uses /mnt/{devicename}</param>
    /// <param name="fileSystemType">Optional filesystem type for mount command</param>
    /// <param name="isWindowsDrive">Whether this is a Windows drive (uses drvfs)</param>
    public async Task<MountResult> MountDriveAsync(string devicePath, string? mountPoint = null, string? fileSystemType = null, bool isWindowsDrive = false)
    {
        try
        {
            // Generate mount point if not provided
            if (string.IsNullOrWhiteSpace(mountPoint))
            {
                var deviceName = Path.GetFileName(devicePath);
                mountPoint = Path.Combine(DefaultMountBasePath, deviceName);
            }

            // Check if directory exists
            if (Directory.Exists(mountPoint))
            {
                // Verify it's empty
                try
                {
                    var entries = Directory.EnumerateFileSystemEntries(mountPoint);
                    if (entries.Any())
                    {
                        return new MountResult
                        {
                            Success = false,
                            ErrorMessage = $"Mount point {mountPoint} already exists and is not empty. Cannot mount here."
                        };
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return new MountResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot access {mountPoint} to verify it's empty. Permission denied."
                    };
                }
            }
            else
            {
                // Create the mount point directory
                var mkdirResult = await _commandLine.ExecuteAsync("sudo", $"mkdir -p {mountPoint}");
                if (!mkdirResult.Success)
                {
                    return new MountResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to create mount point {mountPoint}: {mkdirResult.Error}"
                    };
                }
            }

            // Build mount command arguments
            string arguments;
            if (isWindowsDrive)
            {
                // For Windows drives, always use drvfs
                arguments = $"-t drvfs {devicePath} {mountPoint}";
            }
            else
            {
                // For Linux drives, use standard mount
                arguments = fileSystemType != null
                    ? $"-t {fileSystemType} {devicePath} {mountPoint}"
                    : $"{devicePath} {mountPoint}";
            }

            // Execute mount command
            var result = await _commandLine.ExecuteAsync("sudo", $"mount {arguments}");
            var (exitCode, output, error) = (result.ExitCode, result.Output, result.Error);

            if (exitCode == 0)
            {
                return new MountResult
                {
                    Success = true,
                    MountPoint = mountPoint
                };
            }
            else
            {
                return new MountResult
                {
                    Success = false,
                    ErrorMessage = $"Mount failed: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new MountResult
            {
                Success = false,
                ErrorMessage = $"Error mounting drive: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Unmounts a drive from its mount point
    /// </summary>
    public async Task<MountResult> UnmountDriveAsync(string mountPoint)
    {
        try
        {
            var result = await _commandLine.ExecuteAsync("sudo", $"umount {mountPoint}");
            var (exitCode, output, error) = (result.ExitCode, result.Output, result.Error);

            if (exitCode == 0)
            {
                return new MountResult
                {
                    Success = true,
                    MountPoint = mountPoint
                };
            }
            else
            {
                return new MountResult
                {
                    Success = false,
                    ErrorMessage = $"Unmount failed: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new MountResult
            {
                Success = false,
                ErrorMessage = $"Error unmounting drive: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if user has sudo privileges
    /// </summary>
    public async Task<bool> HasSudoPrivilegesAsync()
    {
        try
        {
            var result = await _commandLine.ExecuteAsync("sudo", "-n true");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }
}
