$texlive_version = '2024'
$alpine_version = '3.20'

$schemes = @(
    "basic",
    "medium",
    "minimal",
    "small",
    "full"
)

foreach ($scheme in $schemes) {
    try {
        docker build `
            --build-arg scheme=${scheme} `
            --build-arg texlive_version=$texlive_version `
            --build-arg alpine_version=${alpine_version} `
            --tag "bus1hero/texlive:${texlive_version}-${scheme}-alpine.${alpine_version}" `
            --push `
            .
    }
    catch { "An error occured for ${scheme}" }
}
