![image](http://freeso.org/wp-content/uploads/2016/03/freeso-logo.png?1)

A full reimplementation of The Sims Online, using Monogame. While FreeSO aims to be faithful to the original game, it includes many quality of life changes such as hardware rendering, custom dynamic lighting, hi-res output and >2 floor houses. If you simply wish to play, you can install FreeSO and the original TSO files using our installer on http://freeso.org, and it will connect to our official servers.

FreeSO currently depends on the original game files (objects, avatars, ui) to function, which are available for download from EA servers. FreeSO is simply a game engine, and does not contain any copyrighted material in and of itself.

![image](http://freeso.org/wp-content/uploads/2017/05/band.png)

# The Sims 1 via Simitone

FreeSO is additionally a base project for an ongoing re-implementation of The Sims 1's engine, [Simitone](https://github.com/RHY3756547/Simitone), targetted mainly at mobile devices. 

The content system, HIT VM and SimAntics VM included within this repo support both TSO and TS1 game files - meaning that TS1 will still run in a limited sense under TSO's UI frontend within FreeSO. [Simitone](https://github.com/RHY3756547/Simitone) fully restores TS1 gameplay by tying the neighbourhood and game systems together with a suitable UI frontend.

# Volcanic

Volcanic is an extension of FreeSO that allows users to view, modify and save game objects alongside a live instance of the SimAntics VM. It features a vast array of resource editors for objects - the most prominent being the script editor. It allows for easy creation of new objects, and debugging of existing ones. Volcanic also functions when the FSO engine has loaded TS1 objects and other resources.

![image](https://i.gyazo.com/431b8e3cb1547563bb2d64a380fb76e6.gif)
![image](https://i.gyazo.com/ba013836812ce97c9b555f72be50b1db.gif)

# Contributing
You can contribute to FreeSO by testing cutting edge features in the latest releases, filing bugs, and joining in the discussion on our forums!

* [Getting Started](https://github.com/RHY3756547/FreeSO/wiki)
* [Coding Standards](https://github.com/RHY3756547/FreeSO/wiki/Coding-standards)
* [Pull Requests](https://github.com/RHY3756547/FreeSO/pulls): [Open](https://github.com/RHY3756547/FreeSO/pulls)/[Closed](https://github.com/RHY3756547/FreeSO/issues?q=is%3Apr+is%3Aclosed)
* [Translation](http://forum.freeso.org/forums/translations.32/)
* [Forums](http://forum.freeso.org)
* [Blog](http://freeso.org)
* [Official Discord](https://discordapp.com/invite/xveESFj)

Looking for something to do? Check out the issues tagged as [help wanted](https://github.com/RHY3756547/FreeSO/labels/help%20wanted) to get started.

Regarding translations, full object and UI translations should currently be released on the forums. An improved system for distribution and organisation will be set up in a month or two. Stay tuned!

## Prerequisites
* [Visual Studio 2015](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx)
* [MonoGame](http://www.monogame.net): 3.5 for the iOS and Android VS2015 project types. (optional)
* [Xamarin for Visual Studio](https://www.xamarin.com/visual-studio): For iOS and Android builds. (optional)

# License
> This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
> If a copy of the MPL was not distributed with this file, You can obtain one at
> http://mozilla.org/MPL/2.0/.
