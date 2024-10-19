@tool
extends Control
class_name TextureFill

@export_category("TiledFill")
@export var texture :Texture2D
@export var flip_h :bool = false
@export var flip_v :bool = false
@export var dynamic :bool = false

var rect :TextureRect = null

const TEXTURE_NAME :String = "__TextureFillInternalRect"

func _enter_tree() -> void:
	if has_node(TEXTURE_NAME):
		get_node(TEXTURE_NAME).queue_free()

func render() ->void:
	if texture == null:
		if rect != null:
			rect.texture = null
		return

	if rect == null:
		rect = TextureRect.new()
		rect.use_parent_material = true
		rect.name = TEXTURE_NAME
		add_child(rect, false, INTERNAL_MODE_FRONT)
	
	rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	rect.stretch_mode = TextureRect.STRETCH_TILE
	rect.flip_h = flip_h
	rect.flip_v = flip_v
	rect.texture = texture
	rect.position = Vector2.ZERO
	rect.size = size

func _ready() ->void:
	render()

func _process(delta :float) ->void:
	if Engine.is_editor_hint() or dynamic:
		render()
