BASS.NET API for the Un4seen BASS Audio Library
-----------------------------------------------
Requires  : BASS Audio Library plus add-ons - available @ www.un4seen.com
            Works with the Microsoft .NET Framework v2.0, v3.0, v3.5 and v4.0 and supports Visual Studio 2005, 2008 and 2010.
Copyright : (c) 2005-2011 by radio42, Hamburg, Germany
Author    : Bernd Niedergesaess, bn@radio42.com

Purpose   : .NET API wrapper for the Un4seen BASS Audio libraray

WARNING
-------
TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, BASS.NET IS PROVIDED
"AS IS", WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY
AND/OR FITNESS FOR A PARTICULAR PURPOSE. THE AUTHORS SHALL NOT BE HELD
LIABLE FOR ANY DAMAGE THAT MAY RESULT FROM THE USE OF BASS.NET. BASICALLY,
YOU USE BASS.NET ENTIRELY AT YOUR OWN RISK.


SETUP
-----
Run the setup.exe as contained in this zip file and simply follow the instructions.
NOTE:
The Bass.Net.dll release version is now installed in the specified <install-dir> 
and will already be registered to the .NET Framework as a standard component (if you leave the installation options checked).
So when writing your own application using BASS.NET you can simple add a new project reference and select the 
"BASS.NET API" from the standard .NET components tab - that's all.
The native BASS libraries are not included here and need to be downloaded seperately.
In order to run the samples provided, you need to copy the relevant BASS libraries to the output directories first!
To uninstall BASS.NET simple go to the windows control panel and select software...and remove BASS.NET there.


Why a setup and no zip file?
----------------------------
The setup allows automatic integration into your Visual Studio environment (e.g. VS2005).
This is:
- registering the Bass.Net.dll as a .NET Framework assembly component
- integrating the provided BASS.NET MS Help 2.x files into your Visual Studio environment
If you deselect all options during the setup process - then the setup will just extract the files and do nothing more - just like an unzip.


Why 15MB?
---------
The BASS.NET API comes with a total of 3 different help systems, for maximum convenience.
Each help system takes around 8 MB and covers BASS and all Add-Ons. So total of approx. 15 MB help files are provided.
This is:
- Bass.Net.xml - which allows Visual Studio to display IntelliSense information during coding
- Bass.Net.chm - the classic MS Help 1.x offline help file for the BASS.NET API
- Bass.Net.HxS - the modern MS Help 2.x online help files which allows full integration into Visual Studio
The actual Bass.Net.dll (which you will ship along with your application) is only approx. 500 KB of size.


What does the Setup do?
-----------------------
a) Lets you select an installation folder
b) Displays the standard license agreement
c) Lets you select three installation options
d) Simply extracts the BASS.NET API files to the installation folder
e) Optionally: Registers the BASS.NET API as a .NET component
f) Optionally: Integrates the BASS.NET API Help files to your Visual Studio environment
g) Optionally: Adds a menu group to your start menu
h) Allows you to uninstall everything


What are the installation options and what do they do?
------------------------------------------------------
a) Register Bass.Net.dll as a .NET Framework assembly component compiled with the 'For Any CPU' setting.
--
Whenever you want to start writing your own applications using the BASS.NET API 
you need to include a reference to the Bass.Net.dll library of course!
In addtion make sure to place the native bass.dll or any add-on you are using in your executable directory as well!
So in your project explorer you need to select "Add Reference..." and select the Bass.Net.dll
This option will automatically register the BASS.NET API as a .NET component - so it will be listed in the first tab!
Actually the following will be done by the setup...
Create a new registry key "BASS.NET API" under
"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders"
resp.
"HKEY_CURRENT_USER\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders"
the value will be set to the selected install directory - that's all - and gets the Bass.Net.dll listed as a .NET component.


b) Integrate the BASS.NET API Help files to your Visual Studio environment
--
Do you know how MS Help 2.x works? It is the new Microsoft help system being used in Visual Studio (among others).
During coding you can press F1 while your cursor is over a command, class, keyword etc. and it will bring up the help online.
Or you can open the help manually when selecting "Help" from the main menu of your Visual Studio environment.
The BASS.NET API also comes with Help 2.x files. This options installs and merges these files into the ms-help system, 
so that they are availabe online during coding as well - just like for any other .Net component.
It also adds a filter to the online ms-help system. Just try it out and you will how nice it is to get the help online by pressing F1
over any piece of BASS code.
If you have deselected this option during installation you can simply install and or uninstall this feature manually.
- Open a 'Command Prompt' (cmd.exe) and go to the "<install-dir>\Help" directory
- Call "H2Reg.exe -r -m" to register the help files to your ms-help system manually
- Call "H2Reg.exe -u -m" to unregister the help files from your ms-help system manually
The BASS.NET API Help files will be integrated into the Visual Studio 2003 and/or 2005 namespace.

c) Add a shortcut to the Start > All Programms menu
--
Simply creates a menu group called "BASS.NET API" and adds it to the Program menu of your Start menu.


