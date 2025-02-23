using System.Collections.Generic;

namespace Godot;

[GlobalClass]
public partial class BTSubRoot : BTNode
{
    protected int CurrentIndex { get; set; } = 0;
    protected BTNode CachedNode { get; set; }
    protected bool ReadyHint {  get; set; } = false;

    protected List<BTNode> BTNodes { get; set; } = new();

    public override void _Ready() => Persistent = true;

    public override void BTReset()
    {
        CurrentIndex = 0;
        CachedNode = null;
        ReadyHint = false;
    }

    public override void BTReady()
    {
        CurrentIndex = 0;
        ReadyHint = false;
        ClearCache();

        foreach (var node in BTNodes)
        {
            if (node.Persistent)
                node.BTReset();
        }
    }

    public BTNode GetCurrentBTNode()
    {
        if (CurrentIndex < 0 || CurrentIndex >= BTNodes.Count)
            return null;

        return BTNodes[CurrentIndex];
    }

    protected BTNode GetCacheNode(BTNode node)
    {
        if (IsInstanceValid(CachedNode)) return CachedNode;

        CachedNode = (BTNode)node.Duplicate();
        Root.CallDeferred(Node.MethodName.AddSibling, CachedNode);
        return CachedNode;
    }

    protected void ClearCache()
    {
        if (IsInstanceValid(CachedNode))
            CachedNode.QueueFree();
    }
    
    private BTNode CurrentBTNode;
    
    public override bool BTProcess(double delta)
    {
        if (CurrentIndex >= BTNodes.Count)
        {
            ClearCache();
            return true;
        }

        var node = BTNodes[CurrentIndex];
        if (!node.Persistent)
            node = GetCacheNode(node);
        CurrentBTNode = node;    
        
        if (!ReadyHint)
        {
            ReadyHint = true;
            Root.EmitSignal(BTRoot.SignalName.BTStarted, node);
            node.BTReady();
        }

        if (node.BTProcess(delta))
        {
            node.BTFinish();
            CurrentBTNode = null;
            ReadyHint = false;
            if (node.End)
            {
                ClearCache();
                return true;
            }

            var next = node.BTNext();
            if (next == null)
                CurrentIndex++;
            else
            {
                if (BTNodes.Contains(next))
                    CurrentIndex = BTNodes.IndexOf(next);
                else
                    CurrentIndex++;
            }

            ClearCache();
        }

        return false;
    }

    public override void BTStop()
    {
        CurrentBTNode?.BTStop();
        ClearCache();
    }

    public BTSubRoot() : base()
    {
        ChildEnteredTree += (Node node) =>
        {
            if (node is BTNode bt)
            {
                bt.Root = Root;
                BTNodes.Add(bt);
            }
            
        };

        ChildExitingTree += (Node node) =>
        {
            if (node is BTNode bt)
                BTNodes.Remove(bt);
        };
    }
}