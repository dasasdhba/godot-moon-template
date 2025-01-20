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
    public Viewport MainViewport { get ;set; }
    
    [Export]
    public Dictionary<string, PackedScene> TransLib { get ;set; } = new();
    
    public System.Collections.Generic.Dictionary<string, AsyncLoader<TransNode>> TransLoader { get ;set; } = new();

    [Signal]
    public delegate void TransInEndedEventHandler();
    
    public Node CurrentScene { get; set; }
    public string CurrentScenePath { get; set; }

    protected TransNode TransNode { get ;set; }
    protected Tween TransTween { get ;set; }

    public override void _EnterTree()
    {
        MainViewport ??= GetViewport();
     
        foreach (var key in TransLib.Keys)
        {
            TransLoader.Add(key, new AsyncLoader<TransNode>(this, TransLib[key]));
        }
    }
    
#if TOOLS
    public override void _Ready()
    {
        if (!OS.IsDebugBuild()) return;
        
        var current = GetTree().CurrentScene;
        var path = current.SceneFilePath;
        
        current.QueueFree();
        ChangeTo(path);
    }
#endif

    private bool _Loading = false;
    public bool IsLoading() => _Loading;

    public async GDTask LoadScene(string path)
    {
        _Loading = true;
        
        await GDTask.RunOnThreadPool(() =>
        {
            var pack = GD.Load<PackedScene>(path);
            lock (AsyncLoader.InstantiateLock)
            {
                CurrentScene = pack.Instantiate();
                CurrentScenePath = path;
            }
        });
        
        _Loading = false;
    }

    public bool IsTrans() => IsInstanceValid(TransNode);

    public async GDTask TransIn(SceneTrans trans)
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
        
        await TransTween.AsGDTask();
        TransTween.Kill();
        
        _LastTrans = trans;
    }
    
    private SceneTrans _LastTrans;

    public async GDTask TransOut(SceneTrans trans = null)
    {
        if (trans != null)
        {
            if (IsInstanceValid(TransNode)) TransNode.QueueFree();
            if (IsInstanceValid(TransTween)) TransTween.Kill();

            TransNode = trans.GetTransNode();
            CallDeferred(Node.MethodName.AddChild, TransNode);
            await TransNode.OnReadyAsync();
            
            _LastTrans = trans;
        }

        if (_LastTrans is null)
        {
            return;
        }
        
        trans = _LastTrans;
        _LastTrans = null;
        
        TransTween = CreateTween();
        if (trans.OutWaitTime > 0d)
            TransTween.TweenInterval(trans.OutWaitTime);
        if (trans.OutTime > 0d)
            TransTween.TweenMethod((double p) => TransNode.TransOutProcess(
                trans.Interpolation(p)), 1d, 0d, trans.OutTime);

        await TransTween.AsGDTask();
        
        TransNode.QueueFree();
        TransTween.Kill();
    }

    private bool _ChangeHint;
    public bool IsChanging() => _ChangeHint;

    public void ChangeTo(string path, SceneTrans trans = null)
    {
        if (_ChangeHint) return;
        _ChangeHint = true;
        
        ChangeToAsync(path, trans).Forget();
    }

    private async GDTask ChangeToAsync(string path, SceneTrans trans = null)
    {
        var current = CurrentScene;
        var load = LoadScene(path);
        if (trans != null) await TransIn(trans);
        current?.QueueFree();
        await load;
        MainViewport.AddChild(CurrentScene);
        await CurrentScene.OnReadyAsync();
        if (trans != null) await TransOut();
        _ChangeHint = false;
    }

    public string GetScenePath(string path)
    {
        if (path is null or "") return CurrentScenePath;
        
        if (path.StartsWith('@'))
        {
            var current = CurrentScenePath;
            var index = current.LastIndexOf('_');
            return current[..(index + 1)] + path[1..] + ".tscn";
        }

        return path;
    }

    public void Reload(SceneTrans trans = null)
        => ChangeTo("", trans);

    public void SetPaused(bool paused)
    {
        if (!IsInstanceValid(CurrentScene)) return;
        CurrentScene.ProcessMode = paused ?
            ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
    }
}