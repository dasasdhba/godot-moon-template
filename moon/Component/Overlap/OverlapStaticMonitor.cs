using System;
using Godot;
using Utils;

namespace Component;

/// <summary>
/// This is used to monitor overlapping with some static object
/// </summary>
public partial class OverlapStaticMonitor : Node
{
    [ExportCategory("OverlapStaticMonitor")]
    [Export(PropertyHint.Layers2DPhysics)]
    public uint CollisionMask
    {
        get => _CollisionMask;
        set
        {
            _CollisionMask = value;
            if (Overlap != null) Overlap.CollisionMask = CollisionMask;
        }
    }
    private uint _CollisionMask = 1;
    
    [Export]
    public bool Disabled { get ;set; }
    
    [Export]
    public CollisionObject2D Body { get ;set; }
    
    /// <summary>
    /// won't emit in first frame, useful for area enter/exit events
    /// </summary>
    [Signal]
    public delegate void EnteredEventHandler();
    
    [Signal]
    public delegate void ExitedEventHandler();
    
    public OverlapSync2D Overlap { get ; private set; }
    public OverlapStaticMonitor() : base()
    {
        TreeEntered += () =>
        {
            Overlap = OverlapSync2D.CreateFrom(Body);
            Overlap.CollisionMask = CollisionMask;
            this.AddPhysicsProcess(OverlapProcess);
        };
    }
    
    protected virtual Func<OverlapResult2D<GodotObject>, bool> GetStaticFilter() => null;

    public virtual bool IsOverlapping(Vector2 offset)
    {
        return Overlap.IsOverlapping(
            GetStaticFilter(),
            offset,
            true);
    }
    
    private bool Overlapped = false;
    private bool OverlappedFirst = false;
    public bool IsOverlapping(bool forceUpdate = false)
    {
        if (!forceUpdate) return Overlapped;

        Overlapped = IsOverlapping(Vector2.Zero);
        return Overlapped;
    }

    private void OverlapProcess()
    {
        if (Disabled)
        {
            OverlappedFirst = false;
            return;
        }
        
        if (!OverlappedFirst)
        {
            OverlappedFirst = true;
            IsOverlapping(true);
            return;
        }
        
        var last = Overlapped;
        var next = IsOverlapping(true);
        
        if (!last && next) EmitSignal(SignalName.Entered);
        if (last && !next) EmitSignal(SignalName.Exited);
    }
}