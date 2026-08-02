$ErrorActionPreference = 'Stop'

$certPath = 'C:\etc\ssl\localhost.pfx'
$pwdText = 'password'

if (Test-Path $certPath) {
    Write-Host "Certificate already exists: $certPath"
    exit 0
}

Write-Host 'Generating self-signed localhost certificate (dev only)...'
$cert = New-SelfSignedCertificate `
    -DnsName localhost `
    -CertStoreLocation Cert:\CurrentUser\My `
    -KeyExportPolicy Exportable `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(5)

$pwd = ConvertTo-SecureString -String $pwdText -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $pwd | Out-Null
Remove-Item $cert.PSPath

Write-Host "Created $certPath"
