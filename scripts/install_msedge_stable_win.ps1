$ErrorActionPreference = 'Stop'

# Ensure TLS 1.2 is used for HTTPS downloads
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$url = $args[0]
if (-not $url) {
    Write-Host "ERROR: No download URL provided."
    Write-Host "Usage: .\install_msedge_stable_win.ps1 <download-url>"
    exit 1
}

$msiInstaller = "$env:temp\microsoft-edge-stable.msi"
Write-Host "Downloading Microsoft Edge"
Invoke-WebRequest -Uri $url -OutFile $msiInstaller -UseBasicParsing

Write-Host "Installing Microsoft Edge"
$arguments = "/i `"$msiInstaller`" /quiet"
Start-Process msiexec.exe -ArgumentList $arguments -Wait
Remove-Item $msiInstaller

$suffix = "\\Microsoft\\Edge\\Application\\msedge.exe"
if (Test-Path "${env:ProgramFiles(x86)}$suffix") {
    (Get-Item "${env:ProgramFiles(x86)}$suffix").VersionInfo
} elseif (Test-Path "${env:ProgramFiles}$suffix") {
    (Get-Item "${env:ProgramFiles}$suffix").VersionInfo
} else {
    Write-Host "ERROR: Failed to install Microsoft Edge."
    Write-Host "ERROR: This could be due to insufficient privileges, in which case re-running as Administrator may help."
    exit 1
}