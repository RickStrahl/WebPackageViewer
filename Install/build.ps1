Clear-Host
Set-Location "$PSScriptRoot" 
$release = "$PSScriptRoot\..\WebPackageViewer\bin\Release\net472\win-x64"
Write-Host $release

remove-item $release\*.pdb

$windir = $env:windir
$platform = "v4,$windir\Microsoft.NET\Framework64\v4.0.30319"

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$release\WebPackageViewer.exe").FileVersion
$version = $version.Trim()
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

# dotnet tool install --global ilrepack
# Merge Dlls into single EXE - missing WebView2Loader.dll - has to be manually copied
ilrepack /target:winexe /ver:$version  /targetplatform:$platform  /lib:. /lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319 /lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF /out:..\WebPackageViewer.exe $release\WebPackageViewer.exe $release\Microsoft.Web.WebView2.Core.dll $release\Microsoft.Web.WebView2.Wpf.dll

# TODO: Remove when done debugging
ilrepack /target:winexe /ver:$version  /targetplatform:$platform  /lib:. /lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319 /lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF /out:"D:\projects\DocumentationMonster\DocumentationMonster\BinSupport\WebPackageViewer.exe" $release\WebPackageViewer.exe $release\Microsoft.Web.WebView2.Core.dll $release\Microsoft.Web.WebView2.Wpf.dll

remove-item ../WebPackageViewer.exe.config

& ".\signtool.exe" sign /v /n "West Wind Technologies"  /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 "..\WebPackageViewer.exe"

exit 0