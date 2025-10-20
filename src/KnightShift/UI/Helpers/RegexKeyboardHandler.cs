namespace KnightShift.UI.Helpers;

/// <summary>
/// Handles keyboard input for the regex replace UI
/// </summary>
public static class RegexKeyboardHandler
{
    public enum FocusState
    {
        SearchField,
        ReplaceField,
        Preview
    }

    public enum KeyAction
    {
        None,
        SwitchToSearch,
        SwitchToReplace,
        SwitchToPreview,
        ScrollPreviewUp,
        ScrollPreviewDown,
        Apply,
        Cancel,
        ShowHelp,
        ToggleMode,
        HandleInInput
    }

    /// <summary>
    /// Determines what action should be taken based on the key press and current focus
    /// </summary>
    public static (KeyAction action, FocusState newFocus, int scrollDelta) HandleKey(
        ConsoleKeyInfo key,
        FocusState currentFocus,
        int previewCount,
        int pageSize,
        int currentScrollOffset = 0)
    {
        var newFocus = currentFocus;
        var scrollDelta = 0;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (currentFocus == FocusState.SearchField)
                {
                    // Wrap to Preview at bottom
                    return (KeyAction.SwitchToPreview, FocusState.Preview, Math.Max(0, previewCount - pageSize));
                }
                else if (currentFocus == FocusState.ReplaceField)
                {
                    return (KeyAction.SwitchToSearch, FocusState.SearchField, 0);
                }
                else if (currentFocus == FocusState.Preview)
                {
                    // If at top of preview, go back to ReplaceField
                    if (currentScrollOffset == 0)
                    {
                        return (KeyAction.SwitchToReplace, FocusState.ReplaceField, 0);
                    }
                    return (KeyAction.ScrollPreviewUp, FocusState.Preview, -1);
                }
                break;

            case ConsoleKey.DownArrow:
                if (currentFocus == FocusState.SearchField)
                {
                    return (KeyAction.SwitchToReplace, FocusState.ReplaceField, 0);
                }
                else if (currentFocus == FocusState.ReplaceField)
                {
                    return (KeyAction.SwitchToPreview, FocusState.Preview, 0);
                }
                else if (currentFocus == FocusState.Preview)
                {
                    return (KeyAction.ScrollPreviewDown, FocusState.Preview, 1);
                }
                break;

            case ConsoleKey.Tab:
                newFocus = currentFocus switch
                {
                    FocusState.SearchField => FocusState.ReplaceField,
                    FocusState.ReplaceField => FocusState.Preview,
                    FocusState.Preview => FocusState.SearchField,
                    _ => FocusState.SearchField
                };
                scrollDelta = (newFocus == FocusState.Preview) ? 0 : 0;
                return (KeyAction.None, newFocus, scrollDelta);

            case ConsoleKey.Enter:
                return (KeyAction.Apply, currentFocus, 0);

            case ConsoleKey.Escape:
                return (KeyAction.Cancel, currentFocus, 0);

            case ConsoleKey.F1:
                return (KeyAction.ShowHelp, currentFocus, 0);

            case ConsoleKey.F2:
                return (KeyAction.ToggleMode, currentFocus, 0);

            default:
                // Pass to input field
                if (currentFocus == FocusState.SearchField || currentFocus == FocusState.ReplaceField)
                {
                    return (KeyAction.HandleInInput, currentFocus, 0);
                }
                break;
        }

        return (KeyAction.None, currentFocus, 0);
    }
}
