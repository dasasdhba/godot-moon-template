using System;
using Godot;
using System.Threading.Tasks;

namespace Utils;

// async tools based on internal nodes
// though there already exists some good async tools e.g. GDTask
// but we prefer the pattern which can benefit from NodeU's custom delta rate

public static partial class Async
{
    public static async Task Wait(Node node, double time, bool physics = false)
    {
        UTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle
        };

        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        await timer.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static Task WaitPhysics(Node node, double time)
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

    public static async Task WaitProcess(Node node, double time, Action<double> process, bool physics = false)
    {
        AsyncProcessTimer timer = new()
        {
            Autostart = true,
            WaitTime = time,
            ProcessCallback = physics ? UTimer.UTimerProcessCallback.Physics : UTimer.UTimerProcessCallback.Idle,
            Process = process
        };

        timer.SignalTimeout += timer.QueueFree;
        node.AddChild(timer, false, Node.InternalMode.Front);
        await timer.ToSignal(timer, UTimer.SignalName.Timeout);
    }

    public static Task WaitPhysicsProcess(Node node, double time, Action<double> process)
        => WaitProcess(node, time, process, true);

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

    public static Task Delegate(Node node, Func<bool> action, bool physics = false)
    => DelegateProcess(node, (delta) => action.Invoke(), physics);

    public static Task DelegatePhysics(Node node, Func<bool> action)
        => Delegate(node, action, true);

    public static async Task DelegateProcess(Node node, Func<double, bool> action, bool physics = false)
    {
        AsyncDelegateNode delegateNode = new()
        {
            Action = action,
            IsPhysics = physics
        };

        node.AddChild(delegateNode, false, Node.InternalMode.Front); 
        await delegateNode.ToSignal(delegateNode, AsyncDelegateNode.SignalName.Finished);
    }

    public static Task DelegatePhysicsProcess(Node node, Func<double, bool> action)
        => DelegateProcess(node, action, true);

    public static Task Wait(Node node, Task task, bool physics = false)
        => Delegate(node, () => task.IsCompleted, physics);

    public static Task WaitPhysics(Node node, Task task)
        => Wait(node, task, true);

    public static Task WaitProcess(Node node, Task task, Action<double> process, bool physics = false)
        => DelegateProcess(node, (delta) =>
        {
            process.Invoke(delta);
            return task.IsCompleted;
        }, physics);

    public static Task WaitPhysicsProcess(Node node, Task task, Action<double> process)
        => WaitProcess(node, task, process, true);

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

    public static async Task WaitProcess(Node node, Tween tween, Action<double> process, bool physics = false)
    {
        AsyncTweenNode tweenNode = new()
        {
            Tween = tween,
            Action = process,
            IsPhysics = physics
        };
        tween.Pause();
        tween.BindNode(tweenNode);

        node.AddChild(tweenNode, false, Node.InternalMode.Front);
        await tweenNode.ToSignal(tweenNode, AsyncDelegateNode.SignalName.Finished);
    }

    public static Task WaitPhysicsProcess(Node node, Tween tween, Action<double> process)
        => WaitProcess(node, tween, process, true);

    public static Task Wait(Node node, Tween tween, bool physics = false)
        => WaitProcess(node, tween, null, physics);

    public static Task WaitPhysics(Node node, Tween tween)
        => Wait(node, tween, true);
}