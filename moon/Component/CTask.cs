using System;
using System.Threading;
using GodotTask;
using Utils;

namespace Component;

/// <summary>
/// task can be cancelled easily.
/// </summary>
public class CTask
{
    private GDTask Task;
    private CancellationTokenSource Cts = new();

    public CTask(Func<CancellationToken, GDTask> taskFunc)
    {
        Task = taskFunc(Cts.Token);
    }
        
    public Action OnCancel { get; init; }

    public void Cancel()
    {
        if (Task.Status.IsCompleted()) return;
        
        Cts.Cancel();
        OnCancel?.Invoke();
    }

    public async GDTask CancelAsync()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelAsync();
        OnCancel?.Invoke();
    }

    public async GDTask CancelSafely()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelSafely();
        OnCancel?.Invoke();
    }

    public async GDTask CancelPhysics()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelPhysics();
        OnCancel?.Invoke();
    }
    
    public GDTaskStatus Status => Task.Status;
    public GDTask.Awaiter GetAwaiter() => Task.GetAwaiter();
}

/// <summary>
/// task can be cancelled easily.
/// </summary>
public class CTask<T>
{
    private GDTask<T> Task;
    private CancellationTokenSource Cts = new();

    public CTask(Func<CancellationToken, GDTask<T>> taskFunc)
    {
        Task = taskFunc(Cts.Token);
    }
        
    public Action OnCancel { get; init; }

    public void Cancel()
    {
        if (Task.Status.IsCompleted()) return;
        
        Cts.Cancel();
        OnCancel?.Invoke();
    }
    
    public async GDTask CancelAsync()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelAsync();
        OnCancel?.Invoke();
    }
    
    public async GDTask CancelSafely()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelSafely();
        OnCancel?.Invoke();
    }

    public async GDTask CancelPhysics()
    {
        if (Task.Status.IsCompleted()) return;
        
        await Cts.CancelPhysics();
        OnCancel?.Invoke();
    }
    
    public GDTaskStatus Status => Task.Status;
    public GDTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();
}    