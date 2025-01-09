using System;
using System.Threading;
using GodotTask;

namespace Component;

public class TaskCanceller
{
    private GDTask Task;
    private CancellationTokenSource Cts = new();

    public TaskCanceller(Func<CancellationToken, GDTask> taskFunc)
    {
        Task = taskFunc(Cts.Token);
    }
        
    public Action OnCancel { get; set; }

    public void Cancel()
    {
        if (Task.Status.IsCompleted()) return;
        
        Cts.Cancel();
        OnCancel?.Invoke();
    }
        
    public GDTask GetTask() => Task;
}
    
public class TaskCanceller<T>
{
    private GDTask<T> Task;
    private CancellationTokenSource Cts = new();

    public TaskCanceller(Func<CancellationToken, GDTask<T>> taskFunc)
    {
        Task = taskFunc(Cts.Token);
    }
        
    public Action OnCancel { get; set; }

    public void Cancel()
    {
        if (Task.Status.IsCompleted()) return;
        
        Cts.Cancel();
        OnCancel?.Invoke();
    }
        
    public GDTask<T> GetTask() => Task;
}    