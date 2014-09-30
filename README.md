CompilePal
==========

Compile Pal is a wrapper for the source map compiling tools that provides easy configuration as well as extra features.

It was inspired by tools like VBCT and serves to be an easier to use replacement with improvements such as:

An easy to manage UI:
![ui](http://i.imgur.com/lR4SlKy.png)

Fast configuration management:

![config](http://zippy.gfycat.com/EasyBewitchedColt.gif)

Taskbar progress display (Windows 7):

![progress](http://zippy.gfycat.com/UnlawfulImpeccableGrosbeak.gif)

Automatic file packing:

![packing](http://i.imgur.com/G5SKGdE.png)

Post compile actions:

![post](http://i.imgur.com/pLlIWCK.png)

Download
==========
*
https://github.com/ruarai/CompilePal/releases/latest


Configuration
==========

Compile Pal should automatically find any game configurations - as long as the game's SDK was run last.

Packing
==========
PACK is a prototype feature that allows for the automatic packing of custom content into a map BSP. It does not cover all custom content, so is best suited for when you simply want to share a map with a friend.

Changes
==========

#### 012:

- Added error checking for compile output

- Fix crash bug that occured when a compile program output certain characters
- Fixed UI alignment issues

Thanks to Statua for reporting his discovery of the crash issue.

#### 011:

- Make output work on a character-by-character basis so you can see progress in realtime
- Add post compile options such as map file archive, shutdown and run file
- Fixed issue where cancelling compile would crash
- Added memory of previous games to the launch window
- Add hotkey support. Press F8 to bring up Compile Pal and press enter to immediately begin compiling after.
- Add large number of extra program configuration options
- Make map copy checkbox ticked by default
- Lots of minor bug fixes

Thanks to TopHATTWaffle for his ideas that contributed to this version.

#### 010:

- Fixed major error with model packing where models would not be packed at all.
- Fixed typo

#### 009:

- Add model packing to PACK

#### 008:

- Fix major compile cancel error
- Fix for exception handling
- Add analytics

