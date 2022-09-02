# Custom Compile Steps

Custom compile steps allow you to run your own programs as part of the compile process.

![Custom](https://i.imgur.com/nZ3LPua.png)

Hammer command line replacement parameters can be used to substitute parameters for compile time values, using the form `$variable`.

| Variable | Description |
| ------  | ----------------  |
| $file   | Map filename |
| $ext    | Extension of map (vmf, vmm, etc) |
| $path   | Full path to folder containing the map, with no trailing slash |
| $exedir | Path to location of the game executable |
| $bspdir | BSP directory |
| $bindir | Bin directory |
| $gamedir | Game directory |
| $vbsp_exe | Path to VBSP |
| $vvis_exe | Path to VVIS |
| $vrad_exe | Path to VRAD |
| $game_exe | Path to game executable |

When the Wait For Exit checkbox is unchecked, it will run the program asynchronously in the background. 
The next compile step will start immedietly after the program is started.
If Read Output is enabled, it will log to the Output window whenever it recieves output from the program.
This can cause the logs from different programs to become mixed up. If Compile Pal does not wait for the program to exit, be aware that the compile will show as finished, even if the custom program is still running.

![Order](https://i.imgur.com/QyYpBDx.png)

Custom processes can be run at any point in the compile. To reorder the compile steps, click the Order tab to switch to the Order view. 
Click and drag on any custom process to move it up or down in the compile process.
Processes at the top get run first.

# Modifying the current Game Configuration
You can modify the current game configuration by sending `COMPILE_PAL_SET variable value` through stdout. These changes will persist until the next map is compiled.

| Variable | Description |
| ------ | ---- |
| file | VMF filepath |
| bspdir | BSP directory |
| bindir | Bin directory|
| sdkbindir | SDK bin directory |
| gamedir | Game directory |
| vbsp_exe | Path to VBSP |
| vvis_exe | Path to VVIS |
| vrad_exe | Path to VRAD |
| game_exe | Path to the game |
| bspzip_exe | Path to BSPZip |
| vpk_exe | Path to VPK.exe |
| vbspinfo_exe | Path to VBSPInfo |
