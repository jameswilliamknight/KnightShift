using KnightShift.Models;

namespace KnightShift.Services.Helpers;

/// <summary>
/// Processes Linux block devices from lsblk output
/// </summary>
public static class LinuxDeviceProcessor
{
    /// <summary>
    /// Processes a device and adds it to the drives list if it meets criteria
    /// </summary>
    public static void ProcessDevice(LsblkDevice device, List<DetectedDrive> drives, bool includeMounted)
    {
        var isMounted = !string.IsNullOrWhiteSpace(device.mountpoint);

        // Skip if already mounted and we only want unmounted drives
        if (isMounted && !includeMounted)
            return;

        // Skip if no filesystem (unformatted)
        if (string.IsNullOrWhiteSpace(device.fstype))
            return;

        // Skip system partitions we shouldn't touch
        if (IsSystemDevice(device.name))
            return;

        var devicePath = $"/dev/{device.name}";
        var sizeBytes = device.sizebytes ?? 0;

        // Skip drives with 0 bytes
        if (sizeBytes == 0)
            return;

        drives.Add(new DetectedDrive
        {
            DevicePath = devicePath,
            DeviceName = device.name,
            SizeBytes = sizeBytes,
            FormattedSize = device.size ?? ByteFormatter.Format(sizeBytes),
            FileSystemType = device.fstype,
            Label = device.label,
            IsMounted = isMounted,
            MountPoint = device.mountpoint,
            IsWindowsDrive = false
        });
    }

    /// <summary>
    /// Determines if a device is a system device that should be filtered out
    /// </summary>
    private static bool IsSystemDevice(string deviceName)
    {
        // Skip loop devices, SR-ROM devices, and RAM disks
        if (deviceName.StartsWith("loop") ||
            deviceName.StartsWith("sr") ||
            deviceName.StartsWith("ram"))
        {
            return true;
        }

        // Skip all /dev/sd* drives (sda, sdb, sdc, ... sdz, sdaa, etc.)
        // These are typically system SCSI/SATA drives
        if (deviceName.StartsWith("sd"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes all devices and their children from lsblk output
    /// </summary>
    public static void ProcessDevicesRecursively(List<LsblkDevice> devices, List<DetectedDrive> drives, bool includeMounted)
    {
        foreach (var device in devices)
        {
            // Skip loop devices, ram disks, etc. Focus on disk and part types
            if (device.type != "disk" && device.type != "part")
                continue;

            // Check the device itself
            ProcessDevice(device, drives, includeMounted);

            // Check children (partitions)
            if (device.children != null)
            {
                foreach (var child in device.children)
                {
                    ProcessDevice(child, drives, includeMounted);
                }
            }
        }
    }
}
