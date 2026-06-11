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



# ILRepack creates a new PE and drops native Win32 resources.
# Copy all native resources from the primary build output back into the merged EXE.
$sourceExe = "$release\WebPackageViewer.exe"
$targetExe = "$PSScriptRoot\..\WebPackageViewer.exe"

Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class Win32ResourceCopier
{
    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

    private delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
    private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
    private delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, ushort wIDLanguage, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, EnumResLangProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, int cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    private static bool IsIntResource(IntPtr ptr)
    {
        return ((ulong)ptr.ToInt64() >> 16) == 0;
    }

    private static IntPtr ToNative(ResId id, List<IntPtr> allocated)
    {
        if (id.IsInt)
            return new IntPtr(id.Id);

        IntPtr p = Marshal.StringToHGlobalUni(id.Name);
        allocated.Add(p);
        return p;
    }

    private sealed class ResId
    {
        public bool IsInt;
        public ushort Id;
        public string Name;
    }

    private sealed class ResKey
    {
        public ResId Type;
        public ResId Name;
        public ushort Lang;
    }

    public static void CopyAll(string sourceExe, string targetExe)
    {
        IntPtr source = LoadLibraryEx(sourceExe, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (source == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx(source) failed");

        IntPtr update = BeginUpdateResource(targetExe, true);
        if (update == IntPtr.Zero)
        {
            FreeLibrary(source);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "BeginUpdateResource(target) failed");
        }

        var keys = new List<ResKey>();

        try
        {
            if (!EnumResourceTypes(source, (h, type, p1) =>
            {
                EnumResourceNames(h, type, (h2, type2, name, p2) =>
                {
                    EnumResourceLanguages(h2, type2, name, (h3, type3, name3, lang, p3) =>
                    {
                        var typeId = new ResId();
                        if (IsIntResource(type3))
                        {
                            typeId.IsInt = true;
                            typeId.Id = (ushort)type3.ToInt64();
                        }
                        else
                        {
                            typeId.Name = Marshal.PtrToStringUni(type3);
                        }

                        var nameId = new ResId();
                        if (IsIntResource(name3))
                        {
                            nameId.IsInt = true;
                            nameId.Id = (ushort)name3.ToInt64();
                        }
                        else
                        {
                            nameId.Name = Marshal.PtrToStringUni(name3);
                        }

                        keys.Add(new ResKey { Type = typeId, Name = nameId, Lang = lang });
                        return true;
                    }, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                return true;
            }, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumResourceTypes failed");
            }

            foreach (var key in keys)
            {
                var allocated = new List<IntPtr>();
                IntPtr nativeType = ToNative(key.Type, allocated);
                IntPtr nativeName = ToNative(key.Name, allocated);

                string typeStr = key.Type.IsInt ? key.Type.Id.ToString() : key.Type.Name;
                string nameStr = key.Name.IsInt ? key.Name.Id.ToString() : key.Name.Name;
                System.Console.WriteLine($"Copying resource: Type={typeStr}, Name={nameStr}, Lang={key.Lang}");

                IntPtr hRes = FindResourceEx(source, nativeType, nativeName, key.Lang);
                if (hRes == IntPtr.Zero)
                {
                    foreach (var ptr in allocated)
                        Marshal.FreeHGlobal(ptr);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "FindResourceEx failed");
                }

                uint size = SizeofResource(source, hRes);
                if (size == 0)
                {
                    foreach (var ptr in allocated)
                        Marshal.FreeHGlobal(ptr);
                    continue;
                }

                IntPtr hLoaded = LoadResource(source, hRes);
                if (hLoaded == IntPtr.Zero)
                {
                    foreach (var ptr in allocated)
                        Marshal.FreeHGlobal(ptr);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadResource failed");
                }

                IntPtr pData = LockResource(hLoaded);
                if (pData == IntPtr.Zero)
                {
                    foreach (var ptr in allocated)
                        Marshal.FreeHGlobal(ptr);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "LockResource failed");
                }

                byte[] bytes = new byte[size];
                Marshal.Copy(pData, bytes, 0, (int)size);

                if (!UpdateResource(update, nativeType, nativeName, key.Lang, bytes, bytes.Length))
                {
                    foreach (var ptr in allocated)
                        Marshal.FreeHGlobal(ptr);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed");
                }

                foreach (var ptr in allocated)
                    Marshal.FreeHGlobal(ptr);
            }

            if (!EndUpdateResource(update, false))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "EndUpdateResource commit failed");

            update = IntPtr.Zero;
        }
        catch
        {
            if (update != IntPtr.Zero)
                EndUpdateResource(update, true);
            throw;
        }
        finally
        {
            FreeLibrary(source);
        }
    }
}
"@ | Out-Null

[Win32ResourceCopier]::CopyAll($sourceExe, $targetExe)
Write-Host "Copied Win32 resources from source EXE to merged EXE."

remove-item ../WebPackageViewer.exe.config

# copy unsigned copy
copy ../WebPackageViewer.exe ../WebPackageViewer-Unsigned.exe
copy ../WebPackageViewer.exe "\projects\DocumentationMonster\DocumentationMonster\BinSupport\WebPackageViewer.exe"

& ".\signfile" -file "..\WebPackageViewer.exe"

#copy ../WebPackageViewer.exe "\projects\DocumentationMonster\DocumentationMonster\BinSupport"

exit 0