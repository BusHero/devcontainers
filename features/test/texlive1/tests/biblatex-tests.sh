#!/bin/sh

echo "\documentclass{article}
\usepackage{biblatex}
\addbibresource{/tmp/sample.bib}
\begin{document}
	test\cite{test}
\end{document}
" > /tmp/biblatex.tex
echo "@article{test, author = \"test\" }" > /tmp/smaple.bib

latex -halt-on-error \
	-interaction=nonstopmode \
	-output-directory /tmp \
	/tmp/biblatex.tex >/dev/null 2>&1
