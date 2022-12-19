from alpine:latest

RUN apk update \
	&& apk upgrade \
	&& apk add \
	texlive \
	texlive-luatex

COPY ./smoke_tests/ /smoke-tests
RUN /smoke-tests/tests.sh 2>&1

# Remove smoke tests
RUN rm /smoke-tests/ -r; \
	if [ -d /smoke-tests/ ]; then \
		echo "/smoke-tests was not removed"; \
		exit 1; \
	fi
