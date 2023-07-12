#!/bin/sh

apk update
apk upgrade
apk add dotnet7-sdk
dotnet tool install Nuke.GlobalTool --global
