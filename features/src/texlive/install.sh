#!/bin/sh

apk update \
	&& apk upgrade \
	&& apk add \
	bash
