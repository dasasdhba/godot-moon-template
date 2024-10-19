# View

* `View2D` is an alternate of Godot `Camera2D`. The main reason here is that Godot `Camera2D` provides a very weird camera rotation behavior. With `View2D`, you can setup the rotation origin much easier. Also, I personlly really dislike the approach of Godot `Camera2D`'s smooth changing region, it just looks so weird.

* `View2DSetting` and `View2DRect` are tools to quickly setup `View2D`.

* `View2DExtension` provides some common used functions for entering view detection.
