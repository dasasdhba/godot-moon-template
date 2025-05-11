using System;
using System.Threading;
using Godot;
using GodotTask;

namespace Utils;

/// <summary>
/// Async tools based on internal nodes.
/// This can ensure that async functions stop when the node is freed.
/// Also, this makes it easier to pause a node with async tasks.
/// Though creating node costs a lot compared with native GDTask API.
/// </summary>
public static partial class Async
{
    // equivalent to GDTask.Delay

    private static UTimer CreateWaitTimer(Node node, double time, bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.BindParent(node);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    public static async GDTask Wait(Node node, double time, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = CreateWaitTimer(node, time, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static GDTask Wait(Node node, double time, CancellationToken ct, bool physics = false)
        => WaitProcess(node, time, () => {}, ct, physics);

    public static GDTask WaitPhysics(Node node, double time)
        => Wait(node, time, true);
        
    public static GDTask WaitPhysics(Node node, double time, CancellationToken ct)
        => Wait(node, time, ct, true);

    public partial class AsyncProcessTimer : UTimer
    {
        public Action<double> Process { get; set; }

        public override void _EnterTree()
        {
            this.AddProcess(Process, ProcessCallback == UTimerProcessCallback.Physics);
        }
    }
    
    public partial class AsyncRawProcessTimer : UTimer
    {
        public Action Process { get; set; }

        public override void _EnterTree()
        {
            this.AddProcess(Process, ProcessCallback == UTimerProcessCallback.Physics);
        }
    }

    private static AsyncProcessTimer CreateWaitProcessTimer(Node node, double time, Action<double> process,
        bool physics = false)
    {
        AsyncProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };

        timer.BindParent(node);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    private static AsyncProcessTimer CreateWaitProcessTimer(Node node, double time, Action<double> process,
        CancellationToken ct, bool physics = false)
    {
        AsyncProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
        };
        timer.Process = delta =>
        {
            if (ct.IsCancellationRequested)
            {
                timer.TimeLeft = 0d;
                return;
            }
            
            process?.Invoke(delta);
        };

        timer.BindParent(node);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }

    public static async GDTask WaitProcess(Node node, double time, Action<double> process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = CreateWaitProcessTimer(node, time, process, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static async GDTask WaitProcess(Node node, double time, Action<double> process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = CreateWaitProcessTimer(node, time, process, ct, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout, ct);
    }

    public static GDTask WaitPhysicsProcess(Node node, double time, Action<double> process)
        => WaitProcess(node, time, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, double time, Action<double> process, CancellationToken ct)
        => WaitProcess(node, time, process, ct, true);
        
    private static AsyncRawProcessTimer CreateWaitRawProcessTimer(Node node, double time, Action process,
        bool physics = false)
    {
        AsyncRawProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };

        timer.BindParent(node);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    private static AsyncRawProcessTimer CreateWaitRawProcessTimer(Node node, double time, Action process,
        CancellationToken ct, bool physics = false)
    {
        AsyncRawProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
        };
        timer.Process = () =>
        {
            if (ct.IsCancellationRequested)
            {
                timer.TimeLeft = 0d;
                return;
            }
            
            process?.Invoke();
        };

        timer.BindParent(node);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        return timer;
    }

    public static async GDTask WaitProcess(Node node, double time, Action process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = CreateWaitRawProcessTimer(node, time, process, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static async GDTask WaitProcess(Node node, double time, Action process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = CreateWaitRawProcessTimer(node, time, process, ct, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout, ct);
    }

    public static GDTask WaitPhysicsProcess(Node node, double time, Action process)
        => WaitProcess(node, time, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, double time, Action process, CancellationToken ct)
        => WaitProcess(node, time, process, ct, true);
        
    // equivalent to GDTask.WaitUntil

    public partial class AsyncDelegateNode : Node
    {
        public Func<double, bool> Action { get; set; }
        public bool IsPhysics { get; set; } = false;

        [Signal]
        public delegate void FinishedEventHandler();

        public void Act(double delta)
        {
            if (Action.Invoke(delta))
            {
                EmitSignal(SignalName.Finished);
                QueueFree();
            }
        }

        public override void _EnterTree()
        {
            this.AddProcess(Act, IsPhysics);
        }
    }

    private static AsyncDelegateNode CreateDelegateNode(Node node, Func<double, bool> action, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        
        delegateNode.BindParent(node);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }
    
    private static AsyncDelegateNode CreateDelegateNode(Node node, Func<double, bool> action,
        CancellationToken ct, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            IsPhysics = physics
        };
        delegateNode.Action = delta 
            => ct.IsCancellationRequested || action.Invoke(delta);
        
        delegateNode.BindParent(node);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }

    public static async GDTask WaitUntil(Node node, Func<double, bool> action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = CreateDelegateNode(node, action, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished);
    }
    
    public static async GDTask WaitUntil(Node node, Func<double, bool> action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = CreateDelegateNode(node, action, ct, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished, ct);
    }

    public static GDTask WaitUntilPhysics(Node node, Func<double, bool> action)
        => WaitUntil(node, action, true);
        
    public static GDTask WaitUntilPhysics(Node node, Func<double, bool> action, CancellationToken ct)
        => WaitUntil(node, action, ct, true);
        
    public partial class AsyncDelegateRawNode : Node
    {
        public Func<bool> Action { get; set; }
        public bool IsPhysics { get; set; } = false;

        [Signal]
        public delegate void FinishedEventHandler();

        public void Act()
        {
            if (Action.Invoke())
            {
                EmitSignal(SignalName.Finished);
                QueueFree();
            }
        }

        public override void _EnterTree()
        {
            this.AddProcess(Act, IsPhysics);
        }
    }

    private static AsyncDelegateRawNode CreateDelegateRawNode(Node node, Func<bool> action, bool physics = false)
    {
        AsyncDelegateRawNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        
        delegateNode.BindParent(node);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }
    
    private static AsyncDelegateRawNode CreateDelegateRawNode(Node node, Func<bool> action,
        CancellationToken ct, bool physics = false)
    {
        AsyncDelegateRawNode delegateNode = new()
        {
            IsPhysics = physics
        };
        delegateNode.Action = () 
            => ct.IsCancellationRequested || action.Invoke();
        
        delegateNode.BindParent(node);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }

    public static async GDTask WaitUntil(Node node, Func<bool> action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = CreateDelegateRawNode(node, action, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateRawNode.SignalName.Finished);
    }
    
    public static async GDTask WaitUntil(Node node, Func<bool> action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = CreateDelegateRawNode(node, action, ct, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateRawNode.SignalName.Finished, ct);
    }

    public static GDTask WaitUntilPhysics(Node node, Func<bool> action)
        => WaitUntil(node, action, true);
        
    public static GDTask WaitUntilPhysics(Node node, Func<bool> action, CancellationToken ct)
        => WaitUntil(node, action, ct, true);
        
    // wait frames

    public static GDTask WaitFrame(Node node, int frames, bool physics = false)
    {
        var counter = 0;
        return WaitUntil(node, () =>
        {
            counter++;
            return counter >= frames;
        }, physics);
    }
    
    public static GDTask WaitFrame(Node node, bool physics = false)
        => WaitFrame(node, 1, physics);
    
    public static GDTask WaitFrame(Node node, int frames, CancellationToken ct, bool physics = false)
    {
        var counter = 0;
        return WaitUntil(node, () =>
        {
            counter++;
            return counter >= frames;
        }, ct, physics);
    }
    
    public static GDTask WaitFrame(Node node, CancellationToken ct, bool physics = false)
        => WaitFrame(node, 1, ct, physics);

    public static GDTask WaitPhysicsFrame(Node node, int frames)
        => WaitFrame(node, frames, true);
        
    public static GDTask WaitPhysicsFrame(Node node, bool physics = false)
        => WaitFrame(node, 1, true);
        
    public static GDTask WaitPhysicsFrame(Node node, int frames, CancellationToken ct)
        => WaitFrame(node, frames, ct, true);
        
    public static GDTask WaitPhysicsFrame(Node node, CancellationToken ct, bool physics = false)
        => WaitFrame(node, 1, ct, true);
        
    // wait a GDTask bind with internal node

    public static GDTask Wait(Node node, GDTask task, bool physics = false)
        => WaitUntil(node, () => task.Status.IsCompleted(), physics);
        
    public static GDTask Wait(Node node, GDTask task, CancellationToken ct, bool physics = false)
        => WaitUntil(node, () => task.Status.IsCompleted(), ct, physics);

    public static async GDTask<T> Wait<T>(Node node, GDTask<T> task, bool physics = false)
    {
        await WaitUntil(node, () => task.Status.IsCompleted(), physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> Wait<T>(Node node, GDTask<T> task, CancellationToken ct, bool physics = false)
    {
        await WaitUntil(node, () => task.Status.IsCompleted(), ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask WaitPhysics(Node node, GDTask task)
        => Wait(node, task, true);
        
    public static GDTask WaitPhysics(Node node, GDTask task, CancellationToken ct)
        => Wait(node, task, ct, true);
        
    public static GDTask<T> WaitPhysics<T>(Node node, GDTask<T> task)
        => Wait(node, task, true);
        
    public static GDTask<T> WaitPhysics<T>(Node node, GDTask<T> task, CancellationToken ct)
        => Wait(node, task, ct, true);

    public static GDTask WaitProcess(Node node, GDTask task, Action<double> process, bool physics = false)
        => WaitUntil(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        
    public static GDTask WaitProcess(Node node, GDTask task, Action<double> process, CancellationToken ct, bool physics = false)
        => WaitUntil(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, ct, physics);
        
    public static async GDTask<T> WaitProcess<T>(Node node, GDTask<T> task, Action<double> process, bool physics = false)
    {
        await WaitUntil(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> WaitProcess<T>(Node node, GDTask<T> task, Action<double> process, CancellationToken ct, bool physics = false)
    {
        await WaitUntil(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask WaitPhysicsProcess(Node node, GDTask task, Action<double> process)
        => WaitProcess(node, task, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, GDTask task, Action<double> process, CancellationToken ct)
        => WaitProcess(node, task, process, ct, true);
        
    public static GDTask<T> WaitPhysicsProcess<T>(Node node, GDTask<T> task, Action<double> process)
        => WaitProcess(node, task, process, true);
        
    public static GDTask<T> WaitPhysicsProcess<T>(Node node, GDTask<T> task, Action<double> process, CancellationToken ct)
        => WaitProcess(node, task, process, ct, true);
        
    public static GDTask WaitProcess(Node node, GDTask task, Action process, bool physics = false)
        => WaitUntil(node, () =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, physics);
        
    public static GDTask WaitProcess(Node node, GDTask task, Action process, CancellationToken ct, bool physics = false)
        => WaitUntil(node, () =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, ct, physics);
        
    public static async GDTask<T> WaitProcess<T>(Node node, GDTask<T> task, Action process, bool physics = false)
    {
        await WaitUntil(node, () =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> WaitProcess<T>(Node node, GDTask<T> task, Action process, CancellationToken ct, bool physics = false)
    {
        await WaitUntil(node, () =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask WaitPhysicsProcess(Node node, GDTask task, Action process)
        => WaitProcess(node, task, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, GDTask task, Action process, CancellationToken ct)
        => WaitProcess(node, task, process, ct, true);
        
    public static GDTask<T> WaitPhysicsProcess<T>(Node node, GDTask<T> task, Action process)
        => WaitProcess(node, task, process, true);
        
    public static GDTask<T> WaitPhysicsProcess<T>(Node node, GDTask<T> task, Action process, CancellationToken ct)
        => WaitProcess(node, task, process, ct, true);
        
    // wait a signal bind with internal node

    public static GDTask<Variant[]> Wait(Node node, GodotObject obj, StringName signal, bool physics = false)
        => Wait(node, GDTask.ToSignal(obj, signal), physics);
        
    public static GDTask<Variant[]> Wait(Node node, GodotObject obj, StringName signal, CancellationToken ct, bool physics = false)
        => Wait(node, GDTask.ToSignal(obj, signal, ct), ct, physics);
        
    public static GDTask<Variant[]> WaitPhysics(Node node, GodotObject obj, StringName signal)
        => Wait(node, obj, signal, true);
        
    public static GDTask<Variant[]> WaitPhysics(Node node, GodotObject obj, StringName signal, CancellationToken ct)
        => Wait(node, obj, signal, ct, true);
    
    public static GDTask<Variant[]> WaitProcess(Node node, GodotObject obj, StringName signal, Action<double> process, bool physics = false)
        => WaitProcess(node, GDTask.ToSignal(obj, signal), process, physics);
        
    public static GDTask<Variant[]> WaitProcess(Node node, GodotObject obj, StringName signal, Action<double> process, CancellationToken ct, bool physics = false)
        => WaitProcess(node, GDTask.ToSignal(obj, signal, ct), process, ct, physics);
    
    public static GDTask<Variant[]> WaitPhysicsProcess(Node node, GodotObject obj, StringName signal, Action<double> process)
        => WaitProcess(node, obj, signal, process, true);
        
    public static GDTask<Variant[]> WaitPhysicsProcess(Node node, GodotObject obj, StringName signal, Action<double> process, CancellationToken ct)
        => WaitProcess(node, obj, signal, process, ct, true);
        
    public static GDTask<Variant[]> WaitProcess(Node node, GodotObject obj, StringName signal, Action process, bool physics = false)
        => WaitProcess(node, GDTask.ToSignal(obj, signal), process, physics);
        
    public static GDTask<Variant[]> WaitProcess(Node node, GodotObject obj, StringName signal, Action process, CancellationToken ct, bool physics = false)
        => WaitProcess(node, GDTask.ToSignal(obj, signal, ct), process, ct, physics);
    
    public static GDTask<Variant[]> WaitPhysicsProcess(Node node, GodotObject obj, StringName signal, Action process)
        => WaitProcess(node, obj, signal, process, true);
        
    public static GDTask<Variant[]> WaitPhysicsProcess(Node node, GodotObject obj, StringName signal, Action process, CancellationToken ct)
        => WaitProcess(node, obj, signal, process, ct, true);
        
    // wait a tween bind with internal node

    public partial class AsyncTweenNode : Node
    {
        public Tween Tween { get ;set; }
        public Action<double> Action { get; set; }
        public bool IsPhysics { get; set; } = false;

        [Signal]
        public delegate void FinishedEventHandler();

        private void Act(double delta)
        {
            Action?.Invoke(delta);
            if (!IsInstanceValid(Tween))
            {
                EmitSignal(SignalName.Finished);
                QueueFree();
            }
            if (!Tween.CustomStep(delta))
            {
                EmitSignal(SignalName.Finished);
                Tween.Kill();
                QueueFree();
            }
        }

        public override void _EnterTree()
        {
            this.AddProcess(Act, IsPhysics);
        }
    }

    private static AsyncTweenNode CreateTweenNode(Node node, Tween tween, Action<double> action, bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            Action = action,
            IsPhysics = physics
        };
        tween.Pause();
        tween.BindNode(tweenNode);

        tweenNode.BindParent(node);
        node.AddChild(tweenNode, false, Node.InternalMode.Front);
        return tweenNode;
    }
    
    private static AsyncTweenNode CreateTweenNode(Node node, Tween tween, Action<double> action,
        CancellationToken ct, bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            IsPhysics = physics
        };
        tweenNode.Action = delta =>
        {
            if (ct.IsCancellationRequested)
            {
                tween.Kill();
                return;
            }
            
            action?.Invoke(delta);
        };
        tween.Pause();
        tween.BindNode(tweenNode);

        tweenNode.BindParent(node);
        node.AddChild(tweenNode, false, Node.InternalMode.Front);
        return tweenNode;
    }

    public static async GDTask WaitProcess(Node node, Tween tween, Action<double> process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var tweenNode = CreateTweenNode(node, tween, process, physics);
        await GDTask.ToSignal(tweenNode, AsyncTweenNode.SignalName.Finished);
    }
    
    public static async GDTask WaitProcess(Node node, Tween tween, Action<double> process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var tweenNode = CreateTweenNode(node, tween, process, ct, physics);
        await GDTask.ToSignal(tweenNode, AsyncTweenNode.SignalName.Finished, ct);
    }

    public static GDTask WaitPhysicsProcess(Node node, Tween tween, Action<double> process)
        => WaitProcess(node, tween, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, Tween tween, Action<double> process, CancellationToken ct)
        => WaitProcess(node, tween, process, ct, true);
        
    public static GDTask WaitProcess(Node node, Tween tween, Action process, bool physics = false)
        => WaitProcess(node, tween, delta => process.Invoke(), physics);
    
    public static GDTask WaitProcess(Node node, Tween tween, Action process, CancellationToken ct, bool physics = false)
        => WaitProcess(node, tween, delta => process.Invoke(), ct, physics);

    public static GDTask WaitPhysicsProcess(Node node, Tween tween, Action process)
        => WaitProcess(node, tween, process, true);
        
    public static GDTask WaitPhysicsProcess(Node node, Tween tween, Action process, CancellationToken ct)
        => WaitProcess(node, tween, process, ct, true);

    public static GDTask Wait(Node node, Tween tween, bool physics = false)
        => WaitProcess(node, tween, () => { }, physics);
        
    public static GDTask Wait(Node node, Tween tween, CancellationToken ct, bool physics = false)
        => WaitProcess(node, tween, () => { }, ct, physics);

    public static GDTask WaitPhysics(Node node, Tween tween)
        => Wait(node, tween, true);
        
    public static GDTask WaitPhysics(Node node, Tween tween, CancellationToken ct)
        => Wait(node, tween, ct, true);
        
    // repeat timer, this will be more precise than multiple Wait calls

    public static async GDTask Repeat(Node node, double time, int count, Action action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.ActionRepeat(time, action, true, physics);
        for (int i = 0; i < count; i++)
        {
            await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
        }
        timer.QueueFree();
    }
    
    public static async GDTask Repeat(Node node, double time, int count, Action action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.ActionRepeat(time, action, true, physics);

        try
        {
            for (int i = 0; i < count; i++)
            {
                await GDTask.ToSignal(timer, UTimer.SignalName.Timeout, ct);
            }
        }
        finally
        {
            timer.QueueFree();
        }
    }
    
    public static GDTask RepeatPhysics(Node node, double time, int count, Action action)
        => Repeat(node, time, count, action, true);
        
    public static GDTask RepeatPhysics(Node node, double time, int count, Action action, CancellationToken ct)
        => Repeat(node, time, count, action, ct, true);
}