namespace Godot;

[GlobalClass, Tool]
public partial class ViewRect2D : DebugFill
{
    public Rect2 GetViewRect() => new(GlobalPosition, Size);
}