# Fetch the version from EscapeFromTarkov.exe
$tarkovPath = 'F:\SPT-AKI-DEV\EscapeFromTarkov.exe'
$tarkovVersion = (Get-Item -Path $tarkovPath).VersionInfo.FileVersionRaw.Revision
Write-Host "Current version of EscapeFromTarkov.exe is: $tarkovVersion"

# Update AssemblyVersion
$assemblyPath = '{0}\..\Properties\AssemblyInfo.cs' -f $PSScriptRoot
$versionPattern = '^\[assembly: TarkovVersion\(.*\)\]'
$foundVersion = $false
$lines = Get-Content $assemblyPath

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $versionPattern) {
        $lines[$i] = '[assembly: TarkovVersion({0})]' -f $tarkovVersion
        $foundVersion = $true
        break
    }
}

if (!$foundVersion) {
    $newLine = '[assembly: TarkovVersion({0})]' -f $tarkovVersion
    $lines += $newLine
    Write-Host "Added 'TarkovVersion' attribute with value '$tarkovVersion' to '$assemblyPath'"
}

$lines | Set-Content $assemblyPath

Write-Host "AssemblyInfo.cs updated successfully!"