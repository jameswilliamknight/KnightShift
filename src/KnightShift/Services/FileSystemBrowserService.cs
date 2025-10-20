using KnightShift.Models;

namespace KnightShift.Services;

/// <summary>
/// Service for browsing the file system
/// </summary>
public class FileSystemBrowserService
{
    /// <summary>
    /// Gets all directories and files in the specified path
    /// </summary>
    public List<FileSystemEntry> GetEntries(string path, bool includeFiles = true)
    {
        var entries = new List<FileSystemEntry>();

        try
        {
            if (!Directory.Exists(path))
                return entries;

            // Get directories
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    entries.Add(new FileSystemEntry
                    {
                        FullPath = dir,
                        Name = dirInfo.Name,
                        IsDirectory = true,
                        SizeBytes = 0,
                        LastModified = dirInfo.LastWriteTime
                    });
                }
                catch
                {
                    // Skip directories we can't access
                }
            }

            // Get files if requested
            if (includeFiles)
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        entries.Add(new FileSystemEntry
                        {
                            FullPath = file,
                            Name = fileInfo.Name,
                            IsDirectory = false,
                            SizeBytes = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                    }
                    catch
                    {
                        // Skip files we can't access
                    }
                }
            }

            // Sort: directories first, then files, alphabetically
            entries = entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading directory {path}: {ex.Message}");
        }

        return entries;
    }

    /// <summary>
    /// Gets only directories in the specified path
    /// </summary>
    public List<FileSystemEntry> GetDirectories(string path)
    {
        return GetEntries(path, includeFiles: false);
    }

    /// <summary>
    /// Gets the immediate child directories of a given path
    /// </summary>
    public List<FileSystemEntry> GetImmediateChildDirectories(string path)
    {
        return GetDirectories(path);
    }

    /// <summary>
    /// Calculates the total size of a directory (recursively)
    /// </summary>
    public long CalculateDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            long size = 0;

            // Get file sizes
            foreach (var file in dirInfo.GetFiles())
            {
                try
                {
                    size += file.Length;
                }
                catch
                {
                    // Skip files we can't access
                }
            }

            // Recursively get subdirectory sizes
            foreach (var dir in dirInfo.GetDirectories())
            {
                try
                {
                    size += CalculateDirectorySize(dir.FullName);
                }
                catch
                {
                    // Skip directories we can't access
                }
            }

            return size;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Counts files and folders in a directory
    /// </summary>
    public (int fileCount, int folderCount) CountContents(string path, bool recursive = false)
    {
        int fileCount = 0;
        int folderCount = 0;

        try
        {
            var dirInfo = new DirectoryInfo(path);

            if (recursive)
            {
                fileCount = dirInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                folderCount = dirInfo.GetDirectories("*", SearchOption.AllDirectories).Length;
            }
            else
            {
                fileCount = dirInfo.GetFiles().Length;
                folderCount = dirInfo.GetDirectories().Length;
            }
        }
        catch
        {
            // Return zeros on error
        }

        return (fileCount, folderCount);
    }

    /// <summary>
    /// Gets the parent directory path
    /// </summary>
    public string? GetParentPath(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.Parent?.FullName;
        }
        catch
        {
            return null;
        }
    }
}
