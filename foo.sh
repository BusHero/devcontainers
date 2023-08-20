#!/bin/bash
commit=$1
path=$2

git diff $commit --name-only $path
git ls-files $path --exclude-standard --others
