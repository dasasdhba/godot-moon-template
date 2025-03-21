using System;
using Utils;

namespace Godot;

[GlobalClass]
public partial class FsmNode : Node
{
    public enum FsmNodeProcessCallback { Idle, Physics }
    
    [ExportCategory("FsmNode")]
    [Export]
    public string State { get ;set; } = "";
    
    [Export]
    public FsmNodeProcessCallback ProcessCallback { get ;set; } 
        = FsmNodeProcessCallback.Physics;
        
    private Node DelegateNode;
    private FsmRoot Root;

    public FsmNode() : base()
    {
        Ready += () => Root = GetParent<FsmRoot>();
    }
    
    protected virtual void OnFsmStart() {}
    protected virtual void OnFsmStop() {}
    protected virtual Func<double, string> GetFsmProcess() => delta => "";

    public void FsmStop()
    {
        if (IsInstanceValid(DelegateNode))
        {
            DelegateNode.QueueFree();
            OnFsmStop();
        }
    }

    public void FsmStart()
    {
        if (IsInstanceValid(DelegateNode)) return; // this should not happen
        
        OnFsmStart();
        var process = GetFsmProcess();
        DelegateNode = this.AddProcess(delta =>
            {
                var next = process?.Invoke(delta);
                if (next != "") Root.State = next;
            }, 
            () => ProcessCallback == FsmNodeProcessCallback.Physics);
    }
}