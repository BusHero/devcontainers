#!/bin/bash

source dev-container-features-test-lib

check "devcontainer installed" bash -c "command -v devcontainer"

reportResults
