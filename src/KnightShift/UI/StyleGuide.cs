using Spectre.Console;
using Spectre.Console.Rendering;

namespace KnightShift.UI;

/// <summary>
/// Centralized style definitions for consistent UI appearance.
///
/// <para><b>IMPORTANT:</b> Don't confuse methods with constants!</para>
/// <para>
/// ❌ WRONG: <c>new Markup($"[{StyleGuide.Success}]text[/]")</c> - Success is a METHOD<br/>
/// ✅ RIGHT: <c>new Markup($"[{StyleGuide.SuccessMarkup}]text[/]")</c> - SuccessMarkup is a CONSTANT<br/>
/// ✅ BETTER: <c>StyleGuide.Success("text")</c> - Use the method directly (handles escaping)
/// </para>
/// </summary>
public static class StyleGuide
{
    // Color markup strings (for use in markup text within string interpolation)
    // Use these constants when building markup strings like: $"[{StyleGuide.SuccessMarkup}]text[/]"
    public const string Primary = "dodgerblue1";
    public const string SuccessMarkup = "green";
    public const string ErrorMarkup = "red";
    public const string WarningMarkup = "orange1";
    public const string Accent = "mediumpurple";
    public const string Muted = "grey";
    public const string Highlight = "yellow";

    // Color scheme using Spectre.Console Color enum
    public static readonly Color PrimaryColor = Color.DodgerBlue1;
    public static readonly Color SuccessColor = Color.Green;
    public static readonly Color ErrorColor = Color.Red;
    public static readonly Color WarningColor = Color.Orange1;
    public static readonly Color AccentColor = Color.MediumPurple;
    public static readonly Color MutedColor = Color.Grey;
    public static readonly Color HighlightColor = Color.Yellow;

    // Text styles
    public static readonly Style PrimaryStyle = new(PrimaryColor);
    public static readonly Style SuccessStyle = new(SuccessColor, decoration: Decoration.Bold);
    public static readonly Style ErrorStyle = new(ErrorColor, decoration: Decoration.Bold);
    public static readonly Style WarningStyle = new(WarningColor);
    public static readonly Style AccentStyle = new(AccentColor);
    public static readonly Style MutedStyle = new(MutedColor);
    public static readonly Style TitleStyle = new(PrimaryColor, decoration: Decoration.Bold);
    public static readonly Style HighlightStyle = new(Color.Black, HighlightColor, Decoration.Bold);

    // Regex match highlighting styles
    public static readonly Style MatchHighlightStyle = new(Color.Black, HighlightColor, Decoration.Bold);
    public static readonly Style ResultHighlightStyle = new(Color.Black, SuccessColor);
    public static readonly Style EmptyResultStyle = new(MutedColor, Color.Grey, Decoration.Dim);

    // Input field border styles
    public static readonly Style ActiveInputBorder = new(PrimaryColor);
    public static readonly Style InactiveInputBorder = new(MutedColor);

    // Box styles for panels
    public static BoxBorder FloatingPanelBorder => BoxBorder.Double;
    public static BoxBorder StandardPanelBorder => BoxBorder.Rounded;
    public static BoxBorder SubtlePanelBorder => BoxBorder.Square;

    // Symbols
    public const string CheckMark = "✓";
    public const string CrossMark = "✗";
    public const string Arrow = "▶";
    public const string Bullet = "■";
    public const string WarningIcon = "⚠️";
    public const string InfoIcon = "ℹ️";
    public const string Folder = "📁";
    public const string File = "📄";
    public const string Drive = "💾";
    public const string USB = "🔌";

    /// <summary>
    /// Creates a styled panel with floating appearance (for menus)
    /// </summary>
    public static Panel CreateFloatingPanel(string title, IRenderable content)
    {
        return new Panel(content)
        {
            Header = new PanelHeader(title, Justify.Center),
            Border = FloatingPanelBorder,
            BorderStyle = new Style(Color.DodgerBlue1),
            Padding = new Padding(2, 1),
            Expand = true
        };
    }

    /// <summary>
    /// Creates a standard panel with rounded borders
    /// </summary>
    public static Panel CreateStandardPanel(string title, IRenderable content)
    {
        return new Panel(content)
        {
            Header = new PanelHeader(title),
            Border = StandardPanelBorder,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0),
            Expand = true
        };
    }

    /// <summary>
    /// Creates a title/header rule (centered for better visual balance)
    /// </summary>
    public static Rule CreateTitleRule(string title)
    {
        return new Rule($"[bold]{title}[/]")
        {
            Style = TitleStyle,
            Justification = Justify.Center
        };
    }

    /// <summary>
    /// Creates a separator rule
    /// </summary>
    public static Rule CreateSeparator()
    {
        return new Rule()
        {
            Style = MutedStyle
        };
    }

    /// <summary>
    /// Formats a success message with checkmark icon and bold styling.
    /// Use this to create a complete styled Markup object, not for string interpolation.
    /// For markup colors in strings, use <see cref="SuccessMarkup"/> instead.
    /// </summary>
    /// <param name="message">The message to format (will be escaped automatically)</param>
    /// <returns>A styled Markup object ready to display</returns>
    public static Markup Success(string message)
    {
        return new Markup($"[green]{CheckMark}[/] [bold green]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Formats an error message with cross icon and bold styling.
    /// Use this to create a complete styled Markup object, not for string interpolation.
    /// For markup colors in strings, use <see cref="ErrorMarkup"/> instead.
    /// </summary>
    /// <param name="message">The message to format (will be escaped automatically)</param>
    /// <returns>A styled Markup object ready to display</returns>
    public static Markup Error(string message)
    {
        return new Markup($"[red]{CrossMark}[/] [bold red]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Formats a warning message with warning icon and styling.
    /// Use this to create a complete styled Markup object, not for string interpolation.
    /// For markup colors in strings, use <see cref="WarningMarkup"/> instead.
    /// </summary>
    /// <param name="message">The message to format (will be escaped automatically)</param>
    /// <returns>A styled Markup object ready to display</returns>
    public static Markup Warning(string message)
    {
        return new Markup($"[orange1]{WarningIcon}[/] [orange1]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Formats an info message with info icon and styling.
    /// Use this to create a complete styled Markup object, not for string interpolation.
    /// </summary>
    /// <param name="message">The message to format (will be escaped automatically)</param>
    /// <returns>A styled Markup object ready to display</returns>
    public static Markup Info(string message)
    {
        return new Markup($"[blue]{InfoIcon}[/] [blue]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Creates an input panel with active/inactive styling
    /// </summary>
    public static Panel CreateInputPanel(string label, string content, bool isActive)
    {
        var borderStyle = isActive ? ActiveInputBorder : InactiveInputBorder;
        var contentMarkup = new Markup($"[{(isActive ? Primary : Muted)}]{Markup.Escape(content)}[/]");

        return new Panel(contentMarkup)
        {
            Header = new PanelHeader($" {label} "),
            Border = BoxBorder.Rounded,
            BorderStyle = borderStyle,
            Padding = new Padding(1, 0),
            Expand = true  // Fill available width
        };
    }

    /// <summary>
    /// Creates a hotkey display panel
    /// </summary>
    public static Panel CreateHotkeyPanel(string content)
    {
        return new Panel(new Markup(content))
        {
            Header = new PanelHeader(" Actions "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0),
            Expand = true  // Fill available width
        };
    }

    /// <summary>
    /// Creates a confirmation panel with warning styling
    /// </summary>
    public static Panel CreateConfirmationPanel(string title, IRenderable content)
    {
        return new Panel(content)
        {
            Header = new PanelHeader($" {title} ", Justify.Center),
            Border = BoxBorder.Double,
            BorderStyle = new Style(WarningColor),
            Padding = new Padding(2, 1),
            Expand = true
        };
    }
}
