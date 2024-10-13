namespace Godot;

[GlobalClass]
public partial class BTimer : BTNode
{
    [ExportCategory("BTimer")]
    [Export]
    public float WaitTime {  get; set; } = 1f;

    protected double Timer { get; set; } = 0d;

    public override bool BTProcess(double delta)
    {
        Timer += delta;

        return Timer >= WaitTime;
    }
}