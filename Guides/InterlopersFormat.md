# Interlopers Error Format

Ex (values from Interlopers.net).

```
2
4|\*\*\*\*\s+leaked\s+\*\*\*\*
<div class="error_box"><h4>**** leaked ****</h4><p><strong>Description:</strong><br />You have a leak. A leak is a hole in your map that exposes any entity in your map to the void (the black space outside your map).<br /><br /><strong>Solution:</strong><br />Fix the leak.<br /><br /><strong>See also:</strong><br /><a href="javascript:showerror(147)">Reference: Leaks</a><br /><a href="http://developer.valvesoftware.com/wiki/Leak">WIKI: leaks</a></p><p><br /><span class="e_type e_lvl4">This error will cause your map to fail compiling correctly</span></p><h5>Last contribution: <a href="forum/memberlist.php?mode=viewprofile&amp;u=0">Anonymous</a></h5></div>
5|bmodel\s+([\d\.,-]+)\s+has\s+no\s+head\s+node\s+\(class\s+'([\w\/\\_\-\.]+)',\s+targetname\s+'([\w\/\\_\-\.]+)'\)
<div class="error_box"><h4>bmodel [sub:1] has no head node (class '[sub:2]', targetname '[sub:3]')</h4><p><strong>Description:</strong><br />A [sub:2] should be brush-based, but doesn't have a brush tied to it. This can happen for instance when you tie the brushes of this entity to another entity in &quot;Ignore groups&quot;-mode.<br /><br /><strong>Solution:</strong><br />Find your entity (use entity report in Hammer to filter for the entities class and/or name) and delete it or tie it to a brush.<br /><br /><strong>See also:</strong><br /><a href="http://developer.valvesoftware.com/wiki/Hammer_Entity_Report_Dialog">WIKI: Entity report</a></p><p><br /><span class="e_type e_lvl5">This error will cause your map to fail compiling completely</span></p><h5>Last contribution: <a href="forum/memberlist.php?mode=viewprofile&amp;u=375">zombie@computer</a></h5></div>
```

The format begins with an integer representing the total number of errors

This is followed by the errors themselves, which are made up of two lines. The first line contains a number representing the severity of the error, and a regex to match the error, seperated by a pipe (`|`). The severity is an integer value between 0 and 5, with 0 being info and 5 being a critical error.

For example,
```
4|\*\*\*\*\s+leaked\s+\*\*\*\*
```
Has a severity of `4` and error regex `\*\*\*\*\s+leaked\s+\*\*\*\*`

The second line is a HTML snippet which is used for rendering the error message.

Error regexes can capture groups which can be referenced in the error message using `[sub:GROUP_NUMBER]`

``` 
5|bmodel\s+([\d\.,-]+)\s+has\s+no\s+head\s+node\s+\(class\s+'([\w\/\\_\-\.]+)',\s+targetname\s+'([\w\/\\_\-\.]+)'\)
<div class="error_box"><h4>bmodel [sub:1] has no head node (class '[sub:2]', targetname '[sub:3]')</h4><p><strong>Description:</strong><br />A [sub:2] should be brush-based, but doesn't have a brush tied to it. This can happen for instance when you tie the brushes of this entity to another entity in &quot;Ignore groups&quot;-mode.<br /><br /><strong>Solution:</strong><br />Find your entity (use entity report in Hammer to filter for the entities class and/or name) and delete it or tie it to a brush.<br /><br /><strong>See also:</strong><br /><a href="http://developer.valvesoftware.com/wiki/Hammer_Entity_Report_Dialog">WIKI: Entity report</a></p><p><br /><span class="e_type e_lvl5">This error will cause your map to fail compiling completely</span></p><h5>Last contribution: <a href="forum/memberlist.php?mode=viewprofile&amp;u=375">zombie@computer</a></h5></div>
```


# How Compile Pal Parses Interlopers Errors
## Error Severity Mapping

| Value | Severity | 
| -------- | ----- |
| 0 | Info |
| 1 | Info |
| 2 | Caution |
| 3 | Warning  |
| 4 | Error |
| 5 | Fatal Error |


## Short Description
The short description shown to users is parsed from the first `<h4>` tag of the error message

For example, the short message of the follow error is `**** leaked ****`:
```
<div class="error_box"><h4>**** leaked ****</h4><p><strong>Description:</strong><br />You have a leak. A leak is a hole in your map that exposes any entity in your map to the void (the black space outside your map).<br /><br /><strong>Solution:</strong><br />Fix the leak.<br /><br /><strong>See also:</strong><br /><a href="javascript:showerror(147)">Reference: Leaks</a><br /><a href="http://developer.valvesoftware.com/wiki/Leak">WIKI: leaks</a></p><p><br /><span class="e_type e_lvl4">This error will cause your map to fail compiling correctly</span></p><h5>Last contribution: <a href="forum/memberlist.php?mode=viewprofile&amp;u=0">Anonymous</a></h5></div>
```


## Error Styling
Compile Pal ignores all style classes and applies it's own styling to the error message:
```
@font-face {
    font-family: 'Segoe UI';
    src: local('Segoe UI'), local('Segoe UI'), url('Segoe UI.ttf');
}

body {
    font-family: "Segoe UI";
}

strong {
    font-weight: 600;
}
h5 {
    font-weight: 600;
}
h4 {
    font-weight: 600;
}
```

# Compile Pal Error Format
```
{
    "RegexTrigger": {
        "Pattern": String,
        "Options": Int (optional)
    },
    "Message": String,
    "ShortDescription": String
    "Severity": Int
}
```
