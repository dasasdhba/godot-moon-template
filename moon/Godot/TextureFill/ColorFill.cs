namespace Godot;

[GlobalClass, Tool]
public partial class ColorFill : NodeSize2D
{
    [ExportCategory("ColorFill")]
    [Export]
    public Color Color
    {
        get => _Color;
        set
        {
            _Color = value;
            QueueRedraw();
        }
    }
    
    private Color _Color = new(0f, 0f, 0f, 1f);

    public override void _Draw()
    {
        DrawRect(new (new(0f, 0f), Size), Color);
    }

    public ColorFill() : base()
    {
        TreeEntered += QueueRedraw;
        SignalSizeChanged += QueueRedraw;
    }
}