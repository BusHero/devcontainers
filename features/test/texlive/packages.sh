#!/bin/bash
set -e

echo "\documentclass[12pt,a4paper]{article} \usepackage{listings} \begin{document}Hi\end{document}" > "test.tex"

pdflatex test.tex
