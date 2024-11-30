#!/bin/bash
set -e

source dev-container-features-test-lib

check "devcontainer installed" command -v devcontainer

reportResults
