# Aseprite

Aseprite is used to import `SpriteFrames` quickly.

## Installation

Aseprite is an excellent pixel art/animation editor, and it is also an open-source project with a loose license. You may buy license to support its development, or you can also build it on your own. See [Aseprite GitHub Pages](https://github.com/aseprite/aseprite).

For Chinese mainland user, Here also exists a verison with Simplified Chinese translation: [Aseprite-zh Download](https://www.ghxi.com/aseprite.html).

It's recommended to add Aseprite installation directory to the system PATH environment variable, otherwise you have to setup the path manually latter in `Editor Settings -> General -> Aseprite`.

Finally, build the C# solution when you first open the project, Then move to `Project Setting -> Plugins` and activate the `Aseprite Importer` plugin.

## Usage

The importer will auto generate `png` file in the same directory and import `aseprite` file as `SpriteFrames`. By default it will include all layers, you may change this in the import setting if you only need to import visible layers.

Here are two animation generating patterns:

1. Use `Tag` in Aseprite. You can set up `Tag` in Aseprite, then the importer will use the tag name to create animation. The tag direction `Forward`, `Reverse`, `Ping-pong` is also supported. However, the untagged part will be ignored while the spritesheet is still entire, please avoid this as possible.

2. Use seperate aseprite files. You can create several different aseprite files with the same `Basename` and different `AnimationName` by using a dot `.` separator. For exmaple, putting `mario.Walk.aseprite` and `mario.Jump.aseprite` in the same folder will let the importer generate a single `SpriteFrames` named `mario.tres` with two animations `Walk` and `Jump`. If there is no `Animation Name` found, then the importer will use `Default` as animation name. (Please avoid using `Default` in `AnimationName` or using `.` in `Basename`, as it may cause conflicts.)

These two patterns can also be used together, the importer will only use `Tag` if `Tag Only` option is enabled, otherwise the tag name will be appended to the animation name.

The animation will be imported as looped by default import settings, if you want to make a specific `Tag` oneshot, you can add `_oneshot` suffix to the tag name.
