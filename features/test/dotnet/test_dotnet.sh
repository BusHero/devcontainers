#!/bin/bash

cd $(dirname "$0")
source dev-container-features-test-lib

for script in tests/*.sh
do
	check "$script" bash $script
done

reportResults
