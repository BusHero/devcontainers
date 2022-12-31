#!/bin/bash

cd $(dirname "$0")
source test-utils.sh

for script in tests/*.sh
do
	check "$script" . $script
done

reportResults
