using Spectre.Console;
using KnightShift.Models;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles custom keyboard navigation for file browser
/// </summary>
public static class FileBrowserNavigationHandler
{
    public enum NavigationAction
    {
        Select,
        EnterFolder,
        GoBack,
        Exit
    }

    public enum ItemType
    {
        Parent,
        Entry,
        Exit
    }

    public class SelectableItem
    {
        public ItemType Type { get; set; }
        public FileSystemEntry? Entry { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shows a custom scrollable selection list with keyboard navigation support
    /// </summary>
    public static (SelectableItem selected, NavigationAction action) ShowCustomSelection(
        List<SelectableItem> items,
        int startIndex = 0)
    {
        int selectedIndex = startIndex;
        const int pageSize = 15;
        int topIndex = Math.Max(0, selectedIndex - pageSize / 2);

        while (true)
        {
            // Adjust top index to keep selected item visible
            if (selectedIndex < topIndex)
            {
                topIndex = selectedIndex;
            }
            else if (selectedIndex >= topIndex + pageSize)
            {
                topIndex = selectedIndex - pageSize + 1;
            }

            // Render the list
            AnsiConsole.Cursor.SetPosition(0, 6); // Position after header
            var visibleItems = items.Skip(topIndex).Take(pageSize).ToList();

            for (int i = 0; i < pageSize; i++)
            {
                var actualIndex = topIndex + i;
                if (actualIndex < items.Count)
                {
                    var item = items[actualIndex];
                    var isSelected = actualIndex == selectedIndex;
                    var prefix = isSelected ? "→ " : "  ";
                    var color = isSelected ? StyleGuide.Primary : "white";

                    AnsiConsole.MarkupLine($"[{color}]{prefix}{Markup.Escape(item.DisplayText)}[/]".PadRight(Console.WindowWidth));
                }
                else
                {
                    AnsiConsole.WriteLine(new string(' ', Console.WindowWidth));
                }
            }

            // Show more indicators
            if (topIndex > 0)
            {
                AnsiConsole.Cursor.SetPosition(0, 6);
                AnsiConsole.MarkupLine($"[{StyleGuide.MutedColor}]  ▲ More above...[/]");
                AnsiConsole.Cursor.SetPosition(0, 6 + 1);
            }
            if (topIndex + pageSize < items.Count)
            {
                AnsiConsole.Cursor.SetPosition(0, 6 + pageSize - 1);
                AnsiConsole.MarkupLine($"[{StyleGuide.MutedColor}]  ▼ More below...[/]");
            }

            // Read key
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                    break;

                case ConsoleKey.DownArrow:
                    selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                    break;

                case ConsoleKey.LeftArrow:
                    // Go back to parent
                    return (items[selectedIndex], NavigationAction.GoBack);

                case ConsoleKey.RightArrow:
                    // Enter folder if directory
                    if (items[selectedIndex].Type == ItemType.Entry &&
                        items[selectedIndex].Entry?.IsDirectory == true)
                    {
                        return (items[selectedIndex], NavigationAction.EnterFolder);
                    }
                    break;

                case ConsoleKey.Enter:
                    // Select current item
                    return (items[selectedIndex], NavigationAction.Select);

                case ConsoleKey.Escape:
                    // Find exit item
                    var exitItem = items.FirstOrDefault(i => i.Type == ItemType.Exit);
                    return (exitItem ?? items[selectedIndex], NavigationAction.Exit);
            }
        }
    }
}
