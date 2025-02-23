using Component;
using Godot;
using Godot.Collections;
using GodotTask;
using GodotTask.Triggers;
using Utils;

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
    
    [Signal]
    public delegate void TransOutEndedEventHandler();
    
    [Signal]
    public delegate void SceneChangedEventHandler();
    
    public Node CurrentScene { get; set; }
    public string CurrentScenePath { get; set; }
    public string FirstScenePath { get; set; }

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
        if (current is GameEntry) return;
        
        GD.PushWarning("Please run entire game with MoonDebug plugin instead.");
        var path = current.SceneFilePath;
        
        // this may cause some error
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
            CurrentScene = pack.InstantiateSafely();
            CurrentScenePath = path;
        });
        
        _Loading = false;
    }

    public bool IsTrans() => IsInstanceValid(TransNode);
    public bool IsTransIn() => _LastTrans != null;
    
    private CTask TransTask;

    public void TransIn(SceneTrans trans)
    {
        if (TransTask != null)
        {
            if (!IsTrans()) EmitSignal(SignalName.TransOutEnded);
            TransTask.Cancel();
        }
        
        TransTask = new(async ct =>
        {
            if (IsInstanceValid(TransNode)) TransNode.QueueFree();
            if (IsInstanceValid(TransTween)) TransTween.Kill();
            
            _LastTrans = trans;

            TransNode = trans.GetTransNode();
            CallDeferred(Node.MethodName.AddChild, TransNode);
            await TransNode.OnReadyAsync();

            TransTween = CreateTween();
            if (trans.InTime > 0d)
                TransTween.TweenMethod((double p) => TransNode.TransInProcess(
                    trans.Interpolation(p)), 0d, 1d, trans.InTime);
            if (trans.InWaitTime > 0d)
                TransTween.TweenInterval(trans.InWaitTime);
        
            await Async.Wait(this, TransTween, ct);
            EmitSignal(SignalName.TransInEnded);
        });
    }

    public async GDTask TransInAsync(SceneTrans trans)
    {
        TransIn(trans);
        await GDTask.ToSignal(this, SignalName.TransInEnded);
    }
    
    private SceneTrans _LastTrans;

    public void TransOut(SceneTrans trans = null)
    {
        if (TransTask != null)
        {
            if (IsTrans()) EmitSignal(SignalName.TransInEnded);
            TransTask.Cancel();
        }
        
        TransTask = new(async ct =>
        {
            if (trans != null)
            {
                if (IsInstanceValid(TransTween)) TransTween.Kill();

                var transNode = trans.GetTransNode();
                CallDeferred(Node.MethodName.AddChild, TransNode);
                await TransNode.OnReadyAsync();
            
                if (IsInstanceValid(TransNode)) TransNode.QueueFree();
                TransNode = transNode;
            
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
        
            await Async.Wait(this, TransTween, ct);
            TransNode.QueueFree();
            TransTween.Kill();
            EmitSignal(SignalName.TransOutEnded);
        });
    }

    public async GDTask TransOutAsync(SceneTrans trans = null)
    {
        TransOut(trans);
        await GDTask.ToSignal(this, SignalName.TransOutEnded);
    }

    private bool _ChangeHint;
    public bool IsChanging() => _ChangeHint;

    public void ChangeTo(string path, SceneTrans trans = null)
    {
        if (_ChangeHint) return;
        _ChangeHint = true;
        
        ChangeToAsync(GetScenePath(path), trans).Forget();
    }

    private async GDTask ChangeToAsync(string path, SceneTrans trans = null)
    {
        var current = CurrentScene;
        var load = LoadScene(path);
        if (trans != null && !IsTransIn()) await TransInAsync(trans);
        if (current != null)
        {
            current.Name = "@OldScene";
            current.QueueFree();
        }
        await load;
        MainViewport.AddChild(CurrentScene);
        await CurrentScene.OnReadyAsync();
        _ChangeHint = false;
        EmitSignal(SignalName.SceneChanged);
        if (IsTransIn()) await TransOutAsync();
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
}