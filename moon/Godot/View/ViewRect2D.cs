namespace Godot;

[GlobalClass, Tool]
public partial class ViewRect2D : Control
{
#if TOOLS
    [ExportCategory("ViewRect2D")]
    [Export]
    public Color DebugColor = new(0f, 1f, 0f, 0.3f);

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;

        DrawRect(new (new(0f, 0f), Size), DebugColor);
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;

        QueueRedraw();
    }

#endif

    public Rect2 GetViewRect() => new(GlobalPosition, Size);
}