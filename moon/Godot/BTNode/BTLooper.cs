namespace Godot;

[GlobalClass]
public partial class BTLooper : BTNode
{
    [ExportCategory("BTLooper")]
    [Export]
    public int LoopTimes { get; set; } = 1;

    [Export]
    public BTNode LoopNode {  get; set; }

    protected int LoopCount { get; set; } = 0;

    public override void _Ready() => Persistent = true;

    public override BTNode BTNext()
    {
        if (LoopTimes <= 0) return LoopNode;

        LoopCount++;
        if (LoopCount <= LoopTimes)
            return LoopNode;
        else
        {
            LoopCount = 0;
            return base.BTNext();
        }
    }

    public override void BTReset() => LoopCount = 0;
}