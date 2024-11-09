using System;
using System.Collections.Generic;
using Utils;

namespace Godot;

[GlobalClass]
public partial class BTRoot : Node
{
    [ExportCategory("BTRoot")]
    [Export]
    public bool Paused { get; set; } = false;

    [Export]
    public bool AutoStart {  get; set; } = false;

    public enum BTRootProcessCallback { Idle, Physics }

    [Export]
    public BTRootProcessCallback ProcessCallback { get; set; } = BTRootProcessCallback.Idle;

    [Signal]
    public delegate void BTStartedEventHandler(BTNode node);

    [Signal]
    public delegate void BTFinishedEventHandler();

    protected int CurrentIndex { get; set; } = 0;
    protected BTNode CachedNode { get; set; }

    protected bool Active { get; set; } = false;
    protected bool ReadyHint {  get; set; } = false;

    protected List<BTNode> BTNodes { get; set; } = new();
    
    public void Start()
    {
        CurrentIndex = 0;
        ReadyHint = false;
        ClearCache();
        Active = true;

        foreach (var node in BTNodes)
        {
            if (node.Persistent)
                node.BTReset();
        }
    }

    public void Stop()
    {
        if (!Active) return;

        ClearCache();
        Active = false;
        EmitSignal(SignalName.BTFinished);
    }

    public bool IsActive() => Active;

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
        CallDeferred(Node.MethodName.AddSibling, CachedNode);
        return CachedNode;
    }

    protected void ClearCache()
    {
        if (IsInstanceValid(CachedNode))
            CachedNode.QueueFree();
    }

    public void BTProcess(double delta)
    {
        if (!Active) return;

        if (CurrentIndex >= BTNodes.Count)
        {
            Stop();
            return;
        }

        var node = BTNodes[CurrentIndex];
        if (!node.Persistent)
            node = GetCacheNode(node);
        
        if (!ReadyHint)
        {
            ReadyHint = true;
            EmitSignal(SignalName.BTStarted, node);
            node.BTReady();
        }

        if (node.BTProcess(delta))
        {
            node.BTFinish();
            ReadyHint = false;
            if (node.End)
            {
                Stop();
                return;
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
    }

    public BTRoot() : base()
    {
        ChildEnteredTree += (Node node) =>
        {
            if (node is BTNode bt)
                BTNodes.Add(bt);

            if (node is BTSubRoot sub)
                sub.Root = this;
        };

        ChildExitingTree += (Node node) =>
        {
            if (node is BTNode bt)
                BTNodes.Remove(bt);
        };

        TreeEntered += () =>
        {
            this.AddProcess((delta) =>
            {
                if (!Paused)
                    BTProcess(delta);
            }, () => ProcessCallback == BTRootProcessCallback.Physics);
        };

        Ready += () =>
        {
            if (AutoStart) Start();
        };
    }
}