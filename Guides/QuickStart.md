# Quick Start

### The Game Selector
The first window you see when you launch Compile Pal is the Game Selector. 
This is where you can select which game you are compiling for. 
These games are automatically detected based on the last instance of Hammer ran.
Game configurations are saved between sessions.

![image](https://user-images.githubusercontent.com/15372675/219902034-b6596aa5-fe1e-44d8-b644-acc3239cc62f.png)

#### If a game is missing, don't panic!
Run Hammer for the game that you want to compile for and click refresh.

### The Compile Window
After selecting a game, the Compile Window will open.

![image](https://user-images.githubusercontent.com/15372675/219901499-16a75fe4-7ea8-4d23-abe5-042adacdf4a4.png)

#### 1. The Map List
![image](https://user-images.githubusercontent.com/15372675/219901925-8368960c-8bb8-4a6b-b764-15a3b0fd8620.png)

The map list displays all the maps that will be compiled. 
Compile Pal supports batch compiling, which lets you compile multiple maps at once. The checkbox to the right of the map controls whether or not the map will be skipped during compile (unchecked maps are skipped).
Maps can be added and removed using the buttons to the right.

#### 2. The Preset Selector
![image](https://user-images.githubusercontent.com/15372675/219901995-73d59232-e7ff-462a-88db-2fb95e017f7a.png)

The preset selector allows you to select a preset compile configuration. Presets store which processes will be run and their parameters.
You can add a preset by clicking the plus button on the top. If you select a preset, you can click the kebab menu to edit, clone, or remove the preset.

#### 3. The Process Selector
![image](https://user-images.githubusercontent.com/15372675/219902158-3f988485-ecc0-4bba-aae1-d426696b31f4.png)

The process selector allows you to select which processes will run during your compile. 
Processes can be disabled by unchecking the checkbox next to the process. 
The buttons on the top add and remove processes.

#### 4. The Parameter Box
![image](https://user-images.githubusercontent.com/15372675/219902235-d03d283d-f1a2-48c8-8527-c4fb29c7e078.png)

The parameter box allows you to view and modify the parameters being passed to a process. 
You can view the name, value, description, and warning for each process.
Some parameters have a value field which allows you to input your own values.

The text bar on the top gives a preview on the command line arguments being passed to the process.
The buttons on the top right let you add and remove parameters

![image](https://user-images.githubusercontent.com/15372675/219902311-7305095f-e548-4189-ace0-8f76f26a02a2.png)

When you add a parameter, the parameter chooser window pops up and allows you to double click on a parameter to add it.

### Compiling
![image](https://user-images.githubusercontent.com/15372675/219902373-dbe048ee-79bc-4cf3-baf0-e140e4aae22f.png)

The output window will actively check your compile log using [Interlopers.net](https://www.interlopers.net/errors) error listings. 
All errors, warnings, and infos will be highlighted in the compile log, and then displayed in a summary at the end.
Each error, warning, and info is clickable, and will display more detailed information.
