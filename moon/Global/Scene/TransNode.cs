using Godot;

namespace Global;

public partial class TransNode : Node
{
    /// <summary>
    /// p 0->1
    /// </summary>
    public virtual void TransInProcess(double p) { }

    /// <summary>
    /// p 1->0
    /// </summary>
    public virtual void TransOutProcess(double p) => TransInProcess(p);
}