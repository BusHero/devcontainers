ARG alpine_version=
FROM mcr.microsoft.com/devcontainers/base:alpine-${alpine_version}

RUN apk update \
    && apk upgrade \
    && apk --no-cache add \
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
    perl-dev

RUN curl -L http://cpanmin.us | perl - App::cpanminus \
    && cpanm Module::Pluggable@5.2 \
    && cpanm Log::Dispatch::File@2.71 \
    && cpanm File::HomeDir@1.006 \
    && cpanm http://www.cpan.org/authors/id/N/NE/NEZUMI/Unicode-LineBreak-2019.001.tar.gz \
    && cpanm YAML::Tiny@1.74

ARG texlive_bin='/opt/texlive/texdir/bin'
RUN TEXLIVE_ARCH="$(uname -m)-linuxmusl" && \
    mkdir -p ${texlive_bin} && \
    ln -sf "${texlive_bin}/${TEXLIVE_ARCH}" "${texlive_bin}/default"

ENV PATH="${texlive_bin}/default:${PATH}"

WORKDIR /root

COPY common/texlive.profile /root/texlive.profile
COPY common/install-texlive.sh /root/install-texlive.sh

ARG texlive_version=2024
ARG scheme=basic
RUN echo "selected_scheme scheme-${scheme}" >> /root/texlive.profile \
    && sh /root/install-texlive.sh $texlive_version \
    && rm -f /root/texlive.profile \
    /root/install-texlive.sh
RUN if [ `which luaotf  load-tool` ] ; then TERM=dumb luaotfload-tool --update; fi \
    && chmod -R o+w /opt/texlive/texdir/texmf-var

COPY LICENSE /LICENSE
