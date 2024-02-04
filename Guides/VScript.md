# VScript Packing Hints
Packing hints are special comments that can be used to pack resources whose paths are generated at runtime

Example:
```js
local overlay = "foo_";
foreach(ability in hudAbilityInstances[player])
{
    local percentage = ability.MeterAsPercentage();
    //<...>
    if (percentage >= 100)
        overlay += "on";
    else
        overlay += "off";
}
//<...>
player.SetScriptOverlayMaterial(API_GetString("ability_hud_folder") + "/" + overlay);    
```

## IncludeFile
```python
// !CompilePal::IncludeFile(internal_path: str)
```
Pack a file and it's dependencies

Example:
```python
// !CompilePal::IncludeFile("materials/ability_hud_folder/foo_on.vmt")
```

## IncludeDirectory
```python
// !CompilePal::IncludeDirectory(internal_path: str)
```

Pack all files in a folder (and all subfolders) and their dependencies

Example:
```python
// !CompilePal::IncludeDirectory("materials/ability_hud_folder")
```
