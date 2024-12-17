![image](http://freeso.org/wp-content/uploads/2016/03/freeso-logo.png?1)

A full reimplementation of The Sims Online, using Monogame. While FreeSO aims to be faithful to the original game, it includes many quality of life changes such as hardware rendering, custom dynamic lighting, hi-res output and >2 floor houses. While there used to be an official FreeSO server, FreeSO is now a technology base for other The Sims Online servers to build upon. Please see the https://freeso.org blog for more information. In the future, a client specifically suited to exploring the original FreeSO server alone or with friends will be made available in a different repository.

FreeSO currently depends on the original game files (objects, avatars, ui) to function, which are available for download from EA servers. FreeSO is simply a game engine, and does not contain any copyrighted material in and of itself.

![image](http://freeso.org/wp-content/uploads/2017/05/band.png)

# The Sims 1 via Simitone

FreeSO is additionally a base project for an ongoing re-implementation of The Sims 1's engine, [Simitone](https://github.com/riperiperi/Simitone). This project is largely incomplete, but is in interesting novelty in itself.

The content system, HIT VM and SimAntics VM included within this repo support both TSO and TS1 game files - meaning that TS1 will still run in a limited sense under TSO's UI frontend within FreeSO. [Simitone](https://github.com/riperiperi/Simitone) fully restores TS1 gameplay by tying the neighbourhood and game systems together with a suitable UI frontend.

# 3D Mode

![image](https://cdn.discordapp.com/attachments/355135351234494464/355396364349210625/unknown.png)

The FreeSO engine additionally supports a 3D mode, which allows you to see the game from a different perspective. 3D meshes are reconstructed at runtime from the z-buffers included with object sprites. FreeSO also generates 3D geometry for walls and floors at runtime, and switches to an alternate camera with different controls when the mode is enabled. 

The mode can be enabled via the launch parameter `-3d`. See the blog for more information. (http://freeso.org/the-impossible/)

# Volcanic

Volcanic is an extension of FreeSO that allows users to view, modify and save game objects alongside a live instance of the SimAntics VM. It features a vast array of resource editors for objects - the most prominent being the script editor. It allows for easy creation of new objects, and debugging of existing ones. Volcanic also functions when the FSO engine has loaded TS1 objects and other resources.

![image](https://i.gyazo.com/431b8e3cb1547563bb2d64a380fb76e6.gif)
![image](https://i.gyazo.com/ba013836812ce97c9b555f72be50b1db.gif)

# Contributing
You can contribute to FreeSO by testing cutting edge features in the latest releases, filing bugs, and joining in the discussion on our forums!

* [Getting Started](https://github.com/riperiperi/FreeSO/wiki)
* [Project Structure](https://github.com/riperiperi/FreeSO/wiki/Project-structure)
* [Coding Standards](https://github.com/riperiperi/FreeSO/wiki/Coding-standards)
* [Pull Requests](https://github.com/riperiperi/FreeSO/pulls): [Open](https://github.com/riperiperi/FreeSO/pulls)/[Closed](https://github.com/riperiperi/FreeSO/issues?q=is%3Apr+is%3Aclosed)
* [Translation](http://forum.freeso.org/forums/translations.32/)
* [Forums](http://forum.freeso.org)
* [Blog](http://freeso.org)
* [Official Discord](https://discordapp.com/invite/xveESFj)

Looking for something to do? Check out the issues tagged as [help wanted](https://github.com/riperiperi/FreeSO/labels/help%20wanted) to get started.

## Prerequisites
* [Visual Studio 2019](https://visualstudio.microsoft.com/vs/)
* [MonoGame](http://www.monogame.net): 3.5 for the iOS and Android VS2015 project types. (optional)

# License
> This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
> If a copy of the MPL was not distributed with this file, You can obtain one at
> http://mozilla.org/MPL/2.0/.
