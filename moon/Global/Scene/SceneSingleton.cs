using Godot;
using GodotTask;

namespace Global;

public partial class SceneSingleton : CanvasLayer
{
    [ExportCategory("SceneSingleton")]
    [ExportGroup("TransformLib")]
    [Export]
    public PackedScene ColorTrans { get ;set; }

    [Signal]
    public delegate void TransInEndedEventHandler();

    protected TransNode TransNode { get ;set; }
    protected Tween TransTween { get ;set; }

    public bool IsTrans() => IsInstanceValid(TransNode);

    public async GDTaskVoid Trans(SceneTrans trans)
    {
        if (IsInstanceValid(TransNode)) TransNode.QueueFree();
        if (IsInstanceValid(TransTween)) TransTween.Kill();

        TransNode = trans.GetTransNode();
        CallDeferred("add_child", TransNode);
        await GDTask.ToSignal(TransNode, Node.SignalName.Ready);

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
        if (path == "" || path is null) return GetTree().CurrentScene.SceneFilePath;
        
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
        => ChangeTo("");
}