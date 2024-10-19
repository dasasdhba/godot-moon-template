@tool
extends Control
class_name ColorFill

@export_category("ColorFill")
@export var color :Color = Color(0,0,0,1)
@export var dynamic :bool = false
@export var debug_only :bool = false

func _draw() ->void:
	if not Engine.is_editor_hint() and debug_only:
		return

	var rect: Rect2 = Rect2(Vector2.ZERO, size)
	draw_rect(rect, color)

func render() ->void:
	queue_redraw()

func _ready() ->void:
	render()

func _process(delta :float) ->void:
	if Engine.is_editor_hint() or dynamic:
		render()
