using System;
using Godot;
using System.Threading.Tasks;

namespace Utils;

// async tools based on internal nodes
// though there already exists some good async tools e.g. GDTask
// but we prefer the pattern which can benefit from NodeU's custom delta rate

public static partial class Async
{
    public partial class TaskContainer : RefCounted
    {
        public Task Task { get; set; }
    }

    public partial class TaskContainer<T> : RefCounted
    {
        public Task<T> Task { get; set; }
    }
    
    public static Task Forget(this Task task, Node node, string tag)
    {
        if (node.HasMeta("__Async__Task" + tag))
        {
            var container = (TaskContainer)node.GetMeta("__Async__Task" + tag);
            container.Task?.Dispose();
        }
        node.SetMeta("__Async__Task" + tag, new TaskContainer { Task = task });
        return task;
    }
    
    public static Task<T> Forget<T>(this Task<T> task, Node node, string tag)
    {
        if (node.HasMeta("__Async__Task" + tag))
        {
            var container = (TaskContainer<T>)node.GetMeta("__Async__Task" + tag);
            container.Task?.Dispose();
        }
        node.SetMeta("__Async__Task" + tag, new TaskContainer<T> { Task = task });
        return task;
    }

    /// <summary>
    /// clear internal async nodes with tag.
    /// </summary>
    public static void Clear(Node node, string tag)
    {
        foreach (var child in node.GetChildren(true))
        {
            if (child.HasMeta("__Async" + tag))
                child.QueueFree();
        }
    }

    public static async Task Wait(Node node, double time, string tag = "", bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };
        if (tag != "") timer.SetMeta("__Async" + tag, true);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        await timer.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static Task WaitPhysics(Node node, double time, string tag = "")
        => Wait(node, time, tag, true);

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

    public static async Task WaitProcess(Node node, double time, Action<double> process, string tag = "", bool physics = false)
    {
        AsyncProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };
        if (tag != "") timer.SetMeta("__Async" + tag, true);
        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        await timer.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static Task WaitPhysicsProcess(Node node, double time, Action<double> process, string tag = "")
        => WaitProcess(node, time, process, tag, true);

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

    public static Task Delegate(Node node, Func<bool> action, string tag = "", bool physics = false)
    => DelegateProcess(node, (delta) => action.Invoke(), tag, physics);

    public static Task DelegatePhysics(Node node, Func<bool> action, string tag = "")
        => Delegate(node, action, tag, true);

    public static async Task DelegateProcess(Node node, Func<double, bool> action, string tag = "", bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };
        if (tag != "") delegateNode.SetMeta("__Async" + tag, true);
        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        await delegateNode.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished);
    }

    public static Task DelegatePhysicsProcess(Node node, Func<double, bool> action, string tag = "")
        => DelegateProcess(node, action, tag, true);

    public static Task Wait(Node node, Task task, string tag = "", bool physics = false)
        => Delegate(node, () => task.IsCompleted, tag, physics);

    public static Task WaitPhysics(Node node, Task task, string tag = "")
        => Wait(node, task, tag, true);

    public static Task WaitProcess(Node node, Task task, Action<double> process, string tag = "", bool physics = false)
        => DelegateProcess(node, (delta) =>
        {
            process.Invoke(delta);
            return task.IsCompleted;
        }, tag, physics);

    public static Task WaitPhysicsProcess(Node node, Task task, Action<double> process, string tag = "")
        => WaitProcess(node, task, process, tag, true);

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

    public static async Task WaitProcess(Node node, Tween tween, Action<double> process, string tag = "", bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            Action = process,
            IsPhysics = physics
        };
        tween.Pause();
        tween.BindNode(tweenNode);
        if (tag != "") tweenNode.SetMeta("__Async" + tag, true);
        node.AddChild(tweenNode, false, Node.InternalMode.Front);
        await tweenNode.ToSignal(tweenNode, AsyncDelegateNode.SignalName.Finished);
    }

    public static Task WaitPhysicsProcess(Node node, Tween tween, Action<double> process, string tag = "")
        => WaitProcess(node, tween, process, tag, true);

    public static Task Wait(Node node, Tween tween, string tag = "", bool physics = false)
        => WaitProcess(node, tween, null, tag, physics);

    public static Task WaitPhysics(Node node, Tween tween, string tag = "")
        => Wait(node, tween, tag, true);
}