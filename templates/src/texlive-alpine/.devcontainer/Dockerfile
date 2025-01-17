ARG scheme=minimal
FROM bus1hero/texlive:2024-${scheme}-alpine.3.20

# RUN apk update \
#     && apk upgrade \
#     && apk --no-cache add \
#     git \
#     openjdk11

WORKDIR /

ARG ltex_version=
ADD https://github.com/valentjn/ltex-ls/releases/download/${ltex_version}/ltex-ls-${ltex_version}.tar.gz /ltex-ls-${ltex_version}.tar.gz
RUN tar -xzf /ltex-ls-${ltex_version}.tar.gz \
    && rm ltex-ls-${ltex_version}.tar.gz

# ARG latexindent_settings_file=
# ENV LATEXINDENT_CONFIG="${texlive_bin}/config/indentconfig.yaml"
# COPY common/mysettings.yaml "${texlive_bin}/config/${latexindent_settings_file}"
# RUN echo -e "paths: \n\
#     - /opt/texlive/texdir/bin/config/${latexindent_settings_file}" >> "${LATEXINDENT_CONFIG}"

# ENV LATEXMKRCSYS="${HOME}/config/latexmk"
# COPY common/latexmk "${LATEXMKRCSYS}"

# ARG packages=
# RUN if [ $packages ]; then echo "${packages}" | sed -e 's/,/ /g' | xargs tlmgr install; fi
