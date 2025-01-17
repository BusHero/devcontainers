#!/bin/bash
TEMPLATE_ID="$1"
set -e

SRC_DIR="/tmp/${TEMPLATE_ID}"
echo "Running Smoke Test"

ID_LABEL="test-container=${TEMPLATE_ID}"
devcontainer exec --workspace-folder "${SRC_DIR}" --id-label ${ID_LABEL} bash -c 'set -e && if [ -f "test-project/test.sh" ]; then cd test-project && bash ./test.sh; else ls -a; fi' 2>&1

# Clean up
docker rm -f $(docker container ls -f "label=${ID_LABEL}" -q)
rm -rf "${SRC_DIR}"
