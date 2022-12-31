FROM alpine:latest

RUN apk update \
	&& apk upgrade \
	&& apk add \
	curl \
	wget \
	perl-dev \
	git-lfs \
	alpine-sdk \
	texlive \
	biber \
	dotnet7-sdk \
	bash

RUN curl -L http://cpanmin.us | perl - App::cpanminus \
	&& cpanm Log::Dispatch::File \
	&& cpanm File::HomeDir \
	&& cpanm Unicode::GCString \
	&& cpanm YAML::Tiny

RUN wget https://mirrors.ctan.org/macros/latex/contrib/logreq.zip \
	&& unzip /logreq.zip -d /usr/share/texmf-dist/tex/latex/contrib \
	&& mktexlsr
