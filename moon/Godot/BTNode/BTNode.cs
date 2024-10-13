namespace Godot;

[GlobalClass]
public partial class BTNode : Node
{
    /// <summary>
    /// for non persistent one, process will be called in a copy.
    /// in which case, the original BTNode will be kept as static.
    /// </summary>
    [ExportCategory("BTNode")]
    [Export]
    public bool Persistent { get; set; } = false;

    /// <summary>
    /// null for next by default
    /// </summary>
    [Export]
    public BTNode NextNode { get; set; }

    /// <summary>
    /// end the BTProcess
    /// </summary>
    [Export]
    public bool End { get ;set; } = false;
    
    public virtual void BTReady() { }
    
    public virtual void BTFinish() { }

    /// <summary>
    /// return true to end current BTProcess
    /// </summary>
    public virtual bool BTProcess(double delta) => true;

    /// <summary>
    /// Used for persistent node to reset
    /// </summary>
    public virtual void BTReset() { }

    /// <summary>
    /// define the next BTNode, NextNode property is used by default.
    /// </summary>
    public virtual BTNode BTNext() => NextNode;
}