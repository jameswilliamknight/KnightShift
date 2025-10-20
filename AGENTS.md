# Instructions for AI Coding Agents

**Hello OpenCode, Claude Code, and other AI coding agents!** 👋

This file provides essential context for working on the **KnightShift** project.

## Project Overview

**KnightShift** is a .NET 9.0 TUI (Text User Interface) application for managing drives and organizing files in WSL (Windows Subsystem for Linux).

**Primary Language**: C# (.NET 9.0)
**UI Framework**: Spectre.Console
**Architecture**: Service pattern with dependency injection
**Platform**: Linux (WSL/Ubuntu)

## Quick Start for Agents

### 1. Essential Files to Read First

**Start here** to understand the project:

1. **`ONESHOT.md`** - The master template. This is a comprehensive self-replicating prompt that explains the entire architecture, implementation phases, patterns, and best practices. **READ THIS FIRST** if you're making significant changes.

2. **`README.md`** - User-facing documentation. Features, installation, usage examples.

3. **`src/KnightShift/Program.cs`** - Application entry point. Shows command structure and flow.

4. **`src/KnightShift/UI/InteractiveFlowController.cs`** - Main TUI orchestrator. See the `Create()` factory method for dependency graph.

### 2. Project Architecture

```
Clean Architecture with Service Pattern:
├── Models/              # POCOs (DetectedDrive, UserSettings, etc.)
│   ├── DetectedDrive.cs
│   ├── WindowsVolume.cs
│   └── LsblkModels.cs
├── Services/            # Business logic
│   ├── ICommandLineProvider       # Facade for Process execution
│   ├── ISettingsRepository        # Persistence interface
│   ├── DriveEnumerationService    # Drive detection
│   ├── WindowsDriveDetectionService  # PowerShell integration
│   └── Helpers/                   # Service helper classes
│       ├── WindowsVolumeParser.cs
│       ├── WslEnvironment.cs
│       ├── LinuxDeviceProcessor.cs
│       └── ByteFormatter.cs
└── UI/                  # Spectre.Console TUI components
    ├── StyleGuide.cs              # Centralized styling
    ├── DriveSelectionPage.cs      # Drive mounting UI
    ├── FileBrowserPage.cs         # File navigation UI
    ├── TextRemovalPage.cs         # Regex rename UI
    └── Helpers/                   # UI helper classes
        ├── RegexPreviewRenderer.cs
        ├── RegexKeyboardHandler.cs
        ├── CustomTextInput.cs
        └── MountedDriveActionHandler.cs
```

**Key Patterns:**
- Repository Pattern for settings (`ISettingsRepository` → `JsonSettingsRepository`)
- Dependency Injection via constructor parameters
- Facade Pattern for external dependencies (`ICommandLineProvider`)
- Helper Extraction Pattern for files >200 lines
- XDG Base Directory Specification compliance (`~/.config/knightshift/`)

**Current Build Status:**
- ✅ Zero warnings, zero errors
- ✅ All files under 330 lines (target: ~200 lines)
- ✅ 14+ helper classes extracted for better organization
- ✅ Clean async/await usage throughout

### 3. Important Conventions

#### Spectre.Console Markup

**CRITICAL**: Always escape user data with `Markup.Escape()`:

```csharp
// ❌ WRONG - Will crash if text contains []
AnsiConsole.MarkupLine($"Drive: {driveName}");

// ✅ CORRECT - Safe for all text
AnsiConsole.MarkupLine($"Drive: {Markup.Escape(driveName)}");
```

**Color Usage:**
- Use `Color` enum (e.g., `Color.DodgerBlue1`) in `Style` constructors
- Use string names (e.g., `"dodgerblue1"`) in markup text
- Never use custom `new Color(r, g, b)` objects

#### Drive Filtering

The app filters out:
- System drives: `/dev/sd*` (all SCSI/SATA drives)
- Empty drives: `SizeBytes == 0`
- Already mounted drives

This ensures only relevant external/Windows drives are shown.

#### Settings Storage

User preferences are stored in `~/.config/knightshift/settings.json` following XDG Base Directory Specification:

```csharp
// Always check XDG_CONFIG_HOME environment variable first
var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
    ?? Path.Combine(homeDir, ".config");
```

### 4. UI/UX Patterns & Implementation

This project follows strict UI/UX patterns that prioritize discoverability, feedback, and user confidence. **Follow these patterns when adding or modifying UI components.**

#### Core Principle: "Always Show Available Actions"

**Users should never have to guess what they can do.** Implement this by:

1. **Persistent Hotkey Panels**: Always display keyboard shortcuts at the bottom of every screen
   ```csharp
   var hotkeys = StyleGuide.CreateHotkeyPanel(
       $"[{StyleGuide.Primary}]↑↓:[/] Navigate  " +
       $"[{StyleGuide.Primary}]Enter:[/] Select  " +
       $"[{StyleGuide.Primary}]Esc:[/] Cancel"
   );
   AnsiConsole.Write(hotkeys);
   ```

2. **Navigation Hints**: Display context-specific instructions at the top
   ```csharp
   AnsiConsole.MarkupLine($"[{StyleGuide.PrimaryColor}]Navigate:[/] Use ↑↓ arrows, ← to go back, → to enter folder");
   ```

3. **Contextual Help**: Provide F1 help screens with detailed instructions
   ```csharp
   case ConsoleKey.F1:
       ShowHelpScreen(); // Full help panel with examples
       break;
   ```

#### Live Feedback Pattern

**Provide real-time visual feedback as users type or interact:**

```csharp
// Debouncing for performance (100ms recommended)
private DateTime _lastPreviewUpdate = DateTime.MinValue;
const int debounceMs = 100;

if ((DateTime.Now - _lastPreviewUpdate).TotalMilliseconds >= debounceMs)
{
    // Update preview
    currentPreviews = _service.GeneratePreview(searchText, replaceText);
    _lastPreviewUpdate = DateTime.Now;
}
```

**Visual Highlighting:**
- **Yellow background** (`[black on yellow]text[/]`): Matched text in "before" state
- **Green background** (`[black on green]text[/]`): Successful result in "after" state
- **Gray box** (`[grey on grey]□[/]`): Empty/invalid results that will be skipped
- **Orange** (`[orange1]⚠️  text[/]`): Warnings or conflicts

#### Visual Hierarchy & Focus States

**Use consistent visual indicators for focus:**

```csharp
// Active/focused element
var activePanel = new Panel(content)
{
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(StyleGuide.PrimaryColor),  // Bright blue
    Header = new PanelHeader("[bold dodgerblue1]► Active Section[/]")
};

// Inactive element
var inactivePanel = new Panel(content)
{
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(StyleGuide.MutedColor),  // Gray
    Header = new PanelHeader("Inactive Section")
};
```

**Selection Indicators:**
- Use `→` prefix for currently selected item
- Highlight selected items with `StyleGuide.Primary` color
- Dim non-selected items with `"white"` or `StyleGuide.Muted`

#### Dual Input Field Pattern

**For complex input forms (like regex replace), implement dual visible fields:**

```csharp
var searchInput = new CustomTextInput(maxLength: 100);
var replaceInput = new CustomTextInput(maxLength: 100);
var focusState = FocusState.SearchField;

while (true)
{
    // Render both fields (both always visible)
    RenderInputFields(searchInput, replaceInput,
        focusState == FocusState.SearchField,
        focusState == FocusState.ReplaceField);

    // Handle Up/Down to switch fields
    var key = Console.ReadKey(intercept: true);
    switch (key.Key)
    {
        case ConsoleKey.UpArrow:
            focusState = FocusState.SearchField;
            break;
        case ConsoleKey.DownArrow:
            focusState = FocusState.ReplaceField;
            break;
    }
}
```

#### Custom Text Input Implementation

**Character-by-character input with cursor management:**

```csharp
public class CustomTextInput
{
    private string _text = "";
    private int _cursorPosition = 0;

    public bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                _cursorPosition = Math.Max(0, _cursorPosition - 1);
                return false; // No text change

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    return true; // Text changed
                }
                break;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    _text = _text.Insert(_cursorPosition, key.KeyChar.ToString());
                    _cursorPosition++;
                    return true; // Text changed
                }
                break;
        }
        return false;
    }

    public string GetDisplayText(bool showCursor)
    {
        if (!showCursor) return _text;
        return _text.Insert(_cursorPosition, "█").Remove(_cursorPosition + 1, 1);
    }
}
```

#### Keyboard Navigation Patterns

**Standard keyboard shortcuts across all screens:**

| Key | Action | Context |
|-----|--------|---------|
| `↑↓` | Navigate items / Switch fields | Lists and forms |
| `←→` | Go back / Enter folder | File browser |
| `←→` | Move cursor | Text input |
| `Tab` | Cycle through fields | Forms |
| `Enter` | Select / Apply | Universal |
| `Esc` | Cancel / Go back | Universal |
| `F1` | Show help | Context-sensitive |
| `F2` | Toggle mode | Where applicable |

**Implementation Example:**

```csharp
public static (KeyAction action, FocusState newFocus) HandleKey(
    ConsoleKeyInfo key, FocusState currentFocus)
{
    switch (key.Key)
    {
        case ConsoleKey.UpArrow:
            return currentFocus == FocusState.SearchField
                ? (KeyAction.SwitchToPreview, FocusState.Preview)
                : (KeyAction.ScrollUp, currentFocus);

        case ConsoleKey.F1:
            return (KeyAction.ShowHelp, currentFocus);

        case ConsoleKey.Escape:
            return (KeyAction.Cancel, currentFocus);
    }
}
```

#### Confirmation Modals with Context

**Always show what will happen before destructive operations:**

```csharp
private async Task<bool> ShowConfirmationModal(List<RenamePreview> previews)
{
    var willChange = previews.Count(p => p.WillChange && !p.HasConflict);
    var conflicts = previews.Count(p => p.HasConflict);

    // Show preview table (first 15 items)
    var table = new Table()
        .AddColumn("Before")
        .AddColumn("→")
        .AddColumn("After");

    foreach (var preview in previews.Take(15))
    {
        table.AddRow(
            Markup.Escape(preview.OriginalName),
            "→",
            FormatAfterText(preview)  // With colors
        );
    }

    AnsiConsole.Write(table);

    // Show statistics and warnings
    var confirmPanel = StyleGuide.CreateConfirmationPanel(
        "Confirm Operation",
        new Markup(
            $"[bold]Ready to rename {willChange} folder(s)[/]\n\n" +
            (conflicts > 0 ? $"[orange1]⚠️  {conflicts} conflicts will be skipped[/]\n" : "") +
            $"[{StyleGuide.Muted}]This operation will rename folders on disk.[/]"
        )
    );

    AnsiConsole.Write(confirmPanel);

    return AnsiConsole.Confirm(
        $"Proceed with renaming {willChange} folder(s)?",
        defaultValue: false  // Safe default
    );
}
```

#### Helper Class Extraction Pattern

**Keep files under ~200 lines by extracting helpers:**

**Example Refactoring:**
```
TextRemovalPage.cs (524 lines) → Refactor to:
├── TextRemovalPage.cs (278 lines) - Main page logic
├── Helpers/RegexPreviewRenderer.cs (258 lines) - All rendering
├── Helpers/RegexKeyboardHandler.cs (109 lines) - Keyboard logic
└── Helpers/CustomTextInput.cs (126 lines) - Input handling
```

**Current Helper Classes in KnightShift:**

Services/Helpers/:
- `WindowsVolumeParser.cs` - Parses PowerShell Get-Volume JSON output
- `WslEnvironment.cs` - WSL detection and Windows drive mount detection
- `LinuxDeviceProcessor.cs` - Processes lsblk device output
- `ByteFormatter.cs` - Formats bytes to human-readable sizes

UI/Helpers/:
- `RegexPreviewRenderer.cs` - Renders regex preview tables with highlighting
- `RegexKeyboardHandler.cs` - Handles keyboard input for regex mode
- `CustomTextInput.cs` - Character-by-character text input with cursor
- `MountedDriveActionHandler.cs` - Handles actions for mounted drives
- `FolderActionHandler.cs` - Folder action menu constants
- (More as needed)

**Helper Naming Convention:**
- `*Renderer.cs`: Static classes with `Render*()` methods (e.g., RegexPreviewRenderer)
- `*Handler.cs`: Static classes with `Handle*()` methods (e.g., RegexKeyboardHandler, MountedDriveActionHandler)
- `*Parser.cs`: Static classes for parsing external data (e.g., WindowsVolumeParser)
- `*Processor.cs`: Static classes for processing collections (e.g., LinuxDeviceProcessor)
- `*Formatter.cs`: Static classes for formatting output (e.g., ByteFormatter)
- `*Validator.cs`: Static classes with `Validate*()` methods
- `*Helper.cs`: General utilities
- `*Environment.cs`: Static classes for environment detection (e.g., WslEnvironment)

**Usage with Static Imports:**

```csharp
using static KnightShift.UI.Helpers.FolderActionHandler;

// Now can use constants directly
switch (action)
{
    case ActionRemoveText:  // From FolderActionHandler
        await _textRemovalPage.ShowAsync(entry);
        break;
}
```

#### StyleGuide Usage

**Always use StyleGuide for consistent styling:**

```csharp
// Color CONSTANTS (for use in markup strings)
StyleGuide.Primary           // "dodgerblue1" (string for markup)
StyleGuide.SuccessMarkup     // "green" (string for markup)
StyleGuide.ErrorMarkup       // "red" (string for markup)
StyleGuide.WarningMarkup     // "orange1" (string for markup)
StyleGuide.Muted             // "grey" (string for markup)

// Color ENUMS (for use in Style constructors)
StyleGuide.PrimaryColor      // Color.DodgerBlue1 (Color enum for styles)
StyleGuide.SuccessColor      // Color.Green
StyleGuide.WarningColor      // Color.Orange1
StyleGuide.ErrorColor        // Color.Red

// Factory Methods
StyleGuide.CreateTitleRule("Page Title")
StyleGuide.CreateInputPanel("Label", "content", isActive: true)
StyleGuide.CreateHotkeyPanel("hotkey text")
StyleGuide.CreateConfirmationPanel("Title", content)

// Message helper METHODS (automatically escape user data)
StyleGuide.Success("Success message")   // Auto-escapes with Markup.Escape()
StyleGuide.Warning("Warning message")   // Auto-escapes with Markup.Escape()
StyleGuide.Error("Error message")       // Auto-escapes with Markup.Escape()
StyleGuide.Info("Info message")         // Auto-escapes with Markup.Escape()
```

**CRITICAL: Don't confuse methods with constants!**

```csharp
// ❌ WRONG - StyleGuide.Success is a METHOD, not a constant
// This will cause "malformed markup tag" errors!
new Markup($"[{StyleGuide.Success}]Thank you![/]")

// ✅ CORRECT - Use StyleGuide.SuccessMarkup (the constant)
new Markup($"[{StyleGuide.SuccessMarkup}]Thank you![/]")

// ✅ OR BETTER - Use the method directly (it handles escaping)
StyleGuide.Success("Thank you!")
```

**Note**: The message helpers (`Success()`, `Warning()`, `Error()`, `Info()`) automatically escape their input with `Markup.Escape()`, so you can safely pass user-generated text (like filenames with `[` or `]`) directly to them.

#### Seamless Flow - No Interruptions

