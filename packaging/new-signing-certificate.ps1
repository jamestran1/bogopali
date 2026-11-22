<#
.SYNOPSIS
  Creates a self-signed code-signing certificate for sideloading the bogopali MSIX and exports it as .pfx.

.DESCRIPTION
  Run once on the maintainer's machine. The subject must equal the MSIX publisher (the Partner Center
  Publisher value, or the build-release.ps1 default). To sign releases in CI, store the .pfx as the
  repository secret MSIX_SIGNING_CERTIFICATE (base64) and its password as MSIX_SIGNING_PASSWORD:

    [Convert]::ToBase64String([IO.File]::ReadAllBytes('bogopali-signing.pfx')) | gh secret set MSIX_SIGNING_CERTIFICATE
    gh secret set MSIX_SIGNING_PASSWORD

  Keep the .pfx out of the repository.

.EXAMPLE
  ./packaging/new-signing-certificate.ps1 -Publisher 'CN=bogopali' -OutputPath bogopali-signing.pfx
#>
[CmdletBinding()]
param(
    [string] $Publisher = 'CN=bogopali',

    [Parameter(Mandatory)]
    [string] $OutputPath,

    [int] $ValidYears = 3
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$password = Read-Host 'Password for the .pfx' -AsSecureString
$certificate = New-SelfSignedCertificate -Type Custom -Subject $Publisher -KeyUsage DigitalSignature `
    -FriendlyName 'bogopali MSIX signing' -CertStoreLocation 'Cert:\CurrentUser\My' `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

try {
    Export-PfxCertificate -Cert $certificate -FilePath $OutputPath -Password $password | Out-Null
    Write-Host "Exported $OutputPath for $Publisher (thumbprint $($certificate.Thumbprint))."
}
finally {
    Remove-Item "Cert:\CurrentUser\My\$($certificate.Thumbprint)"
}
