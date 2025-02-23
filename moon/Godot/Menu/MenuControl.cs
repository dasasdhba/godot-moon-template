namespace Godot;

[GlobalClass]
public partial class MenuControl : Node
{
    public virtual bool IsMovingLeft() => Input.IsActionPressed("Left");
    public virtual bool IsMovingRight() => Input.IsActionPressed("Right");
    public virtual bool IsMovingUp() => Input.IsActionPressed("Up");
    public virtual bool IsMovingDown() => Input.IsActionPressed("Down");
    public virtual bool IsSelected() => Input.IsActionJustPressed("Select");
}