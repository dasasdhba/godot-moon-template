using GodotTask;

namespace Godot;

[GlobalClass]
public partial class BTask: BTNode
{
    public virtual GDTask BTAsync()
    {
        return GDTask.RunOnThreadPool(() => {});
    }

    private GDTask BTNodeTask { get; set; }

    public override void BTReady() => BTNodeTask = BTAsync();

    public override bool BTProcess(double delta) => BTNodeTask.Status.IsCompleted();
}