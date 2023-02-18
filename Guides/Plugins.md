# Plugins (Beta)
Compile Pal's plugin based architecture allows developers to create their own compile steps.

Plugins are currently in a beta state, so the format and structure are subject to change.

![image](https://user-images.githubusercontent.com/15372675/218288001-2154a3fa-201c-4f18-ad0f-36959aed9108.png)

## Installation
Plugins can be installed by copying the plugin folder into the Compile Pal/Parameters folder.

***USE PLUGINS AT YOUR OWN RISK, DO NOT INSTALL PLUGINS FROM UNTRUSTED SOURCES***

## Structure
Plugins consist of a folder that contains a `meta.json` and `parameters.json` file, and optionally other files that the plugin may require to run such as an executable.
```
My Plugin/
  meta.json
  parameters.json
  MyPlugin.exe
```

### Meta.json Structure
`meta.json` is a JSON file that defines the metadata about the compile step
```json
{
  "Name": "string",
  "Description": "string",
  "Warning": "string",
  "Path": "string",
  "Arguments": "string",
  "BasisString": "string",
  "Order": "float",
  "DoRun": "bool",
  "ReadOutput": "bool",
  "SupportsBSP": "bool",
  "CheckExitCode": "bool",
  "CompatibleGames": "int[]",
  "IncompatibleGames": "int[]"
}
```
| Field | Description |
| ----- | ----------- |
| Name    | Plugin Name. Must match the folder name.
| Description | Description shown in the process adder dialog.
| Warning | Warning shown in the process adder dialog.
| Path    | Path to a program, relative to the Compile Pal folder. Can be templated, see [Variable Substitution](#Variable-Substitution). (For versions <=v27.28, this is relative to the Compile Pal/CompileLogs folder)
| Arguments | The first arguments passed to the program. Can be templated, see [Variable Substitution](#Variable-Substitution). (>=v27.28)
| BasisString | The last arguments passed to the program. Can be templated, see [Variable Substitution](#Variable-Substitution). Order of arguments is `Arguments` → `Arguments selected by user` → `BasisString`.
| Order   | Determines when your step should run. For example, an Order of 1.5 would run between VBSP and VVIS. For the complete ordering, look at the existing compile steps in the `Parameters` folder.
| DoRun		| Controls whether step is enabled by default. Set to `true` to enable it.
| ReadOutput | Controls whether program output is shown in the compile log.
| SupportsBSP | Indicates that this step can be used for BSP files. Steps that don't support BSPs are automatically disabled if a user selects a BSP file. Defaults to `false`. (>=v27.27)
| CheckExitCode | Checks for process exit code and raises a warning when it is not 0. Defaults to `true`. (>=v27.31)
| CompatibleGames | Whitelist of Steam App IDs for games that this plugin is compatible with. Will override IncompatibleGames if both are set. (>=v27.29)
| IncompatibleGames | Blacklist of Steam App IDs for games that this plugin is not compatible with. (>=v27.29)

### Variable Substitution
| Variable | Description |
| -------- | ----------- |
| `$vmfFile$` | Path to the vmf file
| `$map$` | Path to the vmf file without extension
| `$bsp$` | Path to the bsp file
| `$mapCopyLocation$` | Path to the bsp file after copying to the map folder
| `$gameName$` | Name of the current Game Configuration
| `$game$` | Path to the folder of the current Game Configuration
| `$gameEXE$` | Path to the game of the current Game Configuration
| `$mapFolder$` | Path to the map folder of the current Game Configuration
| `$sdkFolder$` | Path to the SDK map folder of the current Game Configuration
| `$binFolder$` | Path to the bin folder of the current Game Configuration
| `$vbsp$` | Path to VBSP for the current Game Configuration
| `$vvis$` | Path to VVIS for the current Game Configuration
| `$vrad$` | Path to VRAD for the current Game Configuration
| `$bspzip$` | Path to BSPZip for the current Game Configuration
| `$vbspInfo$` | Path to VBSPInfo for the current Game Configuration

### Parameters.json Structure
`parameters.json` is a JSON file that defines the parameters for a compile step
```json
[
	{
		"Name": "string",
		"Description": "string",
		"Warning": "string",
		"Parameter": "string",
		"CanBeUsedMoreThanOnce": "bool",
		"CanHaveValue": "bool",
		"Value": "string",
		"ValueIsFile": "bool",
		"ValueIsFolder": "bool",
		"CompatibleGames": "int[]",
		"IncompatibleGames": "int[]"
	},
	...
]
```
| Field | Description |
| ----- | ----------- |
| Name    | Parameter name.
| Description | Description shown in parameter adder dialog.
| Warning | Warning shown in parameter adder dialog.
| Parameter | Parameter passed to the plugin. Should have a space in front of the parameter, Ex. " --foo".
| CanBeUsedMoreThanOnce | Allows the parameter to be used multiple times. Defaults to `false`.
| CanHaveValue | Allows users to pass a value to the parameter.
| Value | Default value for the parameter.
| ValueIsFile | Indicates that value is a file. Adds a button that opens a File Picker dialog. Defaults to `false`.
| ValueIsFolder | Indicates that value is a folder. Adds a button that opens a Folder Picker dialog. Defaults to `false`.
| CompatibleGames | Whitelist of Steam App IDs for games that this plugin parameter is compatible with. Will override IncompatibleGames if both are set. (>=v27.29)
| IncompatibleGames | Blacklist of Steam App IDs for games that this plugin parameter is not compatible with. (>=v27.29)

## Modifying The Current Game Configuration (>=v27.30)
You can modify the current game configuration by sending `COMPILE_PAL_SET {variable} {value}` through stdout. These changes will persist until the next map is compiled.

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

For example, sending `COMPILE_PAL_SET file 'new/file/path.vmf'` will update the configuration to point to the vmf at `new/file/path.vmf` instead of what was originally selected.

## Best Practices
For examples, download [PLUGIN DEMO.zip](https://github.com/ruarai/CompilePal/files/9440548/PLUGIN.DEMO.zip) or look at the existing compile steps in the `Parameters` folder.


### Packaging An Application
It is recomended to package your application inside the plugin folder to make it easier to point to. For example, `Path` can be set to `Parameters\\My Plugin\\plugin.exe`.

### Python Plugins
Setting the `Path` to `python` or `python3` is not portable. Use the [Python Launcher](https://docs.python.org/3/using/windows.html#python-launcher-for-windows) `py` (requires Python >= 3.3), passing the python version in the `Arguments`, Ex.
```json
{
	"Path": "py",
	"Arguments": "-3 my_plugin.py",
}
```

## Debugging Plugins
You can view the program path and arguments in the `debug.log` found in the Compile Pal folder.
