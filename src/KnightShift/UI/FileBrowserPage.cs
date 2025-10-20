using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;
using KnightShift.UI.Helpers;
using static KnightShift.UI.Helpers.FileBrowserNavigationHandler;
using static KnightShift.UI.Helpers.FolderActionHandler;

namespace KnightShift.UI;

/// <summary>
/// UI page for browsing files and folders
/// </summary>
public class FileBrowserPage
{
    private readonly FileSystemBrowserService _browserService;
    private readonly TextRemovalPage _textRemovalPage;
    private readonly PropertiesPanel _propertiesPanel;

    private const string SpecialParentEntry = "..";
    private const string SpecialExitEntry = "← Exit Browser";

    public FileBrowserPage(
        FileSystemBrowserService browserService,
        TextRemovalPage textRemovalPage,
        PropertiesPanel propertiesPanel)
    {
        _browserService = browserService;
        _textRemovalPage = textRemovalPage;
        _propertiesPanel = propertiesPanel;
    }

    /// <summary>
    /// Shows the file browser starting at the given path
    /// </summary>
    public async Task ShowAsync(string startPath)
    {
        var currentPath = startPath;
        var mountPointPath = startPath; // Remember the root/mount point
        var running = true;
        var navigationHistory = new Stack<string>();
        string? lastSelectedFolder = null;

        while (running)
        {
            var result = await ShowBrowserAtPath(currentPath, mountPointPath, lastSelectedFolder);

            if (!result.ShouldContinue)
            {
                running = false;
            }
            else if (result.NewPath != null)
            {
                // Check if we're going back (up to parent)
                if (result.NewPath == _browserService.GetParentPath(currentPath))
                {
                    // Going back - pop from history
                    if (navigationHistory.Count > 0)
                    {
                        lastSelectedFolder = navigationHistory.Pop();
                    }
                }
                else
                {
                    // Going forward - push current folder name to history
                    var currentFolderName = Path.GetFileName(currentPath);
                    if (!string.IsNullOrEmpty(currentFolderName))
                    {
                        navigationHistory.Push(currentFolderName);
                    }
                    lastSelectedFolder = null;
                }

                currentPath = result.NewPath;
            }
        }
    }

