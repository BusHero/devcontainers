#!/bin/sh
set -e

apk update
apk upgrade
apk add \
	perl-app-cpanminus \
	bash