INCLUDED/INSTALLED FILES
------------------------
\<install-dir>:
  Bass.Net.dll           : the BASS.NET API library (release build which will be used in your applications)
  Bass.Net.xml           : the BASS.NET API xml documentation file (needed for IntelliSense)
  LICENSE.rtf            : the BASS.NET API license file
  readme.txt             : setup and general information text file (this)
\<AppUserData>\BASS.NET\Samples:
  .\CS                   : this folder contains all C# examples and a global solution for it (sample.sln)
  .\VB                   : this folder contains all VB examples and a global solution for it (sample.sln)
\<install-dir>\Help:
  Bass.Net.chm           : the actual MS Help 1.x offline help file for the BASS.NET API
  H2Reg.exe              : helpware.net utility allowing registering MS Help 2.x Collections (done during setup)
  *.Hx?                  : the actual MS Help 2.x online help files which will be integrated into your Visual Studio environment


What's the point?
-----------------
BASS.NET is an API implementation to be used with the Microsoft .NET Framework. The API can be used with any .NET language, 
like C#, Visual Basic, JScript or managed C++. It is based on managed C# code.
MS Help 2.x files are provided and integrate into VS to support online help for all BASS functions.


Requirements
------------
BASS 2.4 is required (bass.dll). See www.un4seen.com for details!
In addition the respective add-ons are required as well, if you call any method from the "Un4seen.Bass.AddOn" namespace.


Copyright
---------
BASS.NET API: Copyright 2005-2009 by radio42, Author: Bernd Niedergesaess  (bn@radio42.com). All rights reserved. 
BASS.NET is the property of radio42 and is protected by copyright laws and international copyright treaties. BASS.NET is not sold, it is licensed.
BASS and Add-Ons: All trademarks and other registered names contained in the BASS.NET package are the property of their respective owners.
See www.un4seen.com for details!


Disclaimer
----------
The freeware version of BASS.NET is free for non-money making use. 
If you are not charging for or making any money with your software AND you are an individual person (not a company) AND 
your software is intended for personal use only, then you can use the BASS.NET API in your software for free.
Free in this case means, that the you can use BASS.NET without any further license fees.
It does NOT mean that you are free to change, copy, redistribute, share or use BASS.NET in any purpose.

If you wish to use BASS.NET in shareware or commercial products (or it has other commercial purpose, e.g. advertising, training etc.), 
you will require a separate license from radio42 (see the Cost section for details).

By using this software, you agree to the following conditions:
1) The freeware version of BASS.NET API is distributed under the license of radio42 (see LICENSE.rtf).
2) The LICENSEE may not charge for or make any money with the software using the freeware version of BASS.NET.
3) It is prohibited to change any of the provided source code and ALL the files must at any time remain intact and unmodified.
4) You may not decompile, disassemble, reverse engineer or modify any part of BASS.NET.
5) The LICENSEE may ONLY distribute the DLL part of BASS.NET with your software (Bass.Net.dll), no other part of BASS.NET may be distributed.
6) You may not resell or sublicense BASS.NET.
7) A splash screen will appear every time at start-up (unless you obtained a valid Registration-Key).
8) You are NOT allowed to pass your personal Registration-Key to anyone at anytime!
Please note, that you also need to take care of all BASS modules and their respective rights.


Cost
----
BASS.NET is free for non-commercial use. If you are a non-commercial entity (eg. an individual) and you are not charging for your product, 
and the product has no other commercial purpose, then you can use BASS.NET in it for free.
Otherwise, you will require one of the following licences:

Shareware license: 29.00 Euro 
(The "shareware" licence allows the usage of BASS.NET in an unlimited number of your shareware products, which must sell for no more than 40 Euros each.
If you're an individual (not a corporation) making and selling your own software (and its price is within the limit), this is the licence for you.)

Single Commercial license: 199.00 Euro
(The "single commercial" license allows the usage of BASS.NET API in a single commercial product)

Unlimited Commercial license: 499.00 Euro 
(The "unlimited commercial" license allows the usage of BASS.NET API in an unlimited number of your commercial products. 
This license applies to a single site)

Please note the products must be end-user products, e.g. not components used by other products. 
These licences only cover your own software. Not the publishing of other's software. 
If you publish other's software, its developers (or the software itself) will need to be licensed to use BASS.NET.

In all cases there are no royalties to pay, and you can use all future updates without further cost. 
Reselling is not permitted.

You can obtain a shareware or commercial license here: http://www.bass.radio42.com
Contact:
radio42, Bernd Niedergesaess
Gryphiusstrasse 9
22299 Hamburg, Germany
Mail: bn@radio42.com 


Registration
------------
You can obtain your License and Registration-Key here: http://www.bass.radio42.com

After you have received your Registration-Key, you might disable the splash screen by calling the following method prior to any other BASS method:
BassNet.Registration("<your email>", "<your regkey>");


Third party intellectual property rights
----------------------------------------
BASS.NET is a .Net wrapper of the product BASS (www.un4seen.com). In order to use BASS.NET an additional BASS license needs to be obtained separately.
MP3 technology is patented, and so the use of the BASS MP3 decoder in the PRODUCTS requires the LICENSEE to have a patent license from Thomson (www.mp3licensing.com).
Alternatively, the LICENSEE does not need a patent license if BASS is set to use the already licensed Windows MP3 decoder.
