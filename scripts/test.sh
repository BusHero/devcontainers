#!/bin/bash
# Smoke-tests a template that was brought up by scripts/build.sh.
# Runs templates/test/<id>/test.sh inside the running dev container,
# then removes the container and the temporary workspace.
TEMPLATE_ID="$1"
set -e

if [ -z "${TEMPLATE_ID}" ]; then
    echo "Usage: $0 <template-id>" >&2
    exit 1
fi

SRC_DIR="/tmp/${TEMPLATE_ID}"
ID_LABEL="test-container=${TEMPLATE_ID}"

cleanup() {
    echo "(*) Cleaning up"
    CONTAINERS=$(docker container ls -a -f "label=${ID_LABEL}" -q)
    if [ -n "${CONTAINERS}" ]; then
        docker rm -f ${CONTAINERS} > /dev/null
    fi
    rm -rf "${SRC_DIR}"
}
trap cleanup EXIT

if [ ! -d "${SRC_DIR}" ]; then
    echo "Template workspace '${SRC_DIR}' not found. Run scripts/build.sh ${TEMPLATE_ID} first." >&2
    exit 1
fi

if [ ! -f "${SRC_DIR}/test-project/test.sh" ]; then
    echo "No tests found for template '${TEMPLATE_ID}' (expected templates/test/${TEMPLATE_ID}/test.sh)." >&2
    exit 1
fi

echo "(*) Running smoke test for '${TEMPLATE_ID}'"
devcontainer exec \
    --workspace-folder "${SRC_DIR}" \
    --id-label "${ID_LABEL}" \
    bash -c 'set -e && cd test-project && bash ./test.sh' 2>&1
