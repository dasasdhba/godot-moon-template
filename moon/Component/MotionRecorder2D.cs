using Godot;
using Utils;

namespace Component;

/// <summary>
/// Record motion of specific canvas item.
/// </summary>
public partial class MotionRecorder2D : Node
{
    [ExportCategory("MotionRecorder2D")]
    [Export]
    public CanvasItem Target { get ;set; }
    
    [Export]
    public bool Disabled { get ;set; } = false;
    
    private Vector2 Speed = Vector2.Zero;
    public Vector2 GetLastSpeed() => Speed;
    
    private Vector2 Motion = Vector2.Zero;
    public Vector2 GetLastMotion() => Motion;
    
    
    private Vector2 LastPosition = Vector2.Zero;
    private bool FirstRecorded = false;

    public MotionRecorder2D() : base()
    {
        TreeEntered += () =>
        {
            this.AddPhysicsProcess(delta =>
            {
                if (Disabled)
                {
                    Speed = Vector2.Zero;
                    Motion = Vector2.Zero;
                    FirstRecorded = false;
                    return;
                }
                
                var pos = (Vector2)Target.Get("global_position");

                if (!FirstRecorded)
                {
                    FirstRecorded = true;
                    LastPosition = pos;
                    return;
                }
                
                Motion = pos - LastPosition;
                Speed = Motion / (float)delta;
                LastPosition = pos;
            });
        };
    }
}