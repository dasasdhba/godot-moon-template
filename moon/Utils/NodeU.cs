using Godot;
using System;

namespace Utils;

/// <summary>
/// managed delta Process Node with internal node approach
/// </summary>
public static partial class NodeU
{
    private partial class DelegateNode : Node
    {
        public Action<double> ProcessAction { get ;set; }

        protected void DelegateProcess(double delta)
        {
            ProcessAction?.Invoke(delta);
        }
    }

    private partial class DelegateDynamicNode : DelegateNode
    {
        public Func<bool> IsPhysics { get; set; }

        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    if (IsPhysics != null && IsPhysics.Invoke())
                    {
                        SetPhysicsProcessInternal(true);
                    }
                    else
                    {
                        SetProcessInternal(true);
                    }
                    break;
                case NotificationInternalProcess:
                    if (IsPhysics == null || !IsPhysics.Invoke())
                    {
                        DelegateProcess(GetProcessDeltaTime());
                    }
                    else
                    {
                        SetProcessInternal(false);
                        SetPhysicsProcessInternal(true);
                    }
                    break;
                case NotificationInternalPhysicsProcess:
                    if (IsPhysics != null && IsPhysics.Invoke())
                    {
                        DelegateProcess(GetPhysicsProcessDeltaTime());
                    }
                    else
                    {
                        SetProcessInternal(true);
                        SetPhysicsProcessInternal(false);
                    }
                    break;
            }
        }
    }
    
    private partial class DelegateIdleNode : DelegateNode
    {
        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    SetProcessInternal(true);
                    break;
                case NotificationInternalProcess:
                    DelegateProcess(GetProcessDeltaTime());
                    break;
            }
        }
    }
    
    private partial class DelegatePhysicsNode : DelegateNode
    {
        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    SetPhysicsProcessInternal(true);
                    break;
                case NotificationInternalPhysicsProcess:
                    DelegateProcess(GetPhysicsProcessDeltaTime());
                    break;
            }
        }
    }
    
    public static Node AddProcess(this Node root, Action<double> process, Func<bool> isPhysics)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{root} namely {root.GetPathTo(root.GetTree().GetEditedSceneRoot())} is trying to call AddProcess in editor, which is not expected.");
            return null;
        }
    #endif
    
        var uNode = new DelegateDynamicNode()
        {
            ProcessAction = process,
            IsPhysics = isPhysics
        };
        
        uNode.BindParent(root);
        root.AddChild(uNode, false, Node.InternalMode.Front);

        return uNode;
    }

    public static Node AddProcess(this Node root, Action<double> process, bool physics = false)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{root} namely {root.GetPathTo(root.GetTree().GetEditedSceneRoot())} is trying to call AddProcess in editor, which is not expected.");
            return null;
        }
    #endif
    
        DelegateNode uNode = physics ? new DelegatePhysicsNode() : new DelegateIdleNode();
        uNode.ProcessAction = process;
        
        uNode.BindParent(root);
        root.AddChild(uNode, false, Node.InternalMode.Front);
        
        return uNode;
    }
        
    public static Node AddPhysicsProcess(this Node root, Action<double> process)
        => AddProcess(root, process, true);
    
    private partial class DelegateRawNode : Node
    {
        public Action ProcessAction { get ;set; }

        protected void DelegateProcess()
        {
            ProcessAction?.Invoke();
        }
    }

    private partial class DelegateDynamicRawNode : DelegateRawNode
    {
        public Func<bool> IsPhysics { get; set; }

        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    if (IsPhysics != null && IsPhysics.Invoke())
                    {
                        SetPhysicsProcessInternal(true);
                    }
                    else
                    {
                        SetProcessInternal(true);
                    }
                    break;
                case NotificationInternalProcess:
                    if (IsPhysics == null || !IsPhysics.Invoke())
                    {
                        DelegateProcess();
                    }
                    else
                    {
                        SetProcessInternal(false);
                        SetPhysicsProcessInternal(true);
                    }
                    break;
                case NotificationInternalPhysicsProcess:
                    if (IsPhysics != null && IsPhysics.Invoke())
                    {
                        DelegateProcess();
                    }
                    else
                    {
                        SetProcessInternal(true);
                        SetPhysicsProcessInternal(false);
                    }
                    break;
            }
        }
    }
    
    private partial class DelegateIdleRawNode : DelegateRawNode
    {
        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    SetProcessInternal(true);
                    break;
                case NotificationInternalProcess:
                    DelegateProcess();
                    break;
            }
        }
    }
    
    private partial class DelegatePhysicsRawNode : DelegateRawNode
    {
        public override void _Notification(int what)
        {
            switch ((ulong)what)
            {
                case NotificationReady:
                    SetPhysicsProcessInternal(true);
                    break;
                case NotificationInternalPhysicsProcess:
                    DelegateProcess();
                    break;
            }
        }
    }
    
    public static Node AddProcess(this Node root, Action process, Func<bool> isPhysics)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{root} namely {root.GetPathTo(root.GetTree().GetEditedSceneRoot())} is trying to call AddProcess in editor, which is not expected.");
            return null;
        }
    #endif
    
        var uNode = new DelegateDynamicRawNode()
        {
            ProcessAction = process,
            IsPhysics = isPhysics
        };
        
        uNode.BindParent(root);
        root.AddChild(uNode, false, Node.InternalMode.Front);

        return uNode;
    }

    public static Node AddProcess(this Node root, Action process, bool physics = false)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{root} namely {root.GetPathTo(root.GetTree().GetEditedSceneRoot())} is trying to call AddProcess in editor, which is not expected.");
            return null;
        }
    #endif
    
        DelegateRawNode uNode = physics ? new DelegatePhysicsRawNode() : new DelegateIdleRawNode();
        uNode.ProcessAction = process;
        
        uNode.BindParent(root);
        root.AddChild(uNode, false, Node.InternalMode.Front);
        
        return uNode;
    }
        
    public static Node AddPhysicsProcess(this Node root, Action process)
        => AddProcess(root, process, true);

    // Action

    public static UTimer ActionDelay(this Node node, double delay, Action action, bool physics = false)
    {
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{node} namely {node.GetPathTo(node.GetTree().GetEditedSceneRoot())} is trying to call ActionDelay in editor, which is not expected.");
            return null;
        }
    #endif
    
        UTimer timer = new()
        {
            Autostart = true,
            OneShot = true,
            WaitTime = delay,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.SignalTimeout += () =>
        {
            action?.Invoke();
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
    #if TOOLS
        if (Engine.IsEditorHint())
        {
            GD.PushWarning($"{node} namely {node.GetPathTo(node.GetTree().GetEditedSceneRoot())} is trying to call ActionRepeat in editor, which is not expected.");
            return null;
        }
    #endif
    
        UTimer timer = new()
        {
            Autostart = autostart,
            OneShot = false,
            WaitTime = interval,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.SignalTimeout += () =>
        {
            action?.Invoke();
        }; 
        
        timer.BindParent(node);
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    public static UTimer ActionPhysicsRepeat(this Node node, double interval, Action action, bool autostart = true)
        => ActionRepeat(node, interval, action, autostart, true);
}