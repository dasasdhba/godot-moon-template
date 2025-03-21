using System.Collections.Generic;
using Utils;

namespace Godot;

[GlobalClass]
public partial class NodePool : Node
{
    [ExportCategory("NodePool")]
    [Export]
    public int PoolSize { get; set; } = 100;
    
    /// <summary>
    /// If true, the pool will not create new nodes when the pool is empty.
    /// </summary>
    [Export]
    public bool Strict { get ;set; } = false;
    
    /// <summary>
    /// Make sure the pool objects can init when enter tree.
    /// </summary>
    [Export]
    public PackedScene PoolScene { get; set; }
    
    private Stack<Node> Pool = [];

    public NodePool() : base()
    {
        TreeEntered += () =>
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var node = CreatePoolNode();
                Pool.Push(node);
            }
        };
        
        TreeExited += () =>
        {
            foreach (var node in Pool) node.QueueFree();
            Pool.Clear();
        };
    }

    private Node CreatePoolNode()
    {
        var node = PoolScene.InstantiateSafely();
        SetPool(node);
        
        return node;
    }
    
    public int GetPoolCount() => Pool.Count;
    public Node GetPoolNode()
        => Pool.Count == 0 ? (Strict ? null : CreatePoolNode()) : Pool.Pop();
    
    private void SetPool(Node node) => node.SetData(PoolNodeData, this);
    public void ReturnPool(Node node) => Pool.Push(node);
    
    private const string PoolNodeData = "PoolNode";
    public static bool IsInPool(Node node) => node.HasData(PoolNodeData);
    public static NodePool GetPool(Node node) => node.GetData<NodePool>(PoolNodeData);
}