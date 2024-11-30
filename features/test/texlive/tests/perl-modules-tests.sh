#!/bin/sh
set -e
errors=0
modules='File::HomeDir
 Log::Dispatch::File
 Unicode::GCString
 YAML::Tiny'
testfile=/tmp/test-perl-module.pl

for module in $modules
do
	echo "use $module;" > $testfile
	error=0
	( perl $testfile >/dev/null ) || error=$?
	if [ $error -ne 0 ]; then
		errors=`expr $errors + 1`
	fi
done

if [ $errors -ne 0 ]; then
	exit $errors
fi
