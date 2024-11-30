#!/bin/bash
set -e

error=0

mkdir /tmp/lualatex
lualatex --output-directory=/tmp/lualatex small2e >/dev/null
if [ ! -f /tmp/lualatex/small2e.pdf ]; then
	echo "'lualatex small2e' failed to create a pdf file"
	error=1
fi
rm -rf /tmp/lualatex

if [ $error -ne 0 ]; then
	exit $error
fi
