using Godot;
using Utils;

namespace Component;

[GlobalClass]
public abstract partial class OverlapReceiver : Node
{
    private const string RefTag = "OverlapReceiver";
    public static bool HasRef(GodotObject node, string key = "")
        => node.HasData($"{RefTag}_{key}");
    public static OverlapReceiver GetRef(GodotObject node, string key = "")
        => node.GetData<OverlapReceiver>($"{RefTag}_{key}");
    
    protected abstract string GetReceiverKey();
    private string GetDataKey() => $"{RefTag}_{GetReceiverKey()}";
    
    [ExportCategory("OverlapReceiver")]
    [Export]
    public CollisionObject2D Body
    {
        get => _Body;
        set
        {
            if (_Body != value)
            {
                _Body?.RemoveData(GetDataKey());
                value?.SetData(GetDataKey(), this);
                
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
    
    public virtual bool IsDisabled() => Disabled || !CanProcess();
}