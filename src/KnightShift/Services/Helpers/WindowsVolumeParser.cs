using KnightShift.Models;

namespace KnightShift.Services.Helpers;

/// <summary>
/// Parses PowerShell Get-Volume JSON output into WindowsVolume objects
/// </summary>
public static class WindowsVolumeParser
{
    /// <summary>
    /// Parses PowerShell JSON output (handles both single object and array)
    /// </summary>
    public static List<WindowsVolume> ParseJson(string json)
    {
        var volumes = new List<WindowsVolume>();

        try
        {
            // Handle both single object and array responses
            json = json.Trim();

            if (!json.StartsWith("["))
            {
                json = $"[{json}]";
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            volumes = System.Text.Json.JsonSerializer.Deserialize<List<WindowsVolume>>(json, options) ?? new();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing PowerShell output: {ex.Message}");
        }

        return volumes;
    }

    /// <summary>
    /// Gets the PowerShell command for listing Windows volumes
    /// </summary>
    public static string GetVolumeListCommand()
    {
        return @"
            Get-Volume | Where-Object {
                $_.DriveLetter -ne $null -and
                $_.DriveLetter -ne 'C' -and
                ($_.DriveType -eq 'Removable' -or
                 $_.DriveType -eq 'Fixed' -or
                 $_.DriveType -eq 'Network')
            } | Select-Object DriveLetter, DriveType, FileSystemType, Size, SizeRemaining |
            ConvertTo-Json -Compress
        ";
    }

    /// <summary>
    /// Formats drive type for display
    /// </summary>
    public static string FormatDriveType(string driveType)
    {
        return driveType switch
        {
            "Removable" => "USB/Removable",
            "Fixed" => "Fixed Drive",
            "Network" => "Network Share",
            _ => "Drive"
        };
    }
}
