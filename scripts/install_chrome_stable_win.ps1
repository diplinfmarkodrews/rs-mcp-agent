$ErrorActionPreference = 'Stop'

# Ensure TLS 1.2 is used for HTTPS downloads
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$url = 'https://dl.google.com/tag/s/dl/chrome/install/googlechromestandaloneenterprise64.msi'

$msiInstaller = "$env:temp\google-chrome.msi"
Write-Host "Downloading Google Chrome"
Invoke-WebRequest -Uri $url -OutFile $msiInstaller -UseBasicParsing

Write-Host "Installing Google Chrome"
$arguments = "/i `"$msiInstaller`" /quiet"
Start-Process msiexec.exe -ArgumentList $arguments -Wait
Remove-Item $msiInstaller


$suffix = "\\Google\\Chrome\\Application\\chrome.exe"
if (Test-Path "${env:ProgramFiles(x86)}$suffix") {
    (Get-Item "${env:ProgramFiles(x86)}$suffix").VersionInfo
} elseif (Test-Path "${env:ProgramFiles}$suffix") {
    (Get-Item "${env:ProgramFiles}$suffix").VersionInfo
} else {
    Write-Host "ERROR: Failed to install Google Chrome."
    Write-Host "ERROR: This could be due to insufficient privileges, in which case re-running as Administrator may help."
    exit 1
}
