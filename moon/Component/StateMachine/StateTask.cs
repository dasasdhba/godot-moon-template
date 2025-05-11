using System.Threading;
using GodotTask;

namespace Component;

public abstract class StateTask : StateProcess
{
    private CTask<StateProcess> _Task;
    protected abstract GDTask<StateProcess> GetStateTask(CancellationToken ct);
    protected virtual void OnTaskCancelled() { }

    public override void OnStateStart()
    {
        _Task = new(GetStateTask) { OnCancel = OnTaskCancelled };
    }

    public override void OnStateEnd()
    {
        _Task?.Cancel();
    }

    public override StateProcess OnStateProcess(double delta)
    {
        if (_Task.Status.IsCompleted())
        {
            var result = _Task.GetAwaiter().GetResult();
            _Task = null;
            
            if (result == this) OnStateStart();
            return result;
        }
        
        return this;
    }
}