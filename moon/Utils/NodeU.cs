using Godot;
using System;

namespace Utils;

/// <summary>
/// managed delta Process Node with internal node approach
/// </summary>
public static partial class NodeU
{
    public static void SetURate(this Node node, double rate)
    {
        foreach (var child in node.GetChildren(true))
        {
            if (child is DelegateNode uNode) uNode.Rate = rate;
            else SetURate(child, rate);
        }
    }

    public partial class DelegateNode : Node
    {
        public Action<double> ProcessAction { get ;set; }
        public Func<bool> IsPhysics { get; set; }
        public double Rate { get ;set; } = 1d;

        protected void DelegateProcess(double delta)
        {
            if (Rate <= 0d) return;
            ProcessAction?.Invoke(delta * Rate);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsPhysics != null && IsPhysics.Invoke())
                DelegateProcess(delta);
        }

        public override void _Process(double delta)
        {
            if (IsPhysics == null || !IsPhysics.Invoke())
                DelegateProcess(delta);
        }
    }
    
    public partial class DelegateIdleNode : DelegateNode
    {
        public override void _Process(double delta)
        {
            DelegateProcess(delta);
        }
    }
    
    public partial class DelegatePhysicsNode : DelegateNode
    {
        public override void _PhysicsProcess(double delta)
        {
            DelegateProcess(delta);
        }
    }
    
    public static DelegateNode AddProcess(this Node root, Action<double> process, Func<bool> isPhysics)
    {
        var uNode = new DelegateNode()
        {
            ProcessAction = process,
            IsPhysics = isPhysics
        };

        root.TreeExited += uNode.QueueFree;
        root.AddChild(uNode, false, Node.InternalMode.Front);

        return uNode;
    }

    public static DelegateNode AddProcess(this Node root, Action<double> process, bool physics = false)
    {
        DelegateNode uNode = physics ? new DelegatePhysicsNode() : new DelegateIdleNode();
        uNode.ProcessAction = process;
        
        uNode.Ready += () =>
        {
            if (physics) uNode.SetProcess(false);
            else uNode.SetPhysicsProcess(false);
        };
        
        uNode.BindParent(root);
        root.TreeExited += uNode.QueueFree;
        root.AddChild(uNode, false, Node.InternalMode.Front);
        
        return uNode;
    }
        
    public static DelegateNode AddPhysicsProcess(this Node root, Action<double> process)
        => AddProcess(root, process, true);
        
    public static DelegateNode AddProcess(this Node root, Action process, Func<bool> isPhysics)
        => AddProcess(root, (double delta) => process(), isPhysics);

    public static DelegateNode AddProcess(this Node root, Action process, bool physics = false)
        => AddProcess(root, (double delta) => process(), physics);

    public static DelegateNode AddPhysicsProcess(this Node root, Action process)
        => AddProcess(root, process, true);

    // Action

    public static UTimer ActionDelay(this Node node, double delay, Action action, bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = true,
            OneShot = true,
            WaitTime = delay,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.SignalTimeout += () =>
        {
            action.Invoke();
            if (timer.OneShot)
                timer.QueueFree();
        }; 
        
        timer.BindParent(node);
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    public static UTimer ActionPhysicsDelay(this Node node, double delay, Action action)
        => ActionDelay(node, delay, action, true);

    public static UTimer ActionRepeat(this Node node, double interval, Action action, bool autostart = true, bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = autostart,
            OneShot = false,
            WaitTime = interval,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.SignalTimeout += () =>
        {
            action.Invoke();
        }; 
        
        timer.BindParent(node);
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    public static UTimer ActionPhysicsRepeat(this Node node, double interval, Action action, bool autostart = true)
        => ActionRepeat(node, interval, action, autostart, true);
}