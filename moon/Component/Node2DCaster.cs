using Godot;

namespace Component;

[GlobalClass]
public partial class Node2DCaster : Node
{
    [ExportCategory("Node2DCaster")]
    [Export]
    public Node2D Root { get ;set; }

    [Export]
    public PackedScene Node2DScene { get ;set; }
    
    [Export]
    public Vector2 Offset { get ;set; }
    
    [Export]
    public int BufferCount { get ;set; } = 1;
    
    [Signal]
    public delegate void CastedEventHandler(Node2D node);
    
    public AsyncLoader<Node2D> Node2DLoader { get ;set; }

    public Node2DCaster() : base()
    {
        TreeEntered += () =>
        {
            Node2DLoader = new(this, Node2DScene, BufferCount);
        };
    }

    public void Cast()
    {
        var node = Node2DLoader.Create();
        node.Position = Root.Position + Offset;
        EmitSignal(SignalName.Casted, node);
        Root.AddSibling(node);
    }
}