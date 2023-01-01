#!/bin/sh

apk update \
	&& apk upgrade \
	&& apk add \
	curl \
	wget \
	perl-dev \
	alpine-sdk \
	texlive \
	biber \
	bash

curl -L http://cpanmin.us | perl - App::cpanminus \
	&& cpanm Log::Dispatch::File \
	&& cpanm File::HomeDir \
	&& cpanm Unicode::GCString \
	&& cpanm YAML::Tiny

wget https://mirrors.ctan.org/macros/latex/contrib/logreq.zip \
	&& unzip logreq.zip -d /usr/share/texmf-dist/tex/latex/contrib \
	&& mktexlsr
