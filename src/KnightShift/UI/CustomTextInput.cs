using Spectre.Console;

namespace KnightShift.UI;

/// <summary>
/// Custom text input handler with character-by-character control
/// Supports cursor movement, editing, and visual feedback
/// </summary>
public class CustomTextInput
{
    private string _text;
    private int _cursorPosition;
    private readonly int _maxLength;

    public string Text => _text;
    public int CursorPosition => _cursorPosition;

    public CustomTextInput(string initialText = "", int maxLength = 200)
    {
        _text = initialText ?? "";
        _cursorPosition = _text.Length;
        _maxLength = maxLength;
    }

    /// <summary>
    /// Handles a key press and updates the input state
    /// Returns true if the input was modified
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (_cursorPosition > 0)
                {
                    _cursorPosition--;
                    return false; // No text change, just cursor
                }
                break;

            case ConsoleKey.RightArrow:
                if (_cursorPosition < _text.Length)
                {
                    _cursorPosition++;
                    return false;
                }
                break;

            case ConsoleKey.Home:
                _cursorPosition = 0;
                return false;

            case ConsoleKey.End:
                _cursorPosition = _text.Length;
                return false;

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    return true;
                }
                break;

            case ConsoleKey.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    return true;
                }
                break;

            default:
                // Regular character input
                if (!char.IsControl(key.KeyChar) && _text.Length < _maxLength)
                {
                    _text = _text.Insert(_cursorPosition, key.KeyChar.ToString());
                    _cursorPosition++;
                    return true;
                }
                break;
        }

        return false;
    }

    /// <summary>
    /// Gets the display text with a visible cursor
    /// </summary>
    public string GetDisplayText(bool showCursor)
    {
        if (!showCursor || _text.Length == 0)
        {
            return _text.Length == 0 ? " " : _text;
        }

        // Insert cursor character at current position
        if (_cursorPosition >= _text.Length)
        {
            return _text + "█";
        }
        else
        {
            return _text.Insert(_cursorPosition, "█").Remove(_cursorPosition + 1, 1);
        }
    }

    /// <summary>
    /// Sets the text and moves cursor to end
    /// </summary>
    public void SetText(string text)
    {
        _text = text ?? "";
        _cursorPosition = _text.Length;
    }

    /// <summary>
    /// Clears the input
    /// </summary>
    public void Clear()
    {
        _text = "";
        _cursorPosition = 0;
    }
}
