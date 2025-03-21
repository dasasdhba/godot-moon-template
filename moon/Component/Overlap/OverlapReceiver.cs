using Godot;
using Utils;

namespace Game;

[GlobalClass]
public partial class OverlapReceiver : Node
{
    public const string RefTag = "OverlapReceiver";
    public static bool HasRef(GodotObject node)
        => node.HasData(RefTag);
    public static OverlapReceiver GetRef(GodotObject node)
        => node.GetData<OverlapReceiver>(RefTag);
    
    [ExportCategory("OverlapReceiver")]
    [Export]
    public CollisionObject2D Body
    {
        get => _Body;
        set
        {
            if (_Body != value)
            {
                _Body?.RemoveData(RefTag);
                value?.SetData(RefTag, this);
                
                _Body = value;
            }
        }
    }
    private CollisionObject2D _Body;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    [Signal]
    public delegate void MonitorEnteredEventHandler(Variant data);
    
    [Signal]
    public delegate void MonitorExitedEventHandler(Variant data);
    
    public virtual void MonitorOverlapped(Variant data) {}
    
    public virtual bool IsDisabled() => Disabled;
}