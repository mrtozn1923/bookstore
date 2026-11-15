function Resolve-BookstoreBaseUrl {
    # Not exported: only the module's own functions call it
    param(
        [string] $Environment,
        [string] $BaseUrl
    )
    if ($BaseUrl) { return $BaseUrl.TrimEnd('/') }
    $script:BookstoreEnvironments[$Environment]
}
