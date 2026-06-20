
using System;
using System.IO;
using System.Runtime.InteropServices;
using WebPackageViewer;

class Program
{
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);

    [STAThread]
    static int Main(string[] args)
    {
        //string commandLine = Environment.CommandLine;


        //string firstParm = string.Empty;
        //if (args.Length > 0)
        //    firstParm = args[0];
        //bool startedFromConsole = StartedFromConsole();
        
        //if (!startedFromConsole)
        //{
        //    //if (args.Length == 0 ||
        //    //    (!args[0].Contains("package ") &&
        //    //     !args[0].Contains("unpackage ") &&
        //    //     !args[0].Contains("help")))

        //    // hide the Console window immediately
        //    ShowWindow(GetConsoleWindow(), 0); // hide console flash
        //}

        var app = new App();
        app.InitializeComponent();
        app.Run();

        return 0;
    }


    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();

   
}