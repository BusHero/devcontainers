#!/bin/sh

apk update
apk upgrade
apk add \
	alpine-sdk \
	texlive \
	biber \
	perl-app-cpanminus
	bash

cpanm Log::Dispatch::File
cpanm File::HomeDir
cpanm Unicode::GCString
cpanm YAML::Tiny

wget https://mirrors.ctan.org/macros/latex/contrib/logreq.zip \
	&& unzip logreq.zip -d /usr/share/texmf-dist/tex/latex/contrib \
	&& mktexlsr
