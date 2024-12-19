# Platformer2D

`Platformer2D` provides a most common physics body implementation for 2d platform games: General gravity (including underwater) simulation, moving and turning behavior, etc.

There are two implementations of `IPlatformer2D`--`CharaPlatformer2D` and `RigidPlatformer2D`, both of which can be manipulated by `PlatformerMove2D` node.

## Underwater

Any `body` (not `Area2D`) in `WaterMask` (default 1) layer with `metadata` (or `custom_data` for `TileSet`) `Water` set to be `true` will be treated as water area. To make the body floating in the water, set `FloatingHeight` to be positive.

Hint: it's not recommended to change the collision layer of "Water Body", as the physics body may ignore it forever. Try to change the `WaterMask` of physics body instead, or simply move the water bodies away.

## Move and Turn

A component `PlatformerMove2D` is used to implment general moving and turning behavior for `IPlatformer2D`. Both `Linear` and `Acclerate` mode are useful in many cases.

## Chara vs Rigid

Generally `CharaterBody2D` is more recommended for most entites in 2d platform games as it is more controllable. However, when it comes to stacked boxes, `CharaterBody2D` is very unstable while `RigidBody2D` performs much better in this case.

Manipulate `RigidBody2D` is not easy, `RigidPlatformer2D` tries best to provide the same API as `CharacterBody2D` owns e.g. `IsOnFloor()`, `IsOnWall()`. It also provides a max gravity speed limitation as most 2d platform games do. However, these API is not as reliable as `CharacterBody2D`, which can cause issue when used together with `PlatformerMove2D`. For general usage, it's recommended to provide a rounded collision shape for `RigidPlatformer2D`, which can improve the accuracy of API metioned above. When it comes to stacked boxes, `PlatformerMove2D` performs badly with `RigidPlatformer2D`, so it's recommended to do some manual manipulation directly in this case.
