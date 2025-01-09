using Godot;
using Utils;

namespace Component;

[GlobalClass]
public partial class SpriteDir : Node, IFlipInit
{
    /// <summary>
    /// The monitored moving node.
    /// </summary>
    [ExportCategory("SpriteDir")]
    [Export]
    public CanvasItem Root { get ;set; }
    
    /// <summary>
    /// Default value is parent.
    /// </summary>
    [Export]
    public CanvasItem Sprite { get ;set; }
    
    [Export]
    public Rotator Rotator { get ;set; }
    
    [Export]
    public bool Flip { get ;set; }
    
    [Export]
    public bool Disabled { get ;set; }

    private MotionRecorder2D Recorder;
    public override void _EnterTree()
    {
        if (Sprite == null && GetParent() is CanvasItem parent) Sprite = parent;
        if (Root != null && Root is not IPlatformer2D)
        {
            Recorder = new() { Target = Root };
            Recorder.BindParent(this);
            AddChild(Recorder);
        }
    }

    public void FlipInit()
    {
        Connect(
            Node.SignalName.TreeEntered,
            Callable.From(() => SetSpriteFlip(true)),
            (uint)ConnectFlags.OneShot
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Root is IPlatformer2D platformer)
        {
            var s = platformer.GetLastMoveSpeed();
            if (s != 0f) SetSpriteFlip(s < 0f);
        }
        else if (Root != null)
        {
            var s = Recorder.GetLastMotion().X;
            if (s != 0f) SetSpriteFlip(s < 0f);
        }
    }

    protected void SetSpriteFlip(bool value)
    {
        if (Disabled) return;
        
        var result = Flip ? !value : value;
        
        if (Sprite is Sprite2D sprite) sprite.FlipH = result;
        else if (Sprite is AnimatedSprite2D anim) anim.FlipH = result;
        else if (Sprite is AnimGroup2D group) group.FlipH = result;
        
        if (Rotator != null) Rotator.Flip = result;
    }
}