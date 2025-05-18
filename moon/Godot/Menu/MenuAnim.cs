using System.Threading;
using GodotTask;
using Utils;

namespace Godot;

[GlobalClass]
public partial class MenuAnim : Node
{
    private const int MinWaitFrame = 4;
    
    public virtual async GDTask Appear(MenuRoot root, CancellationToken ct)
    {
        await this.AwaitPhysicsFrame(MinWaitFrame, ct);
        root.Show();
    }
    
    public virtual async GDTask Disappear(MenuRoot root, CancellationToken ct)
    {
        root.Hide();
        await this.AwaitPhysicsFrame(MinWaitFrame, ct);
    }

    public virtual void QuickShow(MenuRoot root)
    {
        root.Show();
    }
    
    public virtual void QuickHide(MenuRoot root)
    {
        root.Hide();
    }
}