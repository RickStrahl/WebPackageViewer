# West Wind Web Package Viewer

#### A static Web Site packager for Windows that lets you run a Web site offline as a self-contained Exe

This tool can quickly automate creating a small self-contained Windows Exe that can 'run' a folder as a Web site locally without an Internet connection or Web server.

## Why this?
It's meant to address the scenario of packaging and running local static Web sites that would normally require an Http Server to run - ie. that won't just run as files from disk, due to Http requirements for loading dynamic data. This is a common scenario for client side applications that load data or UI content at runtime as most modern Javascript frameworks do these days. 

It also addresses scenario for documentation Web sites that users often request to be able to run offline. It provides an easy way to provide rich documentation functionality without having to run online. 


## Operational Modes
The tool has 3 different modes:

* **Run**  
Run Mode unpacks a previously packaged application, by unpacking the attached Web site and then 'running' the Web site out of the unpacked temporary folder in a standard Desktop Window. No Web server is required.

* **Package**  
Package Mode is used to create a customized version of the package Exe that includes the packaged Web Site data as a zip file. The packaged Exe is comprised of the original WebPackageViewer.exe plus the zipped Web site. 

* **Unpackage**  
Unpackage allows you to **manually** unpackage the Exe and the embedded data, either as a zip file, or as a folder.

A single small executable that can be driven with Command Line parameters is used to handle all three of these modes 

Unpackage is likely an uncommon scenario - you're typically going to 'run' the package

## Command Line Options
The following command line options are available:

```
----------------------------
West Wind Web Package Viewer
----------------------------
A Web Site Viewer that can:

Usage: WebPackageViewer [foldername | command] [options]

Foldername:
Runs a web site in that location or if no folder in the current folder.
If packaged, unpacks into a tempfolder and runs the site from there.

Commands:
  package   - Create a package from an executable and a zip file
  unpackage - Unpackage the Exe and Website into the output folder
  help      - Show this help message

Options:
  --output     - package: Output filename for the packaged exe
                 unpackage: Output folder where the Web site and Exe is unpackaged to
  --exe        - Optional Exe file to package. If not specified source exe is used
  --zipfile    - The Zip File to package
  --zipfolder  - A folder to zip up and then package
  --virtual    - Virtual Path when running the site (/*, /docs)
  --initialurl - Initial URL to load in the WebView (/index.html*, /docs/index.html)


Configuration File
You can optionally provide a configuration in your Webroot Folder
that allows you to customize how the Packaged site runs:

// WebPackageViewer.config.json
{
    "VirtualPath": "/docs",
    "InitialUrl": "/docs/index.html",
    "Window Title": "West Wind Web Package Viewer"
}
```


