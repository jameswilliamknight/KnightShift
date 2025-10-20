# KnightShift

> **Built with AI**: This project was created using [Claude Code v2.0.22](https://claude.com/claude-code) and Claude Sonnet 4.5 on October 20, 2025.

A powerful command-line TUI (Text User Interface) application for managing drives and organizing files in WSL (Windows Subsystem for Linux).

## Features

- **Smart Drive Detection**: Automatically detects both Linux block devices and Windows drives (via PowerShell integration)
- **Intelligent Filtering**: Filters out system drives (/dev/sd*) and empty drives to show only relevant options
- **Mount Management**: Mount unmounted external USB drives and Windows drives to WSL
- **Interactive File Browser**: Beautiful TUI with folder navigation
- **Batch Rename**: Regex-based folder renaming with live preview (similar to VS Code's Ctrl+H find/replace)
- **Folder Properties**: View folder statistics (size, file count, folder count)
- **Quick Actions**: Open in terminal, Open in VS Code
- **User Preferences**: Persistent settings stored in `~/.config/knightshift/settings.json` (follows XDG Base Directory Specification)
- **Single Drive Auto-Select**: Smart handling when only one drive is available with "don't ask again" option
- **Both Modes**: Interactive TUI and command-line arguments for automation

## Design Philosophy

KnightShift is built on core UX principles that prioritize discoverability, feedback, and user confidence:

### Always Show Available Actions
Users should never have to guess what they can do. Every screen displays:
- **Keyboard shortcuts** in a persistent hotkey panel at the bottom
- **Navigation hints** at the top of each screen (e.g., "Use ↑↓ arrows, ← to go back, → to enter folder")
- **Action menus** that list all possible operations for the current context
- **Context-sensitive help** (press F1 in regex mode for detailed help)

### Live Feedback
Operations provide real-time visual feedback:
- **Regex Replace**: Live preview updates as you type, showing exactly what will change
- **Match Highlighting**: Yellow background shows what the regex pattern matches
- **Result Preview**: Green background shows the replacement result
- **Empty Detection**: Gray boxes indicate empty results that will be skipped
- **Conflict Warning**: Orange text highlights naming conflicts before they happen
- **Debouncing**: 100ms delay prevents excessive updates while typing

### Visual Hierarchy
Clear visual indicators guide attention:
- **Focused Elements**: Bright blue borders indicate the active input field or section
- **Inactive Elements**: Muted gray borders show inactive sections
- **Selection Cursor**: Arrow (→) prefix shows the currently selected item
- **Color Coding**: Consistent color scheme (green for success, yellow for warnings, red for errors)
- **Status Icons**: ✅ for mounted, 💾 for Windows drives, 🔌 for USB drives

### Keyboard-First Navigation
Efficient keyboard controls throughout:
- **Arrow Keys**: ↑↓ to navigate, ←→ to move between fields or enter/exit folders
- **Tab**: Cycle through all interactive elements
- **Enter**: Confirm selection or apply changes
- **Escape**: Cancel operation or go back
- **F-Keys**: F1 for help, F2 to toggle modes
- **No Mouse Required**: All features accessible via keyboard

### Progressive Disclosure
Complexity is revealed gradually:
- **Smart Defaults**: Sensible default mount points, single-drive auto-select
- **Basic → Advanced**: Simple actions first, advanced regex features available but not overwhelming
- **Contextual Options**: Action menus show only relevant operations for the selected item

### Confirmation with Context
Destructive operations always confirm with full details:
- **Preview Before Action**: Show exactly what will change before applying
- **Summary Statistics**: Display count of affected items, conflicts, and skipped items
- **Full Context**: Confirmation panels show drive info, mount points, or rename previews
- **Safe Defaults**: Confirmation prompts default to "No" for risky operations
- **Seamless Flow**: No unnecessary "Press any key" interruptions - the app flows naturally with spinners for operations that need time

### Code Organization Philosophy
The codebase follows these principles:
- **Single Responsibility**: One top-level class per file when practical
- **Helper Extraction**: Complex logic broken into focused helper classes
- **File Size Limit**: Target ~200 lines per file to improve maintainability
- **CQRS-Lite Pattern**: Separation of queries (reads) and commands (writes)
- **Dependency Injection**: Services injected via constructors for testability

## Getting Started

### Prerequisites

- .NET 9.0 SDK or higher
- WSL (Windows Subsystem for Linux)
- `sudo` privileges for mounting drives
- Optional: VS Code for "Open in VS Code" feature

### Installation

1. Clone or download this repository
2. Run the start script (it will automatically build if needed):

```bash
./start
```

The start script is idempotent and will:
- Check for .NET SDK installation
- Restore NuGet packages (only if needed)
- Build the project in Release mode (only if needed or older than 30 minutes)
- Optionally create a global symlink for easy access
- Launch the application

To force a rebuild:
```bash
./start --build
```

### Creating a Global Symlink

If you want to run `knightshift` from anywhere without specifying the full path, you can create a global symlink:

**Option 1: Automatic (recommended)**
The `./start` script will prompt you to create a symlink automatically on first run.

**Option 2: Manual creation**
```bash
# Navigate to the project directory
cd /path/to/knightshift

# Create the symlink to the start script (requires sudo)
sudo ln -sf "$(pwd)/start" /usr/local/bin/knightshift
```

**Verify the symlink:**
```bash
which knightshift
# Should output: /usr/local/bin/knightshift

knightshift --help
# Should display the help message
```

### First Run

The easiest way to start is interactive mode:

```bash
# Using the start script
./start

# Or if installed globally as 'knightshift'
knightshift

# Or from the build directory
dotnet run --project src/KnightShift/KnightShift.csproj
```

You can also explicitly request interactive mode:

```bash
# Using start script
./start --interactive
./start -i

# Using global command
knightshift --interactive
knightshift -i
```

## Usage

### Interactive Mode

Interactive mode provides a guided experience through all features:

1. **Drive Selection**: Lists all unmounted drives and prompts you to select one to mount
2. **File Browser**: Navigate through folders using arrow keys
3. **Action Menu**: Press SPACE on a folder to open the action menu
4. **Quick Actions**: Perform operations like renaming, viewing properties, or opening in external tools

**Navigation Tips:**
- Use arrow keys to move up/down
- Press ENTER to select/navigate into folders
- Press SPACE to open action menu on folders
- Select "← Go Back" or ".." to navigate to parent directory
- Select "← Exit Browser" to quit

### Command-Line Mode

KnightShift also supports direct command-line operations. Examples below show both the global `knightshift` command (if symlink is installed) and the `./start` script:

#### Mount a Drive

```bash
# Using global command
knightshift mount --device /dev/sdb1
knightshift mount -d /dev/sdb1 -p /mnt/my-usb -t ext4

# Using start script
./start mount --device /dev/sdb1
./start mount -d /dev/sdb1 -p /mnt/my-usb -t ext4
```

Options:
- `-d, --device`: Device path (required, e.g., /dev/sdb1)
- `-p, --path`: Custom mount point (optional)
- `-t, --type`: Filesystem type (optional, e.g., ext4, ntfs, vfat)

#### Batch Rename Folders

Remove specific text from all immediate child folder names:

```bash
# Using global command
knightshift rename --path /mnt/usb/Photos --remove-text "IMG_"
knightshift rename -p /mnt/usb/Photos -r "IMG_" -y

# Using start script
./start rename -p /mnt/usb/Photos -r "IMG_"
./start rename -p /mnt/usb/Photos -r "IMG_" -y
```

Options:
- `-p, --path`: Parent folder path (required)
- `-r, --remove-text`: Text to remove from folder names (required)
- `-y, --yes`: Skip confirmation prompt

**Example:**
```bash
# Before:
# - IMG_2024_01_15
# - IMG_2024_01_16
# - IMG_2024_01_17

knightshift rename -p /mnt/usb/Photos -r "IMG_"
# or: ./start rename -p /mnt/usb/Photos -r "IMG_"

# After:
# - 2024_01_15
# - 2024_01_16
# - 2024_01_17
```

#### View Folder Statistics

```bash
# Using global command
knightshift stats --path /mnt/usb/Documents
knightshift stats -p /mnt/usb/Documents

# Using start script
./start stats -p /mnt/usb/Documents
```

Displays:
- Total size
- Number of files
- Number of folders

## Common Use Cases

### Organizing Camera SD Card

1. Mount the SD card:
   ```bash
   knightshift -i
   # or: ./start -i
   # Select your SD card from the list
   ```

2. Navigate to the DCIM folder in the file browser

3. Select the folder and choose "Remove Text from Folder Names"

4. Enter unwanted text like "IMG_" or "DSC" to clean up folder names

5. Preview changes and confirm

### Quick Mount and Browse

```bash
# Mount and start interactive browser
knightshift -i
# or: ./start -i
```

### Scripted Bulk Rename

```bash
# Rename without confirmation prompt (useful in scripts)
knightshift rename -p /mnt/usb/Photos -r "prefix_" -y
# or: ./start rename -p /mnt/usb/Photos -r "prefix_" -y
```

### Check Folder Sizes

```bash
# Quick stats on a directory
knightshift stats -p /mnt/usb/Downloads
# or: ./start stats -p /mnt/usb/Downloads
```

## Configuration

### User Settings

KnightShift stores user preferences in `~/.config/knightshift/settings.json` following the XDG Base Directory Specification.

**Current Settings:**
- `SkipSingleDriveConfirmation`: When set to `true`, automatically proceeds with mounting when only one drive is available (default: `false`)

**Settings Location:**
- Default: `~/.config/knightshift/settings.json`
- Can be overridden with `XDG_CONFIG_HOME` environment variable

**Manual Configuration:**
```bash
# View current settings
cat ~/.config/knightshift/settings.json

# Reset settings (delete the file)
rm ~/.config/knightshift/settings.json
```

### Drive Filtering

KnightShift automatically filters out:
- **System drives**: All `/dev/sd*` devices (sda, sdb, sdc, etc.) to prevent accidental system drive mounting
- **Empty drives**: Drives with 0 bytes (empty card readers, unmounted volumes)
- **Already mounted drives**: Drives currently mounted in WSL

This ensures only relevant external drives and Windows drives are shown.

### Sudo Setup

To avoid entering your password repeatedly for mount operations, you can configure passwordless sudo for specific commands:

```bash
sudo visudo
```

Add this line (replace `<username>` with your username):

```
<username> ALL=(ALL) NOPASSWD: /bin/mount, /bin/umount
```

**Warning**: Only do this if you understand the security implications.

## Versioning and Releases

KnightShift uses **fully automated versioning** via GitHub Actions:

- Every push to `main` triggers an automatic build and release
- Version numbers auto-increment in the pre-alpha series (e.g., v0.1.5 → v0.1.6-pre-alpha)
- Binaries are automatically built for all platforms (Linux x64/ARM64, Windows x64/ARM64, macOS x64/ARM64)
- Releases are published to GitHub with binaries and checksums

**To get the latest version:**
```bash
# View latest release info
gh release view --json tagName,name,publishedAt

# List recent releases
gh release list --limit 10

# Or visit: https://github.com/jameswilliamknight/KnightShift/releases
```

**Download binaries:**
- Pre-built binaries for all platforms are attached to each release
- SHA256 checksums included for verification
- No compilation required - download and run!

For full details on the automated versioning process, see `.cursor/rules/versioning.mdc`.

## Troubleshooting

### "No unmounted drives found"

- Check if your drive is connected: `lsblk`
- Ensure the drive isn't already mounted: `mount | grep /dev/sd`
- Try unplugging and reconnecting the drive

### "Permission denied" errors

- Ensure you have sudo privileges
- Run: `sudo -v` to refresh your sudo session
- Check file permissions on the target directory

### Drive not showing up in WSL

In Windows PowerShell (as Administrator):

```powershell
# List physical disks
wmic diskdrive list brief

# Mount a specific disk to WSL
wsl --mount \\.\PHYSICALDRIVE1
```

Replace `PHYSICALDRIVE1` with your actual drive number.

### "Could not open VS Code"

- Ensure VS Code is installed in WSL:
  ```bash
  code --version
  ```
- If not installed, run:
  ```bash
  # Install VS Code CLI in WSL
  curl -L https://code.visualstudio.com/sha/download?build=stable&os=cli-alpine-x64 -o vscode.tar.gz
  tar -xf vscode.tar.gz
  sudo mv code /usr/local/bin/
  ```

## Development

### Building from Source

```bash
# Quick start: Use the start script (handles restore and build automatically)
./start

# Force a rebuild
./start --build

# Or use dotnet commands directly:

# Restore packages
dotnet restore

# Build debug version
dotnet build

# Build release version
dotnet build -c Release

# Run without building
dotnet run --project src/KnightShift/KnightShift.csproj
```

### Project Structure

```
knightshift/
├── src/
│   └── KnightShift/
│       ├── Commands/          # Command-line argument definitions
│       ├── Models/            # Data models (DetectedDrive, UserSettings, etc.)
│       │   ├── DetectedDrive.cs
│       │   ├── WindowsVolume.cs
│       │   ├── LsblkModels.cs
│       │   └── ...
│       ├── Services/          # Business logic services
│       │   ├── ISettingsRepository.cs
│       │   ├── JsonSettingsRepository.cs
│       │   ├── DriveEnumerationService.cs
│       │   ├── WindowsDriveDetectionService.cs
│       │   ├── Helpers/       # Extracted helper classes
│       │   │   ├── WindowsVolumeParser.cs
│       │   │   ├── WslEnvironment.cs
│       │   │   ├── LinuxDeviceProcessor.cs
│       │   │   ├── ByteFormatter.cs
│       │   │   └── ...
│       │   └── ...
│       ├── UI/                # TUI pages and components
│       │   ├── StyleGuide.cs
│       │   ├── DriveSelectionPage.cs
│       │   ├── FileBrowserPage.cs
│       │   ├── TextRemovalPage.cs
│       │   ├── Helpers/       # UI helper classes
│       │   │   ├── RegexPreviewRenderer.cs
│       │   │   ├── RegexKeyboardHandler.cs
│       │   │   ├── CustomTextInput.cs
│       │   │   ├── MountedDriveActionHandler.cs
│       │   │   └── ...
│       │   └── ...
│       └── Program.cs         # Entry point
├── README.md
├── ONESHOT.md                 # Template for recreating this application
├── AGENTS.md                  # Instructions for AI coding agents
├── CLAUDE.md                  # Claude Code setup instructions
├── start                      # Idempotent build and run script
└── KnightShift.sln
```

**Key Architecture Patterns:**
- **Repository Pattern**: `ISettingsRepository` with JSON file implementation
- **Service Layer**: Business logic separated from UI
- **Helper Extraction Pattern**: Large files broken into focused ~200-line helper classes
- **Dependency Injection**: Constructor injection throughout
- **XDG Compliance**: Settings stored in `~/.config/knightshift/`
- **Clean Build**: Zero warnings, zero errors

## Contributing

This project is designed for personal use but contributions are welcome! Feel free to:
- Report bugs
- Suggest features
- Submit pull requests

## License

This project is provided as-is for personal and educational use.

## Acknowledgments

- Built with [Spectre.Console](https://spectreconsole.net/) for beautiful terminal UI
- Uses [CommandLineParser](https://github.com/commandlineparser/commandline) for argument parsing
- Designed for WSL and Linux environments
