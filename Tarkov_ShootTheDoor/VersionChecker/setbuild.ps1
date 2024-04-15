# Fetch the version from EscapeFromTarkov.exe
$tarkovPath = 'F:\SPT-AKI-DEV\EscapeFromTarkov.exe'
$tarkovVersion = (Get-Item -Path $tarkovPath).VersionInfo.FileVersionRaw.Revision
Write-Host "Current version of EscapeFromTarkov.exe is: $tarkovVersion"

# Update AssemblyVersion
$assemblyPath = '{0}\..\Properties\AssemblyInfo.cs' -f $PSScriptRoot
$versionPattern = '^\[assembly: TarkovVersion\(.*\)\]'
(Get-Content $assemblyPath) | ForEach-Object {
    if ($_ -match $versionPattern){
    	$versionType = $matches[1]
        $newLine = '[assembly: TarkovVersion({0})]' -f $tarkovVersion
        Write-Host "Changed the line from '$_' to '$newLine'"
        $newLine
    } else {
        $_
    }
} | Set-Content $assemblyPath

Write-Host "AssemblyInfo.cs updated successfully!"