using Godot;
using Utils;

namespace Component;

[GlobalClass]
public partial class Rotator : Node
{
    /// <summary>
    /// Default value is parent.
    /// </summary>
    [ExportCategory("Rotator")]
    [Export]
    public CanvasItem RotateNode { get; set; }
    
    [Export]
    public float Speed { get; set; } = 500f;
    
    [Export]
    public bool Flip { get; set; } = false;
    
    public enum RotatorProcessCallback { Idle, Physics }
    
    [Export]
    public RotatorProcessCallback ProcessCallback { get; set; } = RotatorProcessCallback.Idle;

    public Rotator() : base()
    {
        TreeEntered += () =>
        {
            if (RotateNode == null && GetParent() is CanvasItem parent) RotateNode = parent;
            this.AddProcess(RotateProcess, () => ProcessCallback == RotatorProcessCallback.Physics);
        };
    }

    private void RotateProcess(double delta)
    {
        if (RotateNode == null) return;
        
        var rotation = (float)RotateNode.Get(Node2D.PropertyName.Rotation);
        rotation += (float)Mathf.DegToRad(Speed * delta) * (Flip ? -1 : 1);
        rotation = Mathf.Wrap(rotation, -float.Pi, float.Pi);
        RotateNode.Set(Node2D.PropertyName.Rotation, rotation);
    }
}