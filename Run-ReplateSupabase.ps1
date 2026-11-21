param(
    [string]$DatabaseHost = 'aws-1-ap-northeast-1.pooler.supabase.com',
    [string]$DatabaseUser = 'postgres.rnrrdcgcatislnkpyzhr',
    [int]$DatabasePort = 5432,
    [string]$RootCertificate = (Join-Path $PSScriptRoot 'prod-ca-2021.crt')
)

$ErrorActionPreference = 'Stop'

$DatabaseHost = $DatabaseHost.Trim()
$DatabaseUser = $DatabaseUser.Trim()
if ($DatabaseHost -notmatch '^[a-zA-Z0-9.-]+\.pooler\.supabase\.com$' -or
    $DatabaseUser -notmatch '^[a-zA-Z0-9_.-]+$' -or
    $DatabasePort -ne 5432) {
    throw 'Salin Host, User, dan Port 5432 dari opsi Session pooler di dashboard Supabase.'
}

if ($RootCertificate) {
    if (!(Test-Path -LiteralPath $RootCertificate -PathType Leaf)) {
        throw 'File sertifikat Supabase tidak ditemukan.'
    }
    $RootCertificate = (Resolve-Path -LiteralPath $RootCertificate).ProviderPath
}

$password = Read-Host 'Password database Supabase' -AsSecureString
$credential = [System.Net.NetworkCredential]::new($DatabaseUser, $password)
$previousConnection = $env:REPLATE_DB_CONNECTION_STRING
$previousPassword = $env:REPLATE_DB_PASSWORD
$previousRootCertificate = $env:PGSSLROOTCERT

try {
    $env:REPLATE_DB_CONNECTION_STRING = "Host=$DatabaseHost;Port=$DatabasePort;Database=postgres;Username=$DatabaseUser;SSL Mode=VerifyFull;Timeout=10"
    $env:REPLATE_DB_PASSWORD = $credential.Password
    if ($RootCertificate) {
        $env:PGSSLROOTCERT = $RootCertificate
    }
    dotnet run --project (Join-Path $PSScriptRoot 'src/Replate.Desktop')
    if ($LASTEXITCODE -ne 0) {
        throw "Replate berhenti dengan kode $LASTEXITCODE."
    }
}
finally {
    $env:REPLATE_DB_CONNECTION_STRING = $previousConnection
    $env:REPLATE_DB_PASSWORD = $previousPassword
    $env:PGSSLROOTCERT = $previousRootCertificate
    $password.Dispose()
}
