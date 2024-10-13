using System.Threading.Tasks;

namespace Godot;

[GlobalClass]
public partial class BTask: BTNode
{
    public virtual Task BTAsync()
    {
        return Task.Run(() => { });
    }

    private Task BTNodeTask { get; set; }

    public override void BTReady() => BTNodeTask = BTAsync();

    public override bool BTProcess(double delta) => BTNodeTask.IsCompleted;
}