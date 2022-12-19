#!/bin/sh
errors=0

for script in ./tests/*.sh
do
	. $script
	if [ $? -ne 0 ]; then
		echo $errors
		errors=`expr $errors + 1`
	fi
done

exit $errors
