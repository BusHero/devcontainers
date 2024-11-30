#!/bin/sh
set -e

apk add --update \
	npm \
	python3 \
	make \
	g++


if [ $VERSION ]; then
	VERSION=@${VERSION};
fi

npm install -g @devcontainers/cli$VERSION
