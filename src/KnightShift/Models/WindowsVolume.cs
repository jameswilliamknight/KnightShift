namespace KnightShift.Models;

/// <summary>
/// Represents a Windows volume from PowerShell Get-Volume output
/// </summary>
public class WindowsVolume
{
    public string DriveLetter { get; set; } = "";
    public string DriveType { get; set; } = "";
    public string FileSystemType { get; set; } = "";
    public long Size { get; set; }
    public long SizeRemaining { get; set; }
}
