# UID Explorer Plugin

An inspector plugin for the [Godot](https://godotengine.org/) game engine.

<p align="center">
    <img src="images/icon_horizontal_1.png" alt="Plugin icon with resource populated.">
</p>

<p align="center">
    <img src="images/icon_horizontal_2.png" alt="Plugin icon without resource populated.">
</p>

## Overview
Adds a new property hint for [exported properties](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_exports.html#doc-c-sharp-exports). This property hint adds the ability to select a file in the editor, navigate to it in the file system, and populate the property with the resource's [UID](https://docs.godotengine.org/en/stable/classes/class_resourceuid.html). The resource can also be expanded by pressing the converted path button.

## How to Use
1. Variable must be a `string`.
2. Expose the variable to the editor.
3. Set the property hint as a `File`.
4. Specify filter as `uid`.

Example- `C#`
```
[Export(PropertyHint.File, "uid")]
private string myResourcePath = "";
Resource myResource = ResourceLoader.Load(myResourcePath);
```
Example- `GDScript`
```
@export_file("uid")
var my_resource_path: String = ""
var my_resource: Resource = load(my_resource_path)
```

## Installation
**(Must have C# enabled editor. Tested on `v4.3.stable.mono, v4.4.1.stable.mono`)**
1. Place base directory in the `addons` folder of your project.
2. Make sure to press the build button to compile `C#` assemblies.
3. Enable in the plugins tab.

## What is this useful for?
Recent Godot versions have improved UID support. Referencing files with an ID can be beneficial over using a traditional file path because when paths change, the original reference becomes outdated, and then things break. But for UIDs, they should stay the same regardless of the file location. 

Great! So what's the problem?

A downside of UIDs is that one can't tell the location of the file by just looking at it since it's not a path or which file it's supposed to be referring to. For example, when you see a file path that says `res://my_data/MyResource.tres` you can figure out where it is and what the resource is. However, with UIDs `uid://1234567891012` you can't tell either. You'd have to cross reference the UID in some code editor to see which resource the UID belongs to.

Until *now*! With this plugin, UIDs populated in the inspector will convert the file UID to its corresponding file path. It's based off the UID so it's just a utility to provide a way to show what the UID represents. On top of that, the plugin adds additional utility that is just generally useful to have in the inspector because the button below will expand the resource or navigate to the file system location.

Lastly, exporting the file path as opposed to just the resource itself may be preferable because there's a greater degree of control how the resource is loaded which can be useful in many scenarios.