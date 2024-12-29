using System;
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
    
    public static async GDTask Wait(Node node, double time, bool physics = false)
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
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static GDTask WaitPhysics(Node node, double time)
        => Wait(node, time, true);

    public partial class AsyncProcessTimer : UTimer
    {
        public Action<double> Process { get; set; }

        public override void _EnterTree()
        {
            this.AddProcess((delta) =>
            { 
                Process.Invoke(delta);
            }, ProcessCallback == UTimerProcessCallback.Physics);
        }
    }

    public static async GDTask WaitProcess(Node node, double time, Action<double> process, bool physics = false)
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
        await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static GDTask WaitPhysicsProcess(Node node, double time, Action<double> process)
        => WaitProcess(node, time, process, true);
        
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

    public static GDTask Delegate(Node node, Func<bool> action, bool physics = false)
        => DelegateProcess(node, (delta) => action.Invoke(), physics);

    public static GDTask DelegatePhysics(Node node, Func<bool> action)
        => Delegate(node, action, true);

    public static async GDTask DelegateProcess(Node node, Func<double, bool> action, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        
        delegateNode.BindParent(node);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        await GDTask.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished);
    }

    public static GDTask DelegatePhysicsProcess(Node node, Func<double, bool> action)
        => DelegateProcess(node, action, true);
        
    // wait a GDTask bind with internal node

    public static GDTask Wait(Node node, GDTask task, bool physics = false)
        => Delegate(node, () => task.Status.IsCompleted(), physics);

    public static async GDTask<T> Wait<T>(Node node, GDTask<T> task, bool physics = false)
    {
        await Delegate(node, () => task.Status.IsCompleted(), physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask WaitPhysics(Node node, GDTask task)
        => Wait(node, task, true);
        
    public static GDTask<T> WaitPhysics<T>(Node node, GDTask<T> task)
        => Wait(node, task, true);

    public static GDTask WaitProcess(Node node, GDTask task, Action<double> process, bool physics = false)
        => DelegateProcess(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        
    public static async GDTask<T> WaitProcess<T>(Node node, GDTask<T> task, Action<double> process, bool physics = false)
    {
        await DelegateProcess(node, (delta) =>
        {
            process.Invoke(delta);
            return task.Status.IsCompleted();
        }, physics);
        return task.GetAwaiter().GetResult();
    }

    public static GDTask WaitPhysicsProcess(Node node, GDTask task, Action<double> process)
        => WaitProcess(node, task, process, true);
        
    public static GDTask<T> WaitPhysicsProcess<T>(Node node, GDTask<T> task, Action<double> process)
        => WaitProcess(node, task, process, true);
        
    // wait a signal bind with internal node

    public static GDTask<Variant[]> Wait(Node node, GodotObject obj, StringName signal, bool physics = false)
        => Wait(node, GDTask.ToSignal(obj, signal), physics);
        
    public static GDTask<Variant[]> WaitPhysics(Node node, GodotObject obj, StringName signal)
        => Wait(node, obj, signal, true);
    
    public static GDTask<Variant[]> WaitProcess(Node node, GodotObject obj, StringName signal, Action<double> process, bool physics = false)
        => WaitProcess(node, GDTask.ToSignal(obj, signal), process, physics);
    
    public static GDTask<Variant[]> WaitPhysicsProcess(Node node, GodotObject obj, StringName signal, Action<double> process)
        => WaitProcess(node, obj, signal, process, true);
        
    // wait a tween bind with internal node

    public partial class AsyncTweenNode : Node
    {
        public Tween Tween { get ;set; }
        public Action<double> Action { get; set; }
        public bool IsPhysics { get; set; } = false;

        [Signal]
        public delegate void FinishedEventHandler();

        public void Act(double delta)
        {
            Action?.Invoke(delta);
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

    public static async GDTask WaitProcess(Node node, Tween tween, Action<double> process, bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            Action = process,
            IsPhysics = physics
        };
        tween.Pause();
        tween.BindNode(tweenNode);

        tweenNode.BindParent(node);
        node.AddChild(tweenNode, false, Node.InternalMode.Front);
        await tweenNode.ToSignal(tweenNode, AsyncTweenNode.SignalName.Finished);
    }

    public static GDTask WaitPhysicsProcess(Node node, Tween tween, Action<double> process)
        => WaitProcess(node, tween, process, true);

    public static GDTask Wait(Node node, Tween tween, bool physics = false)
        => WaitProcess(node, tween, null, physics);

    public static GDTask WaitPhysics(Node node, Tween tween)
        => Wait(node, tween, true);
        
    // repeat timer, this will be more precise than multiple Wait calls

    public static async GDTask Repeat(Node node, double time, int count, Action action, bool physics = false)
    {
        var timer = node.ActionRepeat(time, action, physics);
        for (int i = 0; i < count; i++)
        {
            await GDTask.ToSignal(timer, UTimer.SignalName.Timeout);
        }
        timer.QueueFree();
    }
    
    public static GDTask RepeatPhysics(Node node, double time, int count, Action action)
        => Repeat(node, time, count, action, true);
}