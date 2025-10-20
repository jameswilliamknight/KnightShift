using Spectre.Console;
using KnightShift.Models;
using System.Text;

namespace KnightShift.UI.Helpers;

/// <summary>
/// Helper class for rendering regex preview UI components
/// </summary>
public static class RegexPreviewRenderer
{
    /// <summary>
    /// Renders the input fields panel
    /// </summary>
    public static void RenderInputFields(CustomTextInput searchInput, CustomTextInput replaceInput, bool searchFocused, bool replaceFocused)
    {
        // Search field
        var searchText = searchInput.GetDisplayText(searchFocused);
        var searchPanel = StyleGuide.CreateInputPanel(
            "Search Pattern (regex)",
            string.IsNullOrEmpty(searchText) ? "(type to begin...)" : searchText,
            searchFocused
        );
        AnsiConsole.Write(searchPanel);

        // Replace field
        var replaceText = replaceInput.GetDisplayText(replaceFocused);
        var replacePanel = StyleGuide.CreateInputPanel(
            "Replace With (supports $1, $2 capture groups)",
            string.IsNullOrEmpty(replaceText) ? "(empty = remove)" : replaceText,
            replaceFocused
        );
        AnsiConsole.Write(replacePanel);
    }

    /// <summary>
    /// Renders the preview table with highlighting
    /// </summary>
    public static void RenderPreview(List<RenamePreview> previews, int scrollOffset, int pageSize, bool isPreviewFocused)
    {
        var title = isPreviewFocused
            ? "[bold dodgerblue1]► Preview (scrollable)[/]"
            : "Preview";

        if (previews.Count == 0)
        {
            var emptyPanel = new Panel(new Markup($"[{StyleGuide.Muted}]No folders found in this directory.[/]"))
            {
                Header = new PanelHeader(title),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(isPreviewFocused ? StyleGuide.PrimaryColor : StyleGuide.MutedColor),
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(emptyPanel);
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(isPreviewFocused ? StyleGuide.PrimaryColor : StyleGuide.MutedColor)
            .Expand();

        table.AddColumn(new TableColumn("").Width(2));
        table.AddColumn(new TableColumn("[bold]Before[/]").LeftAligned());
        table.AddColumn(new TableColumn("→").Width(3).Centered());
        table.AddColumn(new TableColumn("[bold]After[/]").LeftAligned());

        var visiblePreviews = previews.Skip(scrollOffset).Take(pageSize);

        foreach (var preview in visiblePreviews)
        {
            var icon = preview.StatusIcon;
            var beforeText = HighlightMatches(preview.OriginalName, preview.MatchPositions);
            var afterText = FormatAfterText(preview);

            table.AddRow(icon, beforeText, "→", afterText);
        }

        // Add scroll indicator
        if (previews.Count > pageSize)
        {
            var showing = $"Showing {scrollOffset + 1}-{Math.Min(scrollOffset + pageSize, previews.Count)} of {previews.Count}";
            table.AddRow("", $"[{StyleGuide.Muted}]{showing}[/]", "", "");
        }

        var panel = new Panel(table)
        {
            Header = new PanelHeader(title),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(isPreviewFocused ? StyleGuide.PrimaryColor : StyleGuide.MutedColor),
            Padding = new Padding(0, 0)
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Highlights regex matches in text with yellow background
    /// </summary>
    public static string HighlightMatches(string text, List<MatchPosition> matches)
    {
        if (matches == null || matches.Count == 0)
        {
            return Markup.Escape(text);
        }

        var result = new StringBuilder();
        int lastIndex = 0;

        // Sort matches by position
        var sortedMatches = matches.OrderBy(m => m.Start).ToList();

        foreach (var match in sortedMatches)
        {
            // Add text before match
            if (match.Start > lastIndex)
            {
                result.Append(Markup.Escape(text.Substring(lastIndex, match.Start - lastIndex)));
            }

            // Add highlighted match
            var matchedText = text.Substring(match.Start, match.Length);
            result.Append($"[black on yellow]{Markup.Escape(matchedText)}[/]");

            lastIndex = match.Start + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            result.Append(Markup.Escape(text.Substring(lastIndex)));
        }

        return result.ToString();
    }

    /// <summary>
    /// Formats the "After" column text with appropriate colors
    /// </summary>
    public static string FormatAfterText(RenamePreview preview)
    {
        if (preview.HasConflict)
        {
            return $"[{StyleGuide.WarningMarkup}]{Markup.Escape(preview.NewName)} (conflict!)[/]";
        }
        else if (preview.HasEmptyResult)
        {
            return $"[{StyleGuide.Muted} on grey]□ (empty - will skip)[/]";
        }
        else if (preview.WillChange)
        {
            return $"[black on green]{Markup.Escape(preview.NewName)}[/]";
        }
        else
        {
            return $"[{StyleGuide.Muted}]{Markup.Escape(preview.NewName)}[/]";
        }
    }

    /// <summary>
    /// Renders statistics summary
    /// </summary>
    public static void RenderStats(List<RenamePreview> previews)
    {
        if (previews.Count == 0) return;

        var willChange = previews.Count(p => p.WillChange && !p.HasConflict && !p.HasEmptyResult);
        var conflicts = previews.Count(p => p.HasConflict);
        var empty = previews.Count(p => p.HasEmptyResult);

        var stats = new StringBuilder();

        if (willChange > 0)
        {
            stats.Append($"[green]✓ {willChange} will be renamed[/]  ");
        }
        else
        {
            stats.Append($"[{StyleGuide.Muted}]No changes[/]  ");
        }

        if (conflicts > 0)
        {
            stats.Append($"[orange1]⚠️  {conflicts} conflicts[/]  ");
        }

        if (empty > 0)
        {
            stats.Append($"[{StyleGuide.Muted}]{empty} empty results (will skip)[/]");
        }

        AnsiConsole.MarkupLine(stats.ToString());
    }

    /// <summary>
    /// Renders the hotkey panel
    /// </summary>
    public static void RenderHotkeys()
    {
        var hotkeys = StyleGuide.CreateHotkeyPanel(
            $"[{StyleGuide.Primary}]↑↓:[/] Switch Fields/Scroll  " +
            $"[{StyleGuide.Primary}]←→:[/] Move Cursor  " +
            $"[{StyleGuide.Primary}]Tab:[/] Next Field  " +
            $"[{StyleGuide.Primary}]Enter:[/] Apply  " +
            $"[{StyleGuide.Primary}]F2:[/] Toggle Mode  " +
            $"[{StyleGuide.Primary}]F1:[/] Help  " +
            $"[{StyleGuide.Primary}]Esc:[/] Cancel"
        );
        AnsiConsole.Write(hotkeys);
    }

    /// <summary>
    /// Shows the help screen
    /// </summary>
    public static void ShowHelp()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(StyleGuide.CreateTitleRule("Regex Replace - Help"));
        AnsiConsole.WriteLine();

        var helpText = new Markup(
            "[bold underline]How to Use:[/]\n\n" +
            "1. Type your regex pattern in the [dodgerblue1]Search[/] field\n" +
            "2. Press [dodgerblue1]↓[/] to move to the [dodgerblue1]Replace[/] field\n" +
            "3. Enter your replacement (supports $1, $2 for capture groups)\n" +
            "4. Watch the live preview update in real-time\n" +
            "5. Press [dodgerblue1]Enter[/] to apply when ready\n\n" +
            "[bold underline]Keyboard Shortcuts:[/]\n\n" +
            "  [dodgerblue1]↑↓[/]       Switch between fields or scroll preview\n" +
            "  [dodgerblue1]←→[/]       Move cursor within input field\n" +
            "  [dodgerblue1]Tab[/]      Cycle through all fields\n" +
            "  [dodgerblue1]Enter[/]    Apply changes (shows confirmation)\n" +
            "  [dodgerblue1]F2[/]       Toggle between Regex and Literal mode\n" +
            "  [dodgerblue1]Esc[/]      Cancel and return\n\n" +
            "[bold underline]Preview Colors:[/]\n\n" +
            "  [black on yellow]Yellow[/]    Matched text in original names\n" +
            "  [black on green]Green[/]     Result after replacement\n" +
            "  [grey on grey]Gray box[/]  Empty result (will be skipped)\n" +
            "  [orange1]⚠️  Orange[/]  Conflict with existing folder\n\n" +
            "[bold underline]Examples:[/]\n\n" +
            $"  [yellow]IMG_[/]           Remove 'IMG_' prefix\n" +
            $"  [yellow]^\\d{{4}}_[/]       Remove date prefix like '2024_'\n" +
            $"  [yellow](\\d{{4}}).*[/]    Keep only year: [green]$1[/]\n" +
            $"  [yellow]\\s+[/]            Replace multiple spaces with one\n\n" +
            "[grey]Press any key to return...[/]"
        );

        var panel = new Panel(helpText)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(StyleGuide.PrimaryColor),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        Console.ReadKey(true);
    }
}
