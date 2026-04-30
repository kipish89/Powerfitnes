param(
    [ValidateSet("apk", "aab")]
    [string]$PackageFormat = "apk",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$ProjectPath = "C:\Users\User\Documents\New project\PowerFitness.App\PowerFitness.App.csproj",

    [string]$Framework = "net10.0-android",

    [string]$RuntimeIdentifier = "android-arm64",

    [switch]$UseKeystore,

    [string]$KeystoreFile = "",
    [string]$KeystorePassword = "",
    [string]$KeyAlias = "",
    [string]$KeyPassword = ""
)

$publishArgs = @(
    "publish",
    $ProjectPath,
    "-f", $Framework,
    "-c", $Configuration,
    "-r", $RuntimeIdentifier,
    "-p:AndroidPackageFormat=$PackageFormat",
    "-p:PublishTrimmed=false",
    "-p:RunAOTCompilation=false",
    "-p:NuGetAudit=false"
)

if ($UseKeystore) {
    if ([string]::IsNullOrWhiteSpace($KeystoreFile) -or
        [string]::IsNullOrWhiteSpace($KeystorePassword) -or
        [string]::IsNullOrWhiteSpace($KeyAlias) -or
        [string]::IsNullOrWhiteSpace($KeyPassword)) {
        throw "For signed export, specify KeystoreFile, KeystorePassword, KeyAlias, and KeyPassword."
    }

    $publishArgs += @(
        "-p:AndroidKeyStore=true",
        "-p:AndroidSigningKeyStore=$KeystoreFile",
        "-p:AndroidSigningStorePass=$KeystorePassword",
        "-p:AndroidSigningKeyAlias=$KeyAlias",
        "-p:AndroidSigningKeyPass=$KeyPassword"
    )
}

Write-Host "Building Android $PackageFormat ($Configuration)..." -ForegroundColor Cyan
& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "Android export failed."
}

$outputPath = Join-Path (Split-Path $ProjectPath -Parent) "bin\$Configuration\$Framework\publish"
Write-Host "Export completed. Check output:" -ForegroundColor Green
Write-Host $outputPath
