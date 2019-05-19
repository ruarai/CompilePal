# Quick Start

### The Game Selector
The first window you see when you launch Compile Pal is the Game Selector. 
This is where you can select which game you are compiling for. 
These games are automatically detected based on the last instance of Hammer ran.
Game configurations are saved between sessions.

![Start Screen](https://i.imgur.com/Eh3SCDS.png)

#### If a game is missing, don't panic!
Run Hammer for the game that you want to compile for and restart Compile Pal. It should show up in the Game Selector.

### The Compile Window
After selecting a game, the Compile Window opens up.
![Compile Window](https://i.imgur.com/I3dqL1u.png)

#### 1. The Map List
The map list displays all the maps that will be compiled. 
Compile Pal features batch compiling, which lets you compile multiple maps at once.
Maps can be added and removed using the buttons to the right.

#### 2. The Preset Selector
The preset selector allows you to select a preset compile configuration. Presets store which processes will be run and their parameters. 
The buttons at the bottom allow you to add, clone, or remove presets.

#### 3. The Process Selector
The process selector allows you to select which processes will run during your compile. 
Processes can be disabled by unchecking the checkbox next to the process. 
The buttons on the bottom add and remove processes.

#### 4. The Parameter Box
The parameter box allows you to view and modify the parameters being passed into a process. 
You can view the name, value, description, and warning for each process.
Some parameters have a value field which allows you to put in your own values.

The text bar on the bottom gives a preview on the command line arguments being passed to the process.
The buttons to the bottom right let you add and remove parameters
![Parameter Chooser](https://i.imgur.com/jAaWcIQ.png)

When you add a parameter, the parameter chooser window pops up and allows you to double click on a parameter to add it.

### Compiling
To compile, click the compile button located at the bottom right. 
Compile Pal compiles your maps with a lower priority, which prevents your computer from freezing during compiles.

![Output](https://i.imgur.com/EcXMH06.png)

The output window will actively check your compile log using [Interlopers.net]("https://www.interlopers.net/errors") error listings. 
All errors, warnings, and infos will be highlighted in the compile log, and then displayed in a summary at the end.
Each error, warning, and info is clickable, and will display extra information about it.
