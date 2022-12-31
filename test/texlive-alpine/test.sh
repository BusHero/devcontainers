#!/bin/bash

cd $(dirname "$0")
source test-utils.sh

check "bash" bash --version >/dev/null

reportResults
