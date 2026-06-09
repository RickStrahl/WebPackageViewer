# West Wind Web Package Viewer


**A static Web Site packager for Windows that lets you run a Web site offline as a self-contained Exe**

### Still under Construction

This tool can quickly automate creating a small self-contained Windows Exe that can 'run' a folder as a Web site locally without an Internet connection or Web server. 

The idea is that you can package a static site and run it locally just by launching the EXE which internally loads all links.

Here's what this looks like in one of my applications - [Documentation Monster](https://documentationmonster.com) - which generates a self-contained documentation Web site that can then be packaged into the Web Package Viewer as a standalone Exe:

![Web Packager in Action with a documentation project](https://documentationmonster.com/docs/Output-Generation/OfflineDocumentationViewer.png)


## Why this?
It's meant to address the scenario of packaging and running local static Web sites that would normally require an Http Server to run - ie. that won't just run as files from disk, due to Http requirements for loading dynamic data. This is a common scenario for client side applications that load data or UI content at runtime as most modern Javascript frameworks do these days. 

It also addresses scenario for documentation or FAQ Web sites that users often request to be able to run offline. It provides an easy way to provide rich documentation functionality without having to run online. 


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


## Current Status
This project currently is experimental and specifically geared towards the integration in [Documentation Monster](https://documentationmonster.com). It works great for that, but there are some customizations that make it specifically work in this project.

It's possible to customize and integrate with your own solutions. Currently I wouldn't consider this a generic solution that bundles any kind of Web content as WebView only Web browsing has a few limitations. I'm considering adding support for HttpSys based local Web server integration, but that unfortunately requires admin rights to initially to register a local http port.

### A few things that don't work well

* **Default Document Navigation**
Because there's no Web server that sits behind this there's no Web site configuration. You can specify an 'initial' Url that is appended to blank Urls and serves the purpose of a default document. Only one document (ie. `/index.html`) can be specified.

* **#anchor  Navigation**  
Due to some quirks in the WebView resource navigation, `#anchor` paths are not passed to the the initial Web Resource navigation that currently serves page content. So there's no way to know that that's happening, so # links still result in a local navigation as we can't differentiate anchored and non-anchored request.


## Windows Security
The idea of this tool is to allow packaged Html to 'run' and display even if it contains dynamically loaded content (as most SPA apps do). The implementation of this tool allows for that to work just fine.

Unfortunately Windows can get in the way of smooth operation, due to security concerns running an executable application that is downloaded off the Internet causing downloaded files and files contained in downloaded archives to have MOTW (Mark of the Web).

If you:

* Download the EXE off the Web
* Download a Zip File that includes the EXE off the Web

You are likely to run into the dreaded `SmartScreen` blue warning screen that tries to warn you off executing that Exe. 

SmartScreen is a reputation based security feature that warns about unknown executables downloaded off the Web. You can bypass SmartScreen using the not so obvious **More info** link, but the warning dialog is scary and requires educating users on how to bypass it (for more info see [this post](https://markdownmonster.west-wind.com/blog/posts/2026/Jun/01/Windows-Protected-your-PC-Dealing-with-Windows-SmartScreen-on-Installation)).

![bypassingsmartscreen](https://markdownmonster.west-wind.com/blog/imagecontent/2026/windows-protected-your-pc-dealing-with-windows-smartscreen-on-installation/bypassingsmartscreen.png)

