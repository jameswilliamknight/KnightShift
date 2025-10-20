using Spectre.Console;
using KnightShift.Models;
using KnightShift.Services;
using KnightShift.UI.Helpers;

namespace KnightShift.UI;

/// <summary>
/// Enhanced regex rename UI with dual input fields and live preview
/// Refactored with helper classes to reduce complexity
/// </summary>
public class TextRemovalPage
{
    private readonly FolderRenameService _renameService;
    private readonly FileSystemBrowserService _browserService;

    public TextRemovalPage(FolderRenameService renameService, FileSystemBrowserService browserService)
    {
        _renameService = renameService;
        _browserService = browserService;
    }

    /// <summary>
    /// Shows the enhanced regex replace UI with live preview
    /// </summary>
    public async Task<bool> ShowAsync(FileSystemEntry selectedFolder)
    {
        var searchInput = new CustomTextInput();
        var replaceInput = new CustomTextInput();
        var focusState = RegexKeyboardHandler.FocusState.SearchField;
        var previewScrollOffset = 0;
        var lastPreviewUpdateTime = DateTime.MinValue;
        const int debounceMs = 100;

        List<RenamePreview> currentPreviews = new();
        bool useRegex = true;

        while (true)
        {
            // Calculate preview page size dynamically based on terminal height
            // Reserve space for: title(2) + folder info(4) + input fields(6) + stats(4) + hotkeys(4) + margins(6) = ~26 lines
            int previewPageSize = Math.Max(10, Console.WindowHeight - 26);

            // Generate preview if enough time has passed (debouncing)
            if (ShouldUpdatePreview(lastPreviewUpdateTime, debounceMs, searchInput.Text))
            {
                currentPreviews = _renameService.GenerateRenamePreview(
                    selectedFolder.FullPath,
                    searchInput.Text,
                    replaceInput.Text,
                    useRegex
                );
                lastPreviewUpdateTime = DateTime.Now;
            }

            // Render the entire UI
            RenderUI(selectedFolder, searchInput, replaceInput, currentPreviews, focusState, previewScrollOffset, previewPageSize, useRegex);

            // Handle keyboard input
            var key = Console.ReadKey(intercept: true);
            var (action, newFocus, scrollDelta) = RegexKeyboardHandler.HandleKey(
                key,
                focusState,
                currentPreviews.Count,
                previewPageSize,
                previewScrollOffset
            );

            // Process the action
            switch (action)
            {
                case RegexKeyboardHandler.KeyAction.SwitchToSearch:
                case RegexKeyboardHandler.KeyAction.SwitchToReplace:
                case RegexKeyboardHandler.KeyAction.SwitchToPreview:
                    focusState = newFocus;
                    previewScrollOffset = scrollDelta >= 0 ? scrollDelta : Math.Max(0, previewScrollOffset + scrollDelta);
                    break;

                case RegexKeyboardHandler.KeyAction.ScrollPreviewUp:
                    previewScrollOffset = Math.Max(0, previewScrollOffset - 1);
                    break;

                case RegexKeyboardHandler.KeyAction.ScrollPreviewDown:
                    previewScrollOffset = Math.Min(
                        Math.Max(0, currentPreviews.Count - previewPageSize),
                        previewScrollOffset + 1
                    );
                    break;

                case RegexKeyboardHandler.KeyAction.Apply:
                    if (currentPreviews.Any(p => p.WillChange))
                    {
                        var shouldApply = await ShowConfirmationModal(currentPreviews);
                        if (shouldApply)
                        {
                            return await ApplyChanges(currentPreviews);
                        }
                    }
                    break;

                case RegexKeyboardHandler.KeyAction.Cancel:
                    return false;

                case RegexKeyboardHandler.KeyAction.ShowHelp:
                    RegexPreviewRenderer.ShowHelp();
                    break;

                case RegexKeyboardHandler.KeyAction.ToggleMode:
                    useRegex = !useRegex;
                    lastPreviewUpdateTime = DateTime.MinValue; // Force update
                    break;

                case RegexKeyboardHandler.KeyAction.HandleInInput:
                    bool textChanged = HandleInputKey(key, focusState, searchInput, replaceInput);
                    if (textChanged)
                    {
                        lastPreviewUpdateTime = DateTime.MinValue; // Force update
                    }
                    break;
            }

            focusState = newFocus;
        }
    }

    private bool ShouldUpdatePreview(DateTime lastUpdate, int debounceMs, string searchText)
    {
        var timeSinceLastUpdate = (DateTime.Now - lastUpdate).TotalMilliseconds;
        return timeSinceLastUpdate >= debounceMs;
    }

    private bool HandleInputKey(ConsoleKeyInfo key, RegexKeyboardHandler.FocusState focus, CustomTextInput searchInput, CustomTextInput replaceInput)
    {
        if (focus == RegexKeyboardHandler.FocusState.SearchField)
        {
            return searchInput.HandleKey(key);
        }
        else if (focus == RegexKeyboardHandler.FocusState.ReplaceField)
        {
            return replaceInput.HandleKey(key);
        }
        return false;
    }

    private void RenderUI(
        FileSystemEntry folder,
        CustomTextInput searchInput,
        CustomTextInput replaceInput,
        List<RenamePreview> previews,
        RegexKeyboardHandler.FocusState focusState,
        int scrollOffset,
        int pageSize,
        bool useRegex)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(StyleGuide.CreateTitleRule("Regex Replace - Live Preview"));
        AnsiConsole.WriteLine();

        // Folder context
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Parent folder:[/] [bold]{Markup.Escape(folder.Name)}[/]");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Path:[/] {Markup.Escape(folder.FullPath)}");
        AnsiConsole.MarkupLine($"[{StyleGuide.Muted}]Mode:[/] [cyan]{(useRegex ? "Regex (.NET)" : "Literal")}[/] [grey](F2 to toggle)[/]");
        AnsiConsole.WriteLine();

        // Render components using helpers
        RegexPreviewRenderer.RenderInputFields(
            searchInput,
            replaceInput,
            focusState == RegexKeyboardHandler.FocusState.SearchField,
            focusState == RegexKeyboardHandler.FocusState.ReplaceField
        );
        AnsiConsole.WriteLine();

        RegexPreviewRenderer.RenderPreview(
            previews,
            scrollOffset,
            pageSize,
            focusState == RegexKeyboardHandler.FocusState.Preview
        );
        AnsiConsole.WriteLine();

        RegexPreviewRenderer.RenderStats(previews);
        AnsiConsole.WriteLine();

        RegexPreviewRenderer.RenderHotkeys();
    }

    private Task<bool> ShowConfirmationModal(List<RenamePreview> previews)
    {
        var willChange = previews.Count(p => p.WillChange && !p.HasConflict && !p.HasEmptyResult);
        var conflicts = previews.Count(p => p.HasConflict);
        var empty = previews.Count(p => p.HasEmptyResult);

        AnsiConsole.Clear();

        // Re-render preview
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(StyleGuide.PrimaryColor)
            .Expand();

        table.AddColumn(new TableColumn("").Width(2));
        table.AddColumn(new TableColumn("[bold]Before[/]").LeftAligned());
        table.AddColumn(new TableColumn("→").Width(3).Centered());
        table.AddColumn(new TableColumn("[bold]After[/]").LeftAligned());

        foreach (var preview in previews.Where(p => p.WillChange).Take(15))
        {
            var icon = preview.StatusIcon;
            var beforeText = Markup.Escape(preview.OriginalName);
            var afterText = RegexPreviewRenderer.FormatAfterText(preview);
            table.AddRow(icon, beforeText, "→", afterText);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Confirmation panel
        var confirmContent = new Markup(
            $"[bold]Ready to rename {willChange} folder(s)[/]\n\n" +
            (conflicts > 0 ? $"[orange1]⚠️  {conflicts} items will be skipped due to conflicts[/]\n" : "") +
            (empty > 0 ? $"[grey]{empty} items will be skipped (empty results)[/]\n" : "") +
            "\n" +
            $"[{StyleGuide.Muted}]This operation will rename folders on disk.[/]\n" +
            $"[{StyleGuide.Muted}]Make sure you have backups if needed.[/]"
        );

        var confirmPanel = StyleGuide.CreateConfirmationPanel("Confirm Rename Operation", confirmContent);
        AnsiConsole.Write(confirmPanel);
        AnsiConsole.WriteLine();

        var result = AnsiConsole.Confirm(
            $"[{StyleGuide.Primary}]Proceed with renaming {willChange} folder(s)?[/]",
            defaultValue: false
        );

        return Task.FromResult(result);
    }

    private async Task<bool> ApplyChanges(List<RenamePreview> previews)
    {
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Renaming folders...", async ctx =>
            {
                return await _renameService.ApplyRenamesAsync(previews);
            });

        AnsiConsole.WriteLine();

        if (result.Successful > 0)
        {
            AnsiConsole.Write(StyleGuide.Success($"Successfully renamed {result.Successful} folder(s)."));
            AnsiConsole.WriteLine();
        }

        if (result.Skipped > 0)
        {
            AnsiConsole.MarkupLine($"[{StyleGuide.WarningMarkup}]Skipped {result.Skipped} folder(s).[/]");
        }

        if (result.HasErrors)
        {
            AnsiConsole.Write(StyleGuide.Error($"Failed to rename {result.Failed} folder(s)."));
            AnsiConsole.WriteLine();

            foreach (var error in result.Errors.Take(5))
            {
                AnsiConsole.MarkupLine($"  [{StyleGuide.Muted}]• {error}[/]");
            }

            if (result.Errors.Count > 5)
            {
                AnsiConsole.MarkupLine($"  [{StyleGuide.Muted}]... and {result.Errors.Count - 5} more errors[/]");
            }
        }

        AnsiConsole.WriteLine();

        return result.Successful > 0;
    }
}
