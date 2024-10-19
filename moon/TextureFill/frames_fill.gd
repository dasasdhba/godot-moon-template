@tool
extends Control
class_name FramesFill

# we cannot use tiled TextureRect with atlas
# see https://github.com/godotengine/godot/issues/20472 for more details

# current approach does not ignore the out view part
# but for most case it should be fine

@export_category("FramesFill")
@export var frames :SpriteFrames
@export var animation :String = "Default"
@export var speed_scale :float = 1
@export var pause :bool = false
@export var flip_h :bool = false
@export var flip_v :bool = false
@export var dynamic :bool = false

var anim :String = ""
var count :int = 0
var wait :bool = false
var timer :Timer

const TIMER_NAME :String = "__FramesFillInternalTimer"

func _enter_tree() -> void:
	if has_node(TIMER_NAME):
		get_node(TIMER_NAME).queue_free()

func render() ->void:
	if timer == null:
		timer = Timer.new()
		timer.timeout.connect(on_timer_timeout)
		timer.name = TIMER_NAME
		add_child(timer, false, INTERNAL_MODE_FRONT)
		
	render_animation()
	
func render_animation() ->void:
	if frames == null or not frames.has_animation(animation):
		queue_redraw()
		return
	
	if anim != animation:
		anim = animation
		count = 0
		wait = false
		timer.stop()
		queue_redraw()
		
	if pause or speed_scale <= 0:
		wait = false
		timer.stop()
		return
		
	if not wait:
		wait = true
		timer.wait_time = 1 / (frames.get_animation_speed(anim) * speed_scale) * frames.get_frame_duration(anim, count)
		timer.start()
	
func on_timer_timeout() ->void:
	if not wait:
		return
	wait = false

	count += 1
	if count >= frames.get_frame_count(animation):
		count = 0
	queue_redraw()

func _draw() ->void:
	if size.x <= 0 or size.y <= 0 or frames == null or not frames.has_animation(animation):
		return
	
	var tp :Vector2 = Vector2.ZERO
	var ts :Vector2 = Vector2.ONE
	if flip_h:
		tp.x += size.x
		ts.x = -1
	if flip_v:
		tp.y += size.y
		ts.y = -1
	draw_set_transform(tp, 0, ts)
	
	var tex :Texture2D = frames.get_frame_texture(animation, count)
	var unit :Vector2 = tex.get_size()
	var pos :Vector2i = Vector2i.ZERO
	while pos.y * unit.y < size.y:
		pos.x = 0
		while pos.x * unit.x < size.x:
			var p :Vector2 = Vector2(pos.x * unit.x, pos.y * unit.y)
			var s :Vector2 = Vector2(min(unit.x, size.x - p.x), min(unit.y, size.y - p.y))
			var rect :Rect2 = Rect2(p,s)
			draw_texture_rect_region(tex, rect, Rect2(Vector2.ZERO, s))
			pos.x += 1
		pos.y += 1

func _ready() ->void:
	render()

func _process(delta :float) ->void:
	if Engine.is_editor_hint() or dynamic:
		render()
	else:
		render_animation()
