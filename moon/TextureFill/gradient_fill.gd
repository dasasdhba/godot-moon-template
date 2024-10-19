@tool
extends Control
class_name GradientFill

@export_category("GradientFill")
@export var gradient_color :Gradient
@export_enum("Horizontal", "Vertical") var mode :int = 1
@export var flip :bool = false
@export var dynamic :bool = false

var rect :TextureRect = null
var tex :GradientTexture1D = null

const GRADIENT_NAME :String = "__GradientFillInternalRect"

func _enter_tree() -> void:
	if has_node(GRADIENT_NAME):
		get_node(GRADIENT_NAME).queue_free()

func render() ->void:
	if gradient_color == null:
		if rect != null:
			rect.texture = null
		return

	if rect == null:
		rect = TextureRect.new()
		rect.use_parent_material = true
		rect.name = GRADIENT_NAME
		add_child(rect, false, INTERNAL_MODE_FRONT)
	
	rect.flip_h = flip
	if tex == null || tex.gradient != gradient_color:
		tex = GradientTexture1D.new()
		tex.gradient = gradient_color
	
	if mode == 1:
		tex.width = size.y as int
	else:
		tex.width = size.x as int
	
	rect.texture = tex;
	if mode == 1:
		rect.size.x = size.y
		rect.size.y = size.x
		rect.position = Vector2(size.x, 0)
		rect.rotation = PI / 2
	else:
		rect.size = size
		rect.position = Vector2(0, 0)
		rect.rotation = 0

func _ready() ->void:
	render()

func _process(delta :float) ->void:
	if Engine.is_editor_hint() or dynamic:
		render()
