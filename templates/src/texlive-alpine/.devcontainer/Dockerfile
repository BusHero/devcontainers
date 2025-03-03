ARG scheme=minimal
FROM bus1hero/texlive:2024-${scheme}-alpine.3.20

RUN apk update \
    && apk upgrade \
    && apk --no-cache add \
    git \
    openjdk11

WORKDIR /

ARG ltex_version=
ADD https://github.com/valentjn/ltex-ls/releases/download/${ltex_version}/ltex-ls-${ltex_version}.tar.gz /ltex-ls.tar.gz
RUN tar -xzf /ltex-ls.tar.gz \
    && mv ltex-ls-${ltex_version} /ltex-ls \
    && rm ltex-ls.tar.gz

ENV latexindent_settings_file="mysettings.yaml"
ENV texlive_bin="/opt/texlive/texdir/bin"
ENV LATEXINDENT_CONFIG="${texlive_bin}/config/indentconfig.yaml"
COPY common/mysettings.yaml "${texlive_bin}/config/${latexindent_settings_file}"
RUN echo -e "paths: \n\
    - ${texlive_bin}/config/${latexindent_settings_file}" >> "${LATEXINDENT_CONFIG}"

RUN tlmgr update --self \
    && tlmgr install latexindent \
    && tlmgr install latexmk

COPY common/packages /tmp/packages

RUN cat /tmp/packages | xargs tlmgr install && rm /tmp/packages
