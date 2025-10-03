namespace Godot;

[GlobalClass, Tool]
#if TOOLS
public partial class DebugFill : NodeSize2D, ISerializationListener
#else
public partial class DebugFill : NodeSize2D
#endif
{
#if TOOLS
    [ExportCategory("DebugFill")]
    [Export]
    public Color DebugColor
    {
        get => _DebugColor;
        set
        {
            _DebugColor = value;
            QueueRedraw();
        }
    }
    
    private Color _DebugColor = new(0f, 1f, 0f, 0.3f);

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;

        DrawRect(new (new(0f, 0f), Size), DebugColor);
    }

    public DebugFill() : base()
    {
        if (!Engine.IsEditorHint()) return;
        
        TreeEntered += QueueRedraw;
        SignalSizeChanged += QueueRedraw;
    }

    public virtual void OnBeforeSerialize()
    {
        TreeEntered -= QueueRedraw;
        SignalSizeChanged -= QueueRedraw;
    }

    public void OnAfterDeserialize()
    {
        QueueRedraw();
    }

#endif
}