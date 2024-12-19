# Physics2D

## The Physics Layer

It's recommended to use `layer 32` for general physics collision, and put anything else which needs overlapping test in `layer 1`.

## Discarded Area

We don't use any moniterable `Area2D` and avoid using it to do overlapping test.
`Area2D` should only be used for RigidBody's gravity override or as a simple trigger.

The main reason is that `Area2D` does not provide a reliable overlapping query: it may delay a few frames or even ignore any static bodies, which can cause unexpected behavior.

Using `StaticBody2D` with an `OverlappingSycn2D` node is recommended for general overlapping test, though it's costly and may cause performance issues.

## Object Identification

Here we introduce some main approaches to identify overlapping objects:

1. Using metadata (or custom data layer for tilesets) tags. `OverlapResult2D.GetData(string tag)` may be useful here. This approch is simple and suitable for tileset, but may be hard to store more useful information. For general nodes, it's possible to store object reference in the metadata, but for tileset we don't have any simple pattern for this, except manually update the whole TileMap node. By the way, as the tags are string type, we'd better make detailed documentations for them.

2. Using unique physics layer. We can ensure we only put specific objects in a specific physics layer. This approach is similar to data tags but may have better performance, though there are only 32 physics layers available in Godot.

3. Using specific class or interface query. This ensures that we can get the specific objects, but we have to design inhenritance or interface, which is not always possible. Also, this approach is impossible for tileset, as a result we cannot use auto tile feature in this case.
