#!/bin/sh

apk update \
	&& apk upgrade \
	&& apk add \
	perl-dev \
	curl \
	wget \
	alpine-sdk \
	bash

curl -L http://cpanmin.us | perl - App::cpanminus
