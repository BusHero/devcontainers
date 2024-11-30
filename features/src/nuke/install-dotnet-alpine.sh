#!/bin/sh
set -e

apk update
apk upgrade
apk add dotnet8-sdk
