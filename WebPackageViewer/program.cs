
using System;
using System.Runtime.InteropServices;
using WebPackageViewer;

class Program
{
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);

    [STAThread]
    static int Main(string[] args)
    {
        string commandLine = Environment.CommandLine;

        if (args.Length == 0 || 
            (!commandLine.Contains("package ") ||
             !commandLine.Contains("unpackage ") ))
        {
            // hide the Console window immediately
            ShowWindow(GetConsoleWindow(), 0); // hide console flash
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();

        return 0;
    }
}