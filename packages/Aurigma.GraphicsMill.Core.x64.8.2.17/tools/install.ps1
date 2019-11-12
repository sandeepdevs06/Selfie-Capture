param($installPath, $toolsPath, $package, $project)

function Test-RegistryValue($path, $name)
{
    $key = Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
    $key -and $null -ne $key.GetValue($name, $null)
}

$hasKey = Test-RegistryValue "HKLM:\SOFTWARE\Aurigma\Graphics Mill for .NET\6.0" "License Key"

if (!$hasKey)
{
    start-process $toolsPath\LicenseManager\LicenseManager.exe -verb runAs
}