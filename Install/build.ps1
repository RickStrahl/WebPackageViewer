# uses IlRepack (dotnet tool)
Clear-Host
Set-Location "$PSScriptRoot" 
$release = "$PSScriptRoot\..\WebPackageViewer\bin\Release\net472\win-x64"
Write-Host $release

remove-item $release\*.pdb

$windir = $env:windir
$platform = "v4,$windir\Microsoft.NET\Framework64\v4.0.30319"

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$release\WebPackageViewer.exe").FileVersion
$version = $version.Trim()
$originalVersion = $version
"Initial Version: " + $version

# Remove last two .0 version tuples if it's 0
if($version.EndsWith(".0.0")) {
    $version = $version.SubString(0,$version.Length - 4);
}
else {
    if($version.EndsWith(".0")) {    
        $version = $version.SubString(0,$version.Length - 2);
    }
}
"Truncated Version: " + $version

# dotnet tool install --global dotnet-ilrepack
# Merge Dlls into single EXE - missing WebView2Loader.dll - has to be manually copied
$ilRepackArgs = @(
    '/target:winexe'
    "/targetplatform:$platform"
    "/ver:$originalVersion"
    '/lib:.'
    '/lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
    '/lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'
    '/out:..\WebPackageViewer.exe'
    "$release\WebPackageViewer.exe"
    "$release\Microsoft.Web.WebView2.Core.dll"
    "$release\Microsoft.Web.WebView2.Wpf.dll"
)

Write-Host "------------- IL Repack Arguments -------------"
Write-Host ($ilRepackArgs -join ' ')
Write-Host "-----------------------------------------------"

& ilrepack @ilRepackArgs

remove-item ../WebPackageViewer.exe.config

# copy unsigned copy
copy ../WebPackageViewer.exe ../WebPackageViewer-Unsigned.exe
copy ../WebPackageViewer.exe "\projects\DocumentationMonster\DocumentationMonster\BinSupport\WebPackageViewer.exe"

& ".\signfile" -file "..\WebPackageViewer.exe"

#copy ../WebPackageViewer.exe "\projects\DocumentationMonster\DocumentationMonster\BinSupport"

exit 0