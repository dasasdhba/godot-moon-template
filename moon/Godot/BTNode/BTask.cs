using System.Threading;
using GodotTask;

namespace Godot;

[GlobalClass]
public partial class BTask: BTNode
{
    public virtual async GDTask BTAsync(CancellationToken ct)
        => await GDTask.RunOnThreadPool(() => {}, ct);

    private GDTask BTNodeTask { get; set; }
    
    private CancellationTokenSource Cts;

    public override void BTReady()
    {
        Cts = new();
        BTNodeTask = BTAsync(Cts.Token);
    }

    public override bool BTProcess(double delta) 
        => BTNodeTask.Status.IsCompleted();

    public override void BTStop()
        => Cts?.Cancel();
}