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

# create texlive.profile
echo "selected_scheme $SCHEME" >> /root/texlive.profile
echo "TEXDIR /opt/texlive/texdir" >> /root/texlive.profile
echo "TEXMFLOCAL /opt/texlive/texmf-local" >> /root/texlive.profile
echo "TEXMFSYSVAR /opt/texlive/texdir/texmf-var" >> /root/texlive.profile
echo "TEXMFSYSCONFIG /opt/texlive/texdir/texmf-config" >> /root/texlive.profile
echo "TEXMFVAR ~/.texlive/texmf-var" >> /root/texlive.profile
echo "TEXMFCONFIG ~/.texlive/texmf-config" >> /root/texlive.profile
echo "TEXMFHOME ~/texmf" >> /root/texlive.profile
echo "instopt_adjustpath 0" >> /root/texlive.profile
echo "instopt_adjustrepo 1" >> /root/texlive.profile
echo "instopt_letter 0" >> /root/texlive.profile
echo "instopt_portable 0" >> /root/texlive.profile
echo "instopt_write18_restricted 1" >> /root/texlive.profile
echo "tlpdbopt_autobackup 1" >> /root/texlive.profile
echo "tlpdbopt_backupdir tlpkg/backups" >> /root/texlive.profile
echo "tlpdbopt_create_formats 1" >> /root/texlive.profile
echo "tlpdbopt_desktop_integration 1" >> /root/texlive.profile
echo "tlpdbopt_file_assocs 1" >> /root/texlive.profile
echo "tlpdbopt_generate_updmap 0" >> /root/texlive.profile
echo "tlpdbopt_install_docfiles 0" >> /root/texlive.profile
echo "tlpdbopt_install_srcfiles 0" >> /root/texlive.profile
echo "tlpdbopt_post_code 1" >> /root/texlive.profile
echo "tlpdbopt_sys_bin /usr/local/bin" >> /root/texlive.profile
echo "tlpdbopt_sys_info /usr/local/share/info" >> /root/texlive.profile
echo "tlpdbopt_sys_man /usr/local/share/man" >> /root/texlive.profile
echo "tlpdbopt_w32_multi_user 1" >> /root/texlive.profile

/root/install-texlive.sh $texlive_version
tlmgr install listings
if [ $PACKAGES ]; then
  sed -e 's/,/ /g' $PACKAGES | xargs tlmgr install
fi
rm -f /root/texlive.profile \
  /root/install-texlive.sh

# load fonts for luatex
if [ `which luaotfload-tool` ]; then
  TERM=dumb luaotfload-tool --update
fi
