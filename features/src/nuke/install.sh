#!/bin/sh

if command -v apt; then
	sh install-dotnet-debian.sh
elif command -v apk; then
	sh install-dotnet-alpine.sh
fi

dotnet tool install Nuke.GlobalTool --global
