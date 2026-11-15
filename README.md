# Bookstore.Analytics — "Analytics ve Ölçüm" series code

Every dataset under `data/` is synthetic, made for the examples; none of it is real site data.

## Requirements
- .NET 10 SDK

## Build and test
    dotnet build
    dotnet test

## Commands
    # 01: classify an access log (combined format) by traffic source
    dotnet run --project src/Bookstore.Analytics.Cli -- logs data/access-sample.log www.bookstore.example

    # 02: low-CTR queries compared with the site's own CTR per position bucket
    dotnet run --project src/Bookstore.Analytics.Cli -- opportunities data/gsc-pages-queries.csv

    # 02: try a Search Console regex with Go's RE2-based regexp package (Go 1.27)
    cd tools/re2check && go run . '(^|\s)(nasıl|nasil|nedir)(\s|$)' ../../data/queries-sample.txt
