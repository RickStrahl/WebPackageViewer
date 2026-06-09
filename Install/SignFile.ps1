# West Wind Sign Script
# install:
# dotnet tool install -g --prerelease sign

param(
    [string]$file = "",
    [string]$file1 = "",
    [string]$file2 = "",
    [string]$file3 = "",
    [string]$file4 = "",
    [string]$file5 = "",
    [string]$file6 = "",
    [string]$file7 = "",
    [string]$file8 = "",
    [boolean]$login = $false
)
if (-not $file) {
    Write-Host "Usage: SignFile.ps1 -file <path to file to sign>"
    exit 1
}


if ($login) {   # force a login
    az config set core.enable_broker_on_windows=false
    az login
    az account set --subscription "Pay-As-You-Go"
}


# SignfileMetadata.json is not checked in. Format:
# {
#   "Endpoint": "https://eus.codesigning.azure.net/",
#   "CodeSigningAccountName": "MySigningAccount",
#   "CertificateProfileName": "MySigningCertificateProfile"
# }
$metadata = Get-Content -Path "SignfileMetadata.json" -Raw | ConvertFrom-Json
$tsEndpoint = $metadata.Endpoint
$tsAccount = $metadata.CodeSigningAccountName
$tsCertProfile = $metadata.CertificateProfileName
$timeServer = "http://timestamp.digicert.com"

$signArgs = @(
    "--verbosity", "warning",
    "--timestamp-url", $timeServer,
    "--artifact-signing-endpoint", $tsEndpoint,
    "--artifact-signing-account", $tsAccount,
    "--artifact-signing-certificate-profile", $tsCertProfile
)

# Add non-empty file arguments at the end
foreach ($f in @($file, $file1, $file2, $file3, $file4, $file5, $file6, $file7, $file8)) {
    if (![string]::IsNullOrWhiteSpace($f)) {
        $signArgs += $f
    }
}

# ...existing code...

# Add non-empty file arguments
foreach ($f in @($file, $file1, $file2, $file3, $file4, $file5, $file6, $file7, $file8)) {
    if (![string]::IsNullOrWhiteSpace($f)) {
        $signArgs += $f
    }
}

$argString = [string]::Join(" ", $signArgs)
$argString

# Run signtool and capture the exit code
sign code artifact-signing $signArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "File(s) signed successfully." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Signing failed with exit code: $exitCode" -ForegroundColor Red
    exit $exitCode
}
