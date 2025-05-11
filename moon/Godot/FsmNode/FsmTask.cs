using System;
using System.Threading;
using Component;
using GodotTask;

namespace Godot;

public abstract partial class FsmTask : FsmNode
{
    private CTask<string> Task;
    
    protected abstract GDTask<string> GetFsmTask(CancellationToken ct);
    protected virtual void OnTaskCancelled() { }

    protected override void OnFsmStart()
    {
        Task = new(GetFsmTask) { OnCancel = OnTaskCancelled };
    }

    protected override void OnFsmStop()
    {
        Task?.Cancel();
    }
    
    protected override Func<double, string> GetFsmProcess()
    {
        return delta =>
        {
            if (Task.Status.IsCompleted())
            {
                var result = Task.GetAwaiter().GetResult();
                Task = null;
                
                if (result == State || result == "") OnFsmStart();
                return result;
            }
            
            return "";
        };
    }
}