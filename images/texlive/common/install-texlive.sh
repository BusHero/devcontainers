#!/bin/sh

# Adapted from
# https://raw.githubusercontent.com/pandoc/dockerfiles/8fdaeb6e3652438c3b657cc72def1109e42f641f/common/latex/install-texlive.sh

default_version=2025
tlversion=${1:-"$default_version"}
installer_archive=install-tl-unx.tar.gz

if [ "$tlversion" = "$default_version" ]; then
    installer_url='http://mirror.ctan.org/systems/texlive/tlnet/'
    repository=
else
    installer_url="\
ftp://tug.org/historic/systems/texlive/$tlversion/tlnet-final"
    repository="\
ftp://tug.org/historic/systems/texlive/$tlversion/tlnet-final"
fi

wget --no-verbose \
     "$installer_url/$installer_archive" \
     "$installer_url/$installer_archive".sha512 \
     "$installer_url/$installer_archive".sha512.asc \
    || exit 1

gpg --keyserver keyserver.ubuntu.com \
    --receive-key 0xC78B82D8C79512F79CC0D7C80D5E5D9106BAB6BC || exit 5
gpg --verify "$installer_archive".sha512.asc || exit 5
sha512sum "$installer_archive".sha512 || exit 5

mkdir -p ./install-tl
tar --strip-components 1 -zvxf "$installer_archive" -C "$PWD/install-tl" \
    || exit 1

./install-tl/install-tl ${repository:+-repository "$repository"} \
                        --profile=/root/texlive.profile

rm -rf ./install-tl \
   "$installer_archive" \
   "$installer_archive.sha512" \
   "$installer_archive.sha512.asc"
