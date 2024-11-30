#!/bin/sh
set -e

if grep -q "dotnet complete" ~/.bashrc ~/.zshrc 2>/dev/null; then
    echo "Dotnet completion is installed."
else
    echo "Dotnet completion is not installed."
    exit 1
fi
