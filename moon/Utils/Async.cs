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
    // useful for cancelling tasks

    public static async GDTask CancelSafely(this CancellationTokenSource cts)
    {
        await cts.CancelAsync();
        await GDTask.SwitchToMainThread();
    }
    
    public static async GDTask CancelPhysics(this CancellationTokenSource cts)
    {
        await cts.CancelAsync();
        await GDTask.SwitchToMainThread(PlayerLoopTiming.PhysicsProcess);
    }

    // equivalent to GDTask.Delay

    private static UTimer CreateWaitTimer(this Node node, double time, bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.BindParent(node, false);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChildSafely(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    public static async GDTask Await(this Node node, double time, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.CreateWaitTimer(time, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static GDTask Await(this Node node, double time, CancellationToken ct, bool physics = false)
        => node.AwaitProcess(time, () => {}, ct, physics);

    public static GDTask AwaitPhysics(this Node node, double time)
        => node.Await(time, true);
        
    public static GDTask AwaitPhysics(this Node node, double time, CancellationToken ct)
        => node.Await(time, ct, true);

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

    private static AsyncProcessTimer CreateWaitProcessTimer(this Node node, double time, Action<double> process,
        bool physics = false)
    {
        AsyncProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };

        timer.BindParent(node, false);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChildSafely(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    private static AsyncProcessTimer CreateWaitProcessTimer(this Node node, double time, Action<double> process,
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

        timer.BindParent(node, false);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChildSafely(timer, false, Node.InternalMode.Front);
        return timer;
    }

    public static async GDTask AwaitProcess(this Node node, double time, Action<double> process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.CreateWaitProcessTimer(time, process, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static async GDTask AwaitProcess(this Node node, double time, Action<double> process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.CreateWaitProcessTimer(time, process, ct, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout, ct);
        ct.ThrowIfCancellationRequested();
    }

    public static GDTask AwaitPhysicsProcess(this Node node, double time, Action<double> process)
        => node.AwaitProcess(time, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, double time, Action<double> process, CancellationToken ct)
        => node.AwaitProcess(time, process, ct, true);
        
    private static AsyncRawProcessTimer CreateWaitRawProcessTimer(this Node node, double time, Action process,
        bool physics = false)
    {
        AsyncRawProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };

        timer.BindParent(node, false);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChildSafely(timer, false, Node.InternalMode.Front);
        return timer;
    }
    
    private static AsyncRawProcessTimer CreateWaitRawProcessTimer(this Node node, double time, Action process,
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

        timer.BindParent(node, false);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChildSafely(timer, false, Node.InternalMode.Front);
        return timer;
    }

    public static async GDTask AwaitProcess(this Node node, double time, Action process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.CreateWaitRawProcessTimer(time, process, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }
    
    public static async GDTask AwaitProcess(this Node node, double time, Action process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = node.CreateWaitRawProcessTimer(time, process, ct, physics);
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout, ct);
        ct.ThrowIfCancellationRequested();
    }

    public static GDTask AwaitPhysicsProcess(this Node node, double time, Action process)
        => node.AwaitProcess(time, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, double time, Action process, CancellationToken ct)
        => node.AwaitProcess(time, process, ct, true);
        
    // equivalent to GDTask.WaitUntil

    public partial class AsyncDelegateNode : Node
    {
        public Func<double, bool> Action { get; set; }
        public bool IsPhysics { get; set; } = false;

        [Signal]
        public delegate void FinishedEventHandler();

        private void Act(double delta)
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

    private static AsyncDelegateNode CreateDelegateNode(this Node node, Func<double, bool> action, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        
        delegateNode.BindParent(node, false);
        node.AddChildSafely(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }
    
    private static AsyncDelegateNode CreateDelegateNode(this Node node, Func<double, bool> action,
        CancellationToken ct, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            IsPhysics = physics
        };
        delegateNode.Action = delta 
            => ct.IsCancellationRequested || action.Invoke(delta);
        
        delegateNode.BindParent(node, false);
        node.AddChildSafely(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }

    public static async GDTask AwaitUntil(this Node node, Func<double, bool> action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = node.CreateDelegateNode(action, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished);
    }
    
    public static async GDTask AwaitUntil(this Node node, Func<double, bool> action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = node.CreateDelegateNode(action, ct, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished, ct);
        ct.ThrowIfCancellationRequested();
    }

    public static GDTask AwaitUntilPhysics(this Node node, Func<double, bool> action)
        => node.AwaitUntil(action, true);
        
    public static GDTask AwaitUntilPhysics(this Node node, Func<double, bool> action, CancellationToken ct)
        => node.AwaitUntil(action, ct, true);
        
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

    private static AsyncDelegateRawNode CreateDelegateRawNode(this Node node, Func<bool> action, bool physics = false)
    {
        AsyncDelegateRawNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        
        delegateNode.BindParent(node, false);
        node.AddChildSafely(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }
    
    private static AsyncDelegateRawNode CreateDelegateRawNode(this Node node, Func<bool> action,
        CancellationToken ct, bool physics = false)
    {
        AsyncDelegateRawNode delegateNode = new()
        {
            IsPhysics = physics
        };
        delegateNode.Action = () 
            => ct.IsCancellationRequested || action.Invoke();
        
        delegateNode.BindParent(node, false);
        node.AddChildSafely(delegateNode, false, Node.InternalMode.Front); 
        return delegateNode;
    }

    public static async GDTask AwaitUntil(this Node node, Func<bool> action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = node.CreateDelegateRawNode(action, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateRawNode.SignalName.Finished);
    }
    
    public static async GDTask AwaitUntil(this Node node, Func<bool> action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var delegateNode = node.CreateDelegateRawNode(action, ct, physics);
        await GDTask.ToSignal(delegateNode, AsyncDelegateRawNode.SignalName.Finished, ct);
        ct.ThrowIfCancellationRequested();
    }

    public static GDTask AwaitUntilPhysics(this Node node, Func<bool> action)
        => node.AwaitUntil(action, true);
        
    public static GDTask AwaitUntilPhysics(this Node node, Func<bool> action, CancellationToken ct)
        => node.AwaitUntil(action, ct, true);
        
    // wait frames

    public static GDTask AwaitFrame(this Node node, int frames, bool physics = false)
    {
        var counter = 0;
        return node.AwaitUntil(() =>
        {
            counter++;
            return counter >= frames;
        }, physics);
    }
    
    public static GDTask AwaitFrame(this Node node, bool physics = false)
        => node.AwaitFrame(1, physics);
    
    public static GDTask AwaitFrame(this Node node, int frames, CancellationToken ct, bool physics = false)
    {
        var counter = 0;
        return node.AwaitUntil(() =>
        {
            counter++;
            return counter >= frames;
        }, ct, physics);
    }
    
    public static GDTask AwaitFrame(this Node node, CancellationToken ct, bool physics = false)
        => node.AwaitFrame(1, ct, physics);

    public static GDTask AwaitPhysicsFrame(this Node node, int frames)
        => node.AwaitFrame(frames, true);
        
    public static GDTask AwaitPhysicsFrame(this Node node, bool physics = false)
        => node.AwaitFrame(1, true);
        
    public static GDTask AwaitPhysicsFrame(this Node node, int frames, CancellationToken ct)
        => node.AwaitFrame(frames, ct, true);
        
    public static GDTask AwaitPhysicsFrame(this Node node, CancellationToken ct, bool physics = false)
        => node.AwaitFrame(1, ct, true);
        
    // wait a GDTask bind with internal node

    public static GDTask Await(this Node node, GDTask task, bool physics = false)
        => node.AwaitUntil(() => task.Status.IsCompleted(), physics);
        
    public static GDTask Await(this Node node, GDTask task, CancellationToken ct, bool physics = false)
        => node.AwaitUntil(() => task.Status.IsCompleted(), ct, physics);

    public static async GDTask<T> Await<T>(this Node node, GDTask<T> task, bool physics = false)
    {
        await node.AwaitUntil(() => task.Status.IsCompleted(), physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> Await<T>(this Node node, GDTask<T> task, CancellationToken ct, bool physics = false)
    {
        await node.AwaitUntil(() => task.Status.IsCompleted(), ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask AwaitPhysics(this Node node, GDTask task)
        => node.Await(task, true);
        
    public static GDTask AwaitPhysics(this Node node, GDTask task, CancellationToken ct)
        => node.Await(task, ct, true);
        
    public static GDTask<T> AwaitPhysics<T>(this Node node, GDTask<T> task)
        => node.Await(task, true);
        
    public static GDTask<T> AwaitPhysics<T>(this Node node, GDTask<T> task, CancellationToken ct)
        => node.Await(task, ct, true);

    public static GDTask AwaitProcess(this Node node, GDTask task, Action<double> process, bool physics = false)
        => node.AwaitUntil((delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        
    public static GDTask AwaitProcess(this Node node, GDTask task, Action<double> process, CancellationToken ct, bool physics = false)
        => node.AwaitUntil((delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, ct, physics);
        
    public static async GDTask<T> AwaitProcess<T>(this Node node, GDTask<T> task, Action<double> process, bool physics = false)
    {
        await node.AwaitUntil((delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> AwaitProcess<T>(this Node node, GDTask<T> task, Action<double> process, CancellationToken ct, bool physics = false)
    {
        await node.AwaitUntil((delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask AwaitPhysicsProcess(this Node node, GDTask task, Action<double> process)
        => node.AwaitProcess(task, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, GDTask task, Action<double> process, CancellationToken ct)
        => node.AwaitProcess(task, process, ct, true);
        
    public static GDTask<T> AwaitPhysicsProcess<T>(this Node node, GDTask<T> task, Action<double> process)
        => node.AwaitProcess(task, process, true);
        
    public static GDTask<T> AwaitPhysicsProcess<T>(this Node node, GDTask<T> task, Action<double> process, CancellationToken ct)
        => node.AwaitProcess(task, process, ct, true);
        
    public static GDTask AwaitProcess(this Node node, GDTask task, Action process, bool physics = false)
        => node.AwaitUntil(() =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, physics);
        
    public static GDTask AwaitProcess(this Node node, GDTask task, Action process, CancellationToken ct, bool physics = false)
        => node.AwaitUntil(() =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, ct, physics);
        
    public static async GDTask<T> AwaitProcess<T>(this Node node, GDTask<T> task, Action process, bool physics = false)
    {
        await node.AwaitUntil(() =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, physics);
        return task.GetAwaiter().GetResult();
    }
    
    public static async GDTask<T> AwaitProcess<T>(this Node node, GDTask<T> task, Action process, CancellationToken ct, bool physics = false)
    {
        await node.AwaitUntil(() =>
        {
            process.Invoke();
            return task.Status.IsCompleted();
        }, ct, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask AwaitPhysicsProcess(this Node node, GDTask task, Action process)
        => node.AwaitProcess(task, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, GDTask task, Action process, CancellationToken ct)
        => node.AwaitProcess(task, process, ct, true);
        
    public static GDTask<T> AwaitPhysicsProcess<T>(this Node node, GDTask<T> task, Action process)
        => node.AwaitProcess(task, process, true);
        
    public static GDTask<T> AwaitPhysicsProcess<T>(this Node node, GDTask<T> task, Action process, CancellationToken ct)
        => node.AwaitProcess(task, process, ct, true);
        
    // wait a signal bind with internal node

    public static GDTask<Variant[]> Await(this Node node, GodotObject obj, StringName signal, bool physics = false)
        => node.Await(GDTask.ToSignal(obj, signal), physics);
        
    public static GDTask<Variant[]> Await(this Node node, GodotObject obj, StringName signal, CancellationToken ct, bool physics = false)
        => node.Await(GDTask.ToSignal(obj, signal, ct), ct, physics);
        
    public static GDTask<Variant[]> AwaitPhysics(this Node node, GodotObject obj, StringName signal)
        => node.Await(obj, signal, true);
        
    public static GDTask<Variant[]> AwaitPhysics(this Node node, GodotObject obj, StringName signal, CancellationToken ct)
        => node.Await(obj, signal, ct, true);
    
    public static GDTask<Variant[]> AwaitProcess(this Node node, GodotObject obj, StringName signal, Action<double> process, bool physics = false)
        => node.AwaitProcess(GDTask.ToSignal(obj, signal), process, physics);
        
    public static GDTask<Variant[]> AwaitProcess(this Node node, GodotObject obj, StringName signal, Action<double> process, CancellationToken ct, bool physics = false)
        => node.AwaitProcess(GDTask.ToSignal(obj, signal, ct), process, ct, physics);
    
    public static GDTask<Variant[]> AwaitPhysicsProcess(this Node node, GodotObject obj, StringName signal, Action<double> process)
        => node.AwaitProcess(obj, signal, process, true);
        
    public static GDTask<Variant[]> AwaitPhysicsProcess(this Node node, GodotObject obj, StringName signal, Action<double> process, CancellationToken ct)
        => node.AwaitProcess(obj, signal, process, ct, true);
        
    public static GDTask<Variant[]> AwaitProcess(this Node node, GodotObject obj, StringName signal, Action process, bool physics = false)
        => node.AwaitProcess(GDTask.ToSignal(obj, signal), process, physics);
        
    public static GDTask<Variant[]> AwaitProcess(this Node node, GodotObject obj, StringName signal, Action process, CancellationToken ct, bool physics = false)
        => node.AwaitProcess(GDTask.ToSignal(obj, signal, ct), process, ct, physics);
    
    public static GDTask<Variant[]> AwaitPhysicsProcess(this Node node, GodotObject obj, StringName signal, Action process)
        => node.AwaitProcess(obj, signal, process, true);
        
    public static GDTask<Variant[]> AwaitPhysicsProcess(this Node node, GodotObject obj, StringName signal, Action process, CancellationToken ct)
        => node.AwaitProcess(obj, signal, process, ct, true);
        
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

    private static AsyncTweenNode CreateTweenNode(this Node node, Tween tween, Action<double> action, bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            Action = action,
            IsPhysics = physics
        };
        tween.Pause();
        tween.BindNode(tweenNode);

        tweenNode.BindParent(node, false);
        node.AddChildSafely(tweenNode, false, Node.InternalMode.Front);
        return tweenNode;
    }
    
    private static AsyncTweenNode CreateTweenNode(this Node node, Tween tween, Action<double> action,
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

        tweenNode.BindParent(node, false);
        node.AddChildSafely(tweenNode, false, Node.InternalMode.Front);
        return tweenNode;
    }

    public static async GDTask AwaitProcess(this Node node, Tween tween, Action<double> process, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var tweenNode = node.CreateTweenNode(tween, process, physics);
        await GDTask.ToSignal(tweenNode, AsyncTweenNode.SignalName.Finished);
    }
    
    public static async GDTask AwaitProcess(this Node node, Tween tween, Action<double> process, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var tweenNode = node.CreateTweenNode(tween, process, ct, physics);
        await GDTask.ToSignal(tweenNode, AsyncTweenNode.SignalName.Finished, ct);
        ct.ThrowIfCancellationRequested();
    }

    public static GDTask AwaitPhysicsProcess(this Node node, Tween tween, Action<double> process)
        => node.AwaitProcess(tween, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, Tween tween, Action<double> process, CancellationToken ct)
        => node.AwaitProcess(tween, process, ct, true);
        
    public static GDTask AwaitProcess(this Node node, Tween tween, Action process, bool physics = false)
        => node.AwaitProcess(tween, delta => process.Invoke(), physics);
    
    public static GDTask AwaitProcess(this Node node, Tween tween, Action process, CancellationToken ct, bool physics = false)
        => node.AwaitProcess(tween, delta => process.Invoke(), ct, physics);

    public static GDTask AwaitPhysicsProcess(this Node node, Tween tween, Action process)
        => node.AwaitProcess(tween, process, true);
        
    public static GDTask AwaitPhysicsProcess(this Node node, Tween tween, Action process, CancellationToken ct)
        => node.AwaitProcess(tween, process, ct, true);

    public static GDTask Await(this Node node, Tween tween, bool physics = false)
        => node.AwaitProcess(tween, () => { }, physics);
        
    public static GDTask Await(this Node node, Tween tween, CancellationToken ct, bool physics = false)
        => node.AwaitProcess(tween, () => { }, ct, physics);

    public static GDTask AwaitPhysics(this Node node, Tween tween)
        => node.Await(tween, true);
        
    public static GDTask AwaitPhysics(this Node node, Tween tween, CancellationToken ct)
        => node.Await(tween, ct, true);
        
    // repeat timer, this will be more precise than multiple Wait calls

    public static async GDTask AwaitRepeat(this Node node, double time, int count, Action<int> action, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = 0d;
        var counter = 0;
        await node.AwaitUntil(delta =>
        {
            timer += delta;
            if (timer >= time)
            {
                timer -= time;
                action?.Invoke(counter);
                counter++;
            }
            
            return count > 0 && counter >= count;
        }, physics);
    }
    
    public static async GDTask AwaitRepeat(this Node node, double time, int count, Action<int> action, CancellationToken ct, bool physics = false)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        var timer = 0d;
        var counter = 0;
        await node.AwaitUntil(delta =>
        {
            timer += delta;
            if (timer >= time)
            {
                timer -= time;
                action?.Invoke(counter);
                counter++;
            }
            
            return count > 0 && counter >= count;
        }, ct, physics);
    }
    
    public static GDTask AwaitRepeat(this Node node, double time, int count, Action action, bool physics = false)
        => node.AwaitRepeat(time, count, i => action?.Invoke(), physics);
        
    public static GDTask AwaitRepeat(this Node node, double time, int count, Action action, CancellationToken ct, bool physics = false)
        => node.AwaitRepeat(time, count, i => action?.Invoke(), ct, physics);
    
    public static GDTask AwaitRepeatPhysics(this Node node, double time, int count, Action<int> action)
        => node.AwaitRepeat(time, count, action, true);
        
    public static GDTask AwaitRepeatPhysics(this Node node, double time, int count, Action<int> action, CancellationToken ct)
        => node.AwaitRepeat(time, count, action, ct, true);
    
    public static GDTask AwaitRepeatPhysics(this Node node, double time, int count, Action action)
        => node.AwaitRepeat(time, count, action, true);
        
    public static GDTask AwaitRepeatPhysics(this Node node, double time, int count, Action action, CancellationToken ct)
        => node.AwaitRepeat(time, count, action, ct, true);
}