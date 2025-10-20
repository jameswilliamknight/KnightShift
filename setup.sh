#!/bin/bash

set -e  # Exit on error

echo "======================================"
echo "  KnightShift - Setup Script"
echo "======================================"
echo ""

# Check if .NET SDK is installed
echo "Checking for .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed."
    echo ""
    echo "Please install .NET 9.0 SDK or higher:"
    echo "  https://dotnet.microsoft.com/download"
    echo ""
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✓ Found .NET SDK version: $DOTNET_VERSION"
echo ""

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore KnightShift.sln
echo "✓ Packages restored"
echo ""

# Build in Release mode
echo "Building project in Release mode..."
dotnet build KnightShift.sln -c Release
echo "✓ Build successful"
echo ""

# Get the build output path
BUILD_PATH="src/KnightShift/bin/Release/net9.0/KnightShift.dll"

if [ ! -f "$BUILD_PATH" ]; then
    echo "❌ Error: Build output not found at $BUILD_PATH"
    exit 1
fi

echo "======================================"
echo "  Build Complete!"
echo "======================================"
echo ""
echo "You can now run the application:"
echo ""
echo "  cd $(pwd)"
echo "  dotnet run --project src/KnightShift/KnightShift.csproj"
echo ""
echo "Or run the compiled binary:"
echo "  dotnet $BUILD_PATH"
echo ""

# Ask if user wants to create a global symlink
read -p "Would you like to create a global symlink to run 'knightshift' from anywhere? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    SCRIPT_DIR=$(pwd)
    SYMLINK_PATH="/usr/local/bin/knightshift"

    # Create a wrapper script
    WRAPPER_SCRIPT="/tmp/knightshift-wrapper"
    cat > "$WRAPPER_SCRIPT" << EOF
#!/bin/bash
dotnet "$SCRIPT_DIR/$BUILD_PATH" "\$@"
EOF

    chmod +x "$WRAPPER_SCRIPT"

    # Install the wrapper
    echo "Creating symlink (requires sudo)..."
    sudo mv "$WRAPPER_SCRIPT" "$SYMLINK_PATH"
    sudo chmod +x "$SYMLINK_PATH"

    echo "✓ Symlink created at $SYMLINK_PATH"
    echo ""
    echo "You can now run: knightshift"
    echo ""
else
    echo "Skipping symlink creation."
    echo ""
fi

echo "======================================"
echo "  Setup Complete!"
echo "======================================"
echo ""
echo "Quick start:"
echo "  - Run interactive mode: dotnet run --project src/KnightShift/KnightShift.csproj"
echo "  - View help: dotnet run --project src/KnightShift/KnightShift.csproj -- --help"
echo ""
echo "For more information, see README.md"
echo ""
