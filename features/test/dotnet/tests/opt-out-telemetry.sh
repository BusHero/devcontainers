#!/bin/sh
set -e
if [ -z "$DOTNET_CLI_TELEMETRY_OPTOUT" ] ||
   { [ "$DOTNET_CLI_TELEMETRY_OPTOUT" != "1" ] && [ "$DOTNET_CLI_TELEMETRY_OPTOUT" != "true" ]; }; then
    echo "Error: DOTNET_CLI_TELEMETRY_OPTOUT must be set to '1' or 'true'."
    exit 1
fi

echo "DOTNET_CLI_TELEMETRY_OPTOUT is correctly set to '$DOTNET_CLI_TELEMETRY_OPTOUT'."
