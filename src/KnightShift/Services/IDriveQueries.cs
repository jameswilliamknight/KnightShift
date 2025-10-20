using KnightShift.Models;

namespace KnightShift.Services;

/// <summary>
/// Query interface for reading drive information
/// </summary>
public interface IDriveQueries
{
    /// <summary>
    /// Gets a list of all drives (both mounted and unmounted)
    /// </summary>
    Task<List<DetectedDrive>> GetAllDrivesAsync();

    /// <summary>
    /// Gets a list of all unmounted drives (Linux block devices and Windows drives)
    /// </summary>
    Task<List<DetectedDrive>> GetUnmountedDrivesAsync();
}
