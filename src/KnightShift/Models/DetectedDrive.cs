namespace KnightShift.Models;

/// <summary>
/// Represents information about a detected drive/block device
/// </summary>
public class DetectedDrive
{
    /// <summary>
    /// Device path (e.g., /dev/sdb1)
    /// </summary>
    public required string DevicePath { get; init; }

    /// <summary>
    /// Device name (e.g., sdb1)
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Size in bytes
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Formatted size string (e.g., "32.5 GB")
    /// </summary>
    public required string FormattedSize { get; init; }

    /// <summary>
    /// Filesystem type (e.g., ext4, ntfs, vfat)
    /// </summary>
    public string? FileSystemType { get; init; }

    /// <summary>
    /// Label/name of the drive if available
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Whether this drive is currently mounted
    /// </summary>
    public bool IsMounted { get; init; }

    /// <summary>
    /// Mount point if currently mounted
    /// </summary>
    public string? MountPoint { get; init; }

    /// <summary>
    /// Whether this is a Windows drive (mounted via drvfs in WSL)
    /// </summary>
    public bool IsWindowsDrive { get; init; }

    /// <summary>
    /// Display string for the selection prompt
    /// </summary>
    public string DisplayString
    {
        get
        {
            // Different icons for mounted vs unmounted
            string icon;
            string statusLabel;

            if (IsMounted)
            {
                icon = IsWindowsDrive ? "✅💾" : "✅🔌";
                statusLabel = $"Already Mounted at {MountPoint}";
            }
            else
            {
                icon = IsWindowsDrive ? "💾" : "🔌";
                statusLabel = IsWindowsDrive ? "Windows Drive (Not Mounted)" : "USB Drive (Not Mounted)";
            }

            return $"{icon} {DeviceName} - {FormattedSize}" +
                   (string.IsNullOrEmpty(Label) ? "" : $" [{Label}]") +
                   (FileSystemType != null ? $" ({FileSystemType})" : "") +
                   $" - {statusLabel}";
        }
    }
}
