#!/bin/sh
set -e

if ! command -v bash
then
	apk add --update bash
fi
