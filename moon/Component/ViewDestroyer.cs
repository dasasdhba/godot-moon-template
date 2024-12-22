using Godot;
using Godot.Collections;
using Utils;

namespace Component;

[GlobalClass, Tool]
public partial class ViewDestroyer : Node
{
    [ExportCategory("ViewDestroyer")]
    [Export]
    public CanvasItem MonitorNode { get; set; }
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum ViewDestroyerProcessCallback { Idle, Physics }
    
    [Export]
    public ViewDestroyerProcessCallback ProcessCallback { get; set; } = ViewDestroyerProcessCallback.Physics;
    
    public enum MonitorMode { Direction, Manual }

    [ExportGroup("ViewSettings")]
    [Export]
    public MonitorMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            NotifyPropertyListChanged();
        }    
    }
    
    private MonitorMode _mode;
    
    [Export]
    public Vector2 Direction { get ;set; } = Vector2.Down;
    
    [Export]
    public float Range { get; set; } = 48f;
    
    [Export]
    public float UpRange { get; set; } = 48f;
    
    [Export]
    public float DownRange { get; set; } = 48f;
    
    [Export]
    public float LeftRange { get; set; } = 48f;
    
    [Export]
    public float RightRange { get; set; } = 48f;
    
    public override void _ValidateProperty(Dictionary property)
    {
        if (
            (Mode == MonitorMode.Direction && (string)property["name"] is
                "UpRange" or "DownRange" or "LeftRange" or "RightRange") ||
            (Mode == MonitorMode.Manual && (string)property["name"] is 
                "Direction" or "Range")
        )
        {
            property["usage"] = (uint)PropertyUsageFlags.ReadOnly;
        }
    }

    public override void _EnterTree()
    {
#if TOOLS
        if (Engine.IsEditorHint()) return;        
#endif

        this.AddProcess(Process, () => ProcessCallback == ViewDestroyerProcessCallback.Physics);
    }

    public void Process()
    {
        if (Disabled) return;

        if (Mode == MonitorMode.Direction)
        {
            if (Range > 0f && !MonitorNode.IsInViewDir(Direction, Range))
                MonitorNode.QueueFree();
            return;
        }

        if (UpRange > 0f && !MonitorNode.IsInViewTop(UpRange))
            MonitorNode.QueueFree();
        if (DownRange > 0f && !MonitorNode.IsInViewBottom(DownRange))
            MonitorNode.QueueFree();
        if (LeftRange > 0f && !MonitorNode.IsInViewLeft(LeftRange))
            MonitorNode.QueueFree();
        if (RightRange > 0f && !MonitorNode.IsInViewRight(RightRange))
            MonitorNode.QueueFree();
    }

}