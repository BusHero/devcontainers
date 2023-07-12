#!/bin/bash

set -e

source .devcontainer/assert.sh

assert_eq "$(devcontainer --version)" '0.39.0'
