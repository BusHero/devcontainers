#!/bin/bash
set -e

cd $(dirname "$0")
source dev-container-features-test-lib

for script in tests/*.sh
do
	check "$script" . $script
done

reportResults
