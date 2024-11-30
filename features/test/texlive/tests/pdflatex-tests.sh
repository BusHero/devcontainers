#!/bin/sh
set -e

error=0

mkdir /tmp/pdflatex
pdflatex -output-directory=/tmp/pdflatex small2e >/dev/null
if [ ! -f /tmp/pdflatex/small2e.pdf ]; then
	echo "'pdflatex small2e' failed to create a pdf file"
	error=1
fi
rm -rf /tmp/pdflatex

if [ $error -ne 0 ]; then
	exit $error
fi
