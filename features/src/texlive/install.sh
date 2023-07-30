#!/bin/sh

apk update
apk upgrade
apk add \
  alpine-sdk \
  freetype \
  fontconfig \
  gnupg \
  gzip \
  perl \
  tar \
  wget \
  xz \
  perl-app-cpanminus \
  perl-dev \
  bash

cpanm Log::Dispatch::File
cpanm File::HomeDir
cpanm Unicode::GCString
cpanm YAML::Tiny

TEXLIVE_ARCH="$(uname -m)-linuxmusl"
mkdir -p ${texlive_bin}
ln -sf "${texlive_bin}/${TEXLIVE_ARCH}" "${texlive_bin}/default"

mkdir -p /root
cp common/* /root/
cd /root

echo "binary_x86_64-linuxmusl 1" >> /root/texlive.profile
/root/install-texlive.sh $texlive_version
tlmgr install listings
if [ $PACKAGES ]; then
  sed -e 's/,/ /g' $PACKAGES | xargs tlmgr install
fi
rm -f /root/texlive.profile \
  /root/install-texlive.sh \
  /root/packages.txt
TERM=dumb luaotfload-tool --update