**IMPORTANT: Never add "Press any key to continue" prompts.**

The application follows a seamless flow principle:
- Operations that require time use `AnsiConsole.Status()` with spinners
- Success/error messages are displayed briefly without requiring user input
- Users navigate naturally through menus and actions
- The only prompts should be actual choices or confirmations for destructive operations

**❌ NEVER do this:**
```csharp
AnsiConsole.MarkupLine("Operation complete!");
AnsiConsole.MarkupLine("Press any key to continue...");
Console.ReadKey(true);  // ❌ WRONG - interrupts flow
```

**✅ DO this instead:**
```csharp
// For operations with spinners:
var result = await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Processing...", async ctx => {
        return await DoWork();
    });

AnsiConsole.Write(StyleGuide.Success("Operation complete!"));
// Continue immediately - no pause
```

#### Testing UI Changes

**When adding new UI features, verify:**

1. ✅ All keyboard shortcuts are documented in hotkey panel
2. ✅ Navigation hints explain how to use the screen
3. ✅ Focused element has bright blue border
4. ✅ User data is escaped with `Markup.Escape()`
5. ✅ Confirmation modals show exactly what will change
6. ✅ F1 help works (if applicable)
7. ✅ Destructive operations default to "No"
8. ✅ File is under ~200 lines (extract helpers if needed)
9. ✅ No "Press any key to continue" prompts anywhere

### 5. Building & Testing

```bash
# Build and run
./start

# Force rebuild
./start --build

# Run tests (if applicable)
dotnet test

# Check for issues
dotnet build --verbosity normal
```

### 5. WSL/Windows Integration

This app bridges WSL and Windows:

- **Linux drives**: Detected via `lsblk -J` (JSON output)
- **Windows drives**: Detected via `powershell.exe` calling `Get-Volume`
- **Mounting**: Uses `drvfs` filesystem type for Windows drives
- **WSL Detection**: Checks `/proc/version` for "microsoft" or "WSL"

### 6. Common Tasks

**Adding a new setting:**
1. Add property to `Models/UserSettings.cs`
2. Use in UI via `ISettingsRepository.LoadAsync()` and `SaveAsync()`
3. Settings auto-persist to JSON

**Adding a new UI page:**
1. Create class in `UI/` folder
2. Inject dependencies via constructor
3. Add to `InteractiveFlowController.Create()` factory

**Adding a new service:**
1. Create interface `IYourService.cs` in `Services/`
2. Create implementation `YourService.cs`
3. Inject where needed via constructor

### 7. Known Gotchas

- **WSL2 uses `9p` filesystem**, not `drvfs` for Windows mounts
- **Markup.Escape** is non-negotiable for user data in Spectre.Console
- **Don't filter all drives** - only filter system drives and 0-byte drives
- **Always use `async/await`** - no blocking code in UI
- **XDG_CONFIG_HOME** can be overridden by users - respect it

### 8. Testing the Full Flow

1. Unmount a Windows drive in WSL: `sudo umount /mnt/j`
2. Run the app: `./start`
3. Verify drive appears (should show J: but not sda/sdb/sdc)
4. Mount the drive
5. Browse files
6. Test regex rename with live preview

### 9. For OpenCode Specifically

This project was originally built with **Claude Code** but is designed to be agent-agnostic. The architecture follows clean code principles and should be straightforward to work with.

**Suggested workflow:**
1. Read `ONESHOT.md` to understand the template
2. Check `README.md` for current features
3. Review `src/KnightShift/UI/InteractiveFlowController.cs` for dependency graph
4. Make targeted changes using the established patterns
5. Test with `./start --build`

---

## Additional Resources

- [Spectre.Console Documentation](https://spectreconsole.net/)
- [XDG Base Directory Spec](https://specifications.freedesktop.org/basedir-spec/latest/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)

**Questions?** The code is well-documented with XML comments. Use "Go to Definition" liberally.

**Happy coding! 🚀**
