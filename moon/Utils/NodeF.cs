﻿using Godot;
using System;

namespace Utils;

/// <summary>
/// managed delta Process Node with SceneTree signal approach
/// </summary>
public static partial class NodeF
{
    private const string NodeFRateTag = "__NodeFRate";

    public static void SetFRateRecursively(this Node node, double rate, bool includeInternal = false)
    {
        node.SetFRate(rate);
        node.SetChildrenRecursively(child => child.SetFRate(rate), includeInternal);
    }

    public static void SetFRate(this Node node, double rate)
        => node.SetMeta(NodeFRateTag, rate);

    public static double GetFRate(this Node node)
        => (double)node.GetMeta(NodeFRateTag, 1d);

    public class DelegateProcess(Node node)
    {
        protected Node Root { get; set; } = node;
        public Action<double> ProcessAction { get ;set; }

        public void ProcessInvoke(double delta)
        {
            if (!Root.CanProcess()) return;
            
            var rate = Root.GetFRate();
            if (rate <= 0d) return;
            
            ProcessAction?.Invoke(delta * rate);
        }
    }
    
    public static DelegateProcess AddProcessFront(this Node root, Action<double> process, Func<bool> isPhysics)
    {
        var p = new DelegateProcess(root) { ProcessAction = process };

        void IdleProcess()
        {
            if (isPhysics == null || !isPhysics.Invoke())
                p.ProcessInvoke(root.GetProcessDeltaTime());
        }
        void PhysicsProcess()
        {
            if (isPhysics != null && isPhysics.Invoke())
                p.ProcessInvoke(root.GetPhysicsProcessDeltaTime());
        }
        
        var tree = root.GetTree();
        tree.ProcessFrame += IdleProcess;
        tree.PhysicsFrame += PhysicsProcess;
        
        root.TreeExited += () =>
        {
            tree.ProcessFrame -= IdleProcess;
            tree.PhysicsFrame -= PhysicsProcess;
        };
        
        return p;
    }

    public static DelegateProcess AddProcessFront(this Node root, Action<double> process, bool physics = false)
    {
        var p = new DelegateProcess(root) { ProcessAction = process };
        
        Action action = physics ? 
            () => p.ProcessInvoke(root.GetPhysicsProcessDeltaTime()) : 
            () => p.ProcessInvoke(root.GetProcessDeltaTime());
        
        var tree = root.GetTree();
        if (physics) tree.PhysicsFrame += action;
        else tree.ProcessFrame += action;
        
        root.TreeExited += () =>
        {
            if (physics) tree.PhysicsFrame -= action;
            else tree.ProcessFrame -= action;
        };
        
        return p;
    }
        
    public static DelegateProcess AddPhysicsProcessFront(this Node root, Action<double> process)
        => AddProcessFront(root, process, true);
        
    public static DelegateProcess AddProcessFront(this Node root, Action process, Func<bool> isPhysics)
        => AddProcessFront(root, (double delta) => process(), isPhysics);

    public static DelegateProcess AddProcessFront(this Node root, Action process, bool physics = false)
        => AddProcessFront(root, (double delta) => process(), physics);

    public static DelegateProcess AddPhysicsProcessFront(this Node root, Action process)
        => AddProcessFront(root, process, true);
}