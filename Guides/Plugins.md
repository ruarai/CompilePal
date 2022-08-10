# Plugins
Compile Pal's plugin based architecture allows developers to create their own compile steps.

![image](https://user-images.githubusercontent.com/15372675/183811926-d23c8b65-4df1-4cb8-8474-93d63bd1fd56.png)

## Installation
Plugins can be installed by copying the plugin folder into the Compile Pal/Parameters folder.

***USE PLUGINS AT YOUR OWN RISK, DO NOT INSTALL PLUGINS FROM UNTRUSTED SOURCES***

## Structure
Plugins consist of a folder that contains a `meta.json` and `parameters.json` file.
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
  "SupportsBSP": "bool"
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
| DoRun		| This should always be true. This indicates that the step is an external program and not a built-in compile step.
| ReadOutput | Controls whether program output is shown in the compile log.
| SupportsBSP | Indicates that this step can be used for BSP files. Steps that don't support BSPs are automatically disabled if a user selects a BSP file. Defaults to `false`. (>=v27.27)

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
		"ValueIsFolder": "bool"
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

## Best Practices
For examples, download [PLUGIN DEMO.zip](https://github.com/ruarai/CompilePal/files/9296903/PLUGIN.DEMO.zip) or look at the existing compile steps in the `Parameters` folder.

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
