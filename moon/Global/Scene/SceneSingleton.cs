using Component;
using Godot;
using Godot.Collections;
using GodotTask;
using GodotTask.Triggers;

namespace Global;

public partial class SceneSingleton : CanvasLayer
{
    [ExportCategory("SceneSingleton")]
    [Export]
    public Dictionary<string, PackedScene> TransLib { get ;set; } = new();
    
    public System.Collections.Generic.Dictionary<string, AsyncLoader<TransNode>> TransLoader { get ;set; } = new();

    [Signal]
    public delegate void TransInEndedEventHandler();

    protected TransNode TransNode { get ;set; }
    protected Tween TransTween { get ;set; }

    public override void _EnterTree()
    {
        foreach (var key in TransLib.Keys)
        {
            TransLoader.Add(key, new AsyncLoader<TransNode>(this, TransLib[key]));
        }
    }

    public bool IsTrans() => IsInstanceValid(TransNode);

    public async GDTask Trans(SceneTrans trans)
    {
        if (IsInstanceValid(TransNode)) TransNode.QueueFree();
        if (IsInstanceValid(TransTween)) TransTween.Kill();

        TransNode = trans.GetTransNode();
        CallDeferred(Node.MethodName.AddChild, TransNode);
        await TransNode.OnReadyAsync();

        TransTween = CreateTween();
        if (trans.InTime > 0d)
            TransTween.TweenMethod((double p) => TransNode.TransInProcess(
                trans.Interpolation(p)), 0d, 1d, trans.InTime);
        if (trans.InWaitTime > 0d)
            TransTween.TweenInterval(trans.InWaitTime);
        TransTween.TweenCallback(() => EmitSignal(SignalName.TransInEnded));
        if (trans.OutWaitTime > 0d)
            TransTween.TweenInterval(trans.OutWaitTime);
        if (trans.OutTime > 0d)
            TransTween.TweenMethod((double p) => TransNode.TransOutProcess(
                trans.Interpolation(p)), 1d, 0d, trans.OutTime);

        await TransTween.AsGDTask();
        TransNode.QueueFree();
    }

    protected bool ChangeHint { get ;set; } = false;

    public void ChangeTo(PackedScene scene, SceneTrans trans = null)
    {
        if (ChangeHint) return;
        ChangeHint = true;

        if (trans == null)
        {
            GetTree().ChangeSceneToPacked(scene);
            ChangeHint = false;
            return;
        }

        SignalOneshotTransInEnded += () =>
        {
            GetTree().ChangeSceneToPacked(scene);
            ChangeHint = false;
        };
        Trans(trans).Forget();
    }

    public string GetScenePath(string path)
    {
        if (path is null or "") return GetTree().CurrentScene.SceneFilePath;
        
        if (path.StartsWith('@'))
        {
            var current = GetTree().CurrentScene.SceneFilePath;
            var index = current.LastIndexOf('_');
            return current[..(index + 1)] + path[1..] + ".tscn";
        }

        return path;
    }

    public void ChangeTo(string path, SceneTrans trans = null)
    {
        var targetPath = GetScenePath(path);
        ChangeTo(GD.Load<PackedScene>(targetPath), trans);
    }

    public void Reload(SceneTrans trans = null)
        => ChangeTo("", trans);
}