
using System.Diagnostics;

namespace WebPackageViewer.Utilities;
using System.ComponentModel;
using System.IO;
using System;
using System.Runtime.InteropServices;



/// <summary>
/// Class used to read and update Native Resources in an executable.
/// 
/// In this tool, we use this to write the WebSite zip file data 
/// into the executable as a native resource and read it back out.
/// Also used to read the compiled in CoreWebView2loader.dll resource 
/// at runtime back out.
/// 
/// #define RT_RCDATA 10     <-- Resource ID (ushort value)
/// WEBSITE_DATA RT_RCDATA "..\\website_data.zip"     <-- Resource Key (WEBSITE_DATA)
/// WEBVIEW2LOADER RT_RCDATA "..\\webview2loader.dll"  <-- Resource Key (WEBVIEW2LOADER)
/// </summary>
public class NativeResourceHelper
{

    /// <summary>
    /// Updates a native Resource in the specified executable. The resource must already exist in the 
    /// executable's .rc/res file or this will fail.
    /// </summary>
    /// <param name="exePath">Exe to update</param>
    /// <param name="dataPath">Filename to update. Pass `CLEAR` to clear the resource.</param>
    /// <param name="resourceKey">Name of the key to update ie. WEBSITE_DATA</param>
    /// <param name="resourceId">numeric resourceId defined in the Resource file (ie. 10) as in `#define RT_RCDATA 10`</param>
    /// <exception cref="Win32Exception"></exception>
    public static void UpdateResource(string exePath, string dataPath, string resourceKey = "WEBSITE_DATA", ushort resourceId = 10)
    {
        byte[] data = [49,49];  // empty data but it can't be zero length!
        if (dataPath != "CLEAR")
            data = File.ReadAllBytes(dataPath);
        

        var h = BeginUpdateResource(exePath, false);
        if (h == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            // "#10" using a string parm doesn't appear to work 
            // so we use the IntPtr overload with the 
            IntPtr typePtr = MakeIntResource(resourceId);
            
            if (!UpdateResource(
                    h,                    
                    typePtr,
                    resourceKey,
                    1033,
                    data,
                    data.Length))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!EndUpdateResource(h, false))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        catch
        {
            EndUpdateResource(h, true);
            throw;
        }
    }

    /// <summary>
    /// Reads a native Resource from the current Executable.
    /// </summary>
    /// <param name="resourceName">Name of the native ResourceKey that matches resource key</param>
    /// <param name="resourceId">Resource ID defined in the .rc/res file - using numeric Id for consistency</param>
    /// <param name="hModule">Optional - pass in an HMODULE handle. If not provided, the current executable's module handle is used.</param>
    /// <returns></returns>
    /// <exception cref="Win32Exception"></exception>
    public static byte[] ReadResource(string resourceName, int resourceId = 10, IntPtr hModule = default)
    {        
        if (hModule == IntPtr.Zero)
        {
            hModule = GetModuleHandle(null); // current EXE
        }

        IntPtr hResource = FindResource(hModule, resourceName, $"#{resourceId}");
        if (hResource == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "FindResource failed.");

        uint size = SizeofResource(hModule, hResource);
        if (size == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SizeofResource failed.");

        IntPtr hLoaded = LoadResource(hModule, hResource);
        if (hLoaded == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadResource failed.");

        IntPtr pResource = LockResource(hLoaded);
        if (pResource == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LockResource failed.");

        var data = new byte[size];
        Marshal.Copy(pResource, data, 0, (int)size);

        return data;
    }

    #region Native Update Resources

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    //[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    //static extern bool UpdateResource(
    //    IntPtr hUpdate,
    //    IntPtr lpType,        
    //    string lpName,
    //    ushort wLanguage,
    //    byte[] lpData,
    //    int cbData);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool UpdateResource(
        IntPtr hUpdate,
        IntPtr lpType,
        string lpName,
        ushort wLanguage,
        byte[] lpData,
        int cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    #endregion

    #region Native Retrieve Resources

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindResource(
        IntPtr hModule,
        string lpName,
        string lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadResource(
        IntPtr hModule,
        IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LockResource(
        IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(
        IntPtr hModule,
        IntPtr hResInfo);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private static IntPtr MakeIntResource(ushort id)
    {
        return new IntPtr(id);
    }

    #endregion
}