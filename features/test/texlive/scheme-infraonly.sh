#!/bin/bash

ACTUALSCHEME=`grep selected_scheme /opt/texlive/texdir/tlpkg/texlive.profile`

if [ "$ACTUALSCHEME" = 'selected_scheme scheme-infraonly' ]; then
	exit 0 # success
else
	exit 1 # fail
fi
