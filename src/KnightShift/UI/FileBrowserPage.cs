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
        var running = true;
        var navigationHistory = new Stack<string>();
        string? lastSelectedFolder = null;

        while (running)
        {
            var (shouldContinue, newPath, selectedFolderName) = await ShowBrowserAtPath(currentPath, lastSelectedFolder);

            if (!shouldContinue)
            {
                running = false;
            }
            else if (newPath != null)
            {
                // Check if we're going back (up to parent)
                if (newPath == _browserService.GetParentPath(currentPath))
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

                currentPath = newPath;
            }
        }
    }

    private async Task<(bool shouldContinue, string? newPath, string? selectedFolderName)> ShowBrowserAtPath(
        string currentPath,
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
            return HandleEmptyDirectory(currentPath);
        }

        // Create selectable items
        var selectableItems = BuildSelectableItems(currentPath, entries);

        // Find pre-selected item index
        int defaultIndex = FindPreselectedIndex(selectableItems, preSelectedFolder);

        // Use custom navigation with keyboard support
        var (selected, action) = FileBrowserNavigationHandler.ShowCustomSelection(selectableItems, defaultIndex);

        // Handle navigation actions
        return await HandleSelectionAsync(selected, action, currentPath);
    }

    private (bool shouldContinue, string? newPath, string? selectedFolderName) HandleEmptyDirectory(string currentPath)
    {
        AnsiConsole.Write(StyleGuide.Warning("This directory is empty or inaccessible."));
        AnsiConsole.WriteLine();

        var parent = _browserService.GetParentPath(currentPath);
        return (true, parent, null);
    }

    private List<SelectableItem> BuildSelectableItems(string currentPath, List<FileSystemEntry> entries)
    {
        var selectableItems = new List<SelectableItem>();

        // Add parent directory option if not at root
        var parentPath = _browserService.GetParentPath(currentPath);
        if (parentPath != null)
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

    private async Task<(bool shouldContinue, string? newPath, string? selectedFolderName)> HandleSelectionAsync(
        SelectableItem selected,
        NavigationAction action,
        string currentPath)
    {
        var parentPath = _browserService.GetParentPath(currentPath);

        // Handle navigation actions
        if (action == NavigationAction.GoBack || selected.Type == ItemType.Parent)
        {
            return (true, parentPath, null);
        }
        else if (action == NavigationAction.Exit || selected.Type == ItemType.Exit)
        {
            return (false, null, null);
        }
        else if (selected.Type == ItemType.Entry && selected.Entry != null)
        {
            return await HandleEntrySelectionAsync(selected.Entry, action, currentPath);
        }

        return (true, currentPath, null);
    }

    private async Task<(bool shouldContinue, string? newPath, string? selectedFolderName)> HandleEntrySelectionAsync(
        FileSystemEntry entry,
        NavigationAction action,
        string currentPath)
    {
        if (entry.IsDirectory)
        {
            // If action is EnterFolder, navigate directly
            if (action == NavigationAction.EnterFolder)
            {
                return (true, entry.FullPath, entry.Name);
            }

            // Otherwise show action menu for directory
            var menuAction = FolderActionHandler.ShowActionMenu(entry);

            return await HandleDirectoryActionAsync(entry, menuAction, currentPath);
        }
        else
        {
            // For files, just show a message
            ShowFileSelected(entry);
            return (true, currentPath, null);
        }
    }

    private async Task<(bool shouldContinue, string? newPath, string? selectedFolderName)> HandleDirectoryActionAsync(
        FileSystemEntry entry,
        string menuAction,
        string currentPath)
    {
        switch (menuAction)
        {
            case var action when action == ActionRemoveText:
                await _textRemovalPage.ShowAsync(entry);
                return (true, currentPath, null); // Refresh current directory

            case var action when action == ActionProperties:
                await _propertiesPanel.ShowAsync(entry);
                return (true, currentPath, null); // Stay in current directory

            case var action when action == ActionOpenTerminal:
                FolderActionHandler.OpenInTerminal(entry.FullPath);
                return (true, currentPath, null);

            case var action when action == ActionOpenVSCode:
                FolderActionHandler.OpenInVSCode(entry.FullPath);
                return (true, currentPath, null);

            case var action when action == ActionGoBack:
                return (true, currentPath, null); // Stay in current directory

            default:
                // Navigate into the directory
                return (true, entry.FullPath, entry.Name);
        }
    }

    private void ShowFileSelected(FileSystemEntry entry)
    {
        AnsiConsole.Write(StyleGuide.Info($"Selected file: {entry.Name}"));
        AnsiConsole.WriteLine();
    }
}