    private async Task<NavigationResult> ShowBrowserAtPath(
        string currentPath,
        string mountPointPath,
        string? preSelectedFolder = null)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(StyleGuide.CreateTitleRule("File Browser"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Navigate:[/] Use ↑↓ arrows, ← to go back, → to enter folder, ENTER for actions.");
        AnsiConsole.MarkupLine($"[{StyleGuide.MutedColor}]Current path:[/] [bold]{Markup.Escape(currentPath)}[/]");
        AnsiConsole.WriteLine();

        // Get entries in current directory
        var entries = _browserService.GetEntries(currentPath);

        if (entries.Count == 0)
        {
            return HandleEmptyDirectory(currentPath, mountPointPath);
        }

        // Create selectable items
        var selectableItems = BuildSelectableItems(currentPath, mountPointPath, entries);

        // Find pre-selected item index
        int defaultIndex = FindPreselectedIndex(selectableItems, preSelectedFolder);

        // Use custom navigation with keyboard support
        var (selected, action) = FileBrowserNavigationHandler.ShowCustomSelection(selectableItems, defaultIndex);

        // Handle navigation actions
        return await HandleSelectionAsync(selected, action, currentPath, mountPointPath);
    }

    private NavigationResult HandleEmptyDirectory(string currentPath, string mountPointPath)
    {
        AnsiConsole.Write(StyleGuide.Warning("This directory is empty or inaccessible."));
        AnsiConsole.WriteLine();

        // Don't go above mount point
        if (IsAtMountPoint(currentPath, mountPointPath))
        {
            return new NavigationResult(true, currentPath); // Stay at mount point
        }

        var parent = _browserService.GetParentPath(currentPath);
        return new NavigationResult(true, parent);
    }

    private List<SelectableItem> BuildSelectableItems(string currentPath, string mountPointPath, List<FileSystemEntry> entries)
    {
        var selectableItems = new List<SelectableItem>();

        // Add parent directory option if not at mount point
        if (!IsAtMountPoint(currentPath, mountPointPath))
        {
            selectableItems.Add(new SelectableItem
            {
                Type = ItemType.Parent,
                DisplayText = SpecialParentEntry
            });
        }

        // Add all entries
        foreach (var entry in entries)
        {
            selectableItems.Add(new SelectableItem
            {
                Type = ItemType.Entry,
                Entry = entry,
                DisplayText = entry.DisplayString
            });
        }

        // Add exit option
        selectableItems.Add(new SelectableItem
        {
            Type = ItemType.Exit,
            DisplayText = SpecialExitEntry
        });

        return selectableItems;
    }

    private int FindPreselectedIndex(List<SelectableItem> selectableItems, string? preSelectedFolder)
    {
        if (string.IsNullOrEmpty(preSelectedFolder))
        {
            return 0;
        }

        for (int i = 0; i < selectableItems.Count; i++)
        {
            if (selectableItems[i].Entry?.Name == preSelectedFolder)
            {
                return i;
            }
        }

        return 0;
    }

    private async Task<NavigationResult> HandleSelectionAsync(
        SelectableItem selected,
        NavigationAction action,
        string currentPath,
        string mountPointPath)
    {
        // Handle navigation actions
        if (action == NavigationAction.GoBack || selected.Type == ItemType.Parent)
        {
            // Don't allow going above mount point
            if (IsAtMountPoint(currentPath, mountPointPath))
            {
                return new NavigationResult(true, currentPath);
            }

            var parentPath = _browserService.GetParentPath(currentPath);
            return new NavigationResult(true, parentPath);
        }
        else if (action == NavigationAction.Exit || selected.Type == ItemType.Exit)
        {
            return new NavigationResult(false);
        }
        else if (selected.Type == ItemType.Entry && selected.Entry != null)
        {
            return await HandleEntrySelectionAsync(selected.Entry, action, currentPath);
        }

        return new NavigationResult(true, currentPath);
    }

    private async Task<NavigationResult> HandleEntrySelectionAsync(
        FileSystemEntry entry,
        NavigationAction action,
        string currentPath)
    {
        if (entry.IsDirectory)
        {
            // If action is EnterFolder, navigate directly
            if (action == NavigationAction.EnterFolder)
            {
                return new NavigationResult(true, entry.FullPath, entry.Name);
            }

            // Otherwise show action menu for directory
            var menuAction = FolderActionHandler.ShowActionMenu(entry);

            return await HandleDirectoryActionAsync(entry, menuAction, currentPath);
        }
        else
        {
            // For files, just show a message
            ShowFileSelected(entry);
            return new NavigationResult(true, currentPath);
        }
    }

    private async Task<NavigationResult> HandleDirectoryActionAsync(
        FileSystemEntry entry,
        string menuAction,
        string currentPath)
    {
        switch (menuAction)
        {
            case var action when action == ActionRemoveText:
                await _textRemovalPage.ShowAsync(entry);
                return new NavigationResult(true, currentPath); // Refresh current directory

            case var action when action == ActionProperties:
                await _propertiesPanel.ShowAsync(entry);
                return new NavigationResult(true, currentPath); // Stay in current directory

            case var action when action == ActionOpenTerminal:
                FolderActionHandler.OpenInTerminal(entry.FullPath);
                return new NavigationResult(true, currentPath);

            case var action when action == ActionOpenVSCode:
                FolderActionHandler.OpenInVSCode(entry.FullPath);
                return new NavigationResult(true, currentPath);

            case var action when action == ActionGoBack:
                return new NavigationResult(true, currentPath); // Stay in current directory

            default:
                // Navigate into the directory
                return new NavigationResult(true, entry.FullPath, entry.Name);
        }
    }

    private void ShowFileSelected(FileSystemEntry entry)
    {
        AnsiConsole.Write(StyleGuide.Info($"Selected file: {entry.Name}"));
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Checks if the current path is at the mount point (root boundary for navigation)
    /// </summary>
    private bool IsAtMountPoint(string currentPath, string mountPointPath)
    {
        // Normalize paths for comparison (remove trailing slashes)
        var normalizedCurrent = currentPath.TrimEnd('/');
        var normalizedMount = mountPointPath.TrimEnd('/');

        return string.Equals(normalizedCurrent, normalizedMount, StringComparison.OrdinalIgnoreCase);
    }
}
