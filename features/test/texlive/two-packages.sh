#!/bin/bash
set -e

echo "packages: $packages"

echo "\documentclass[12pt,a4paper]{article} \usepackage{listings} \usepackage{subfiles} \begin{document}Hi\end{document}" > "test.tex"

pdflatex -interaction=nonstopmode test.tex
