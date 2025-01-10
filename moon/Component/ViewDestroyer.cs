using Godot;
using Godot.Collections;
using Utils;

namespace Component;

[GlobalClass, Tool]
public partial class ViewDestroyer : ViewMonitor
{
    [ExportCategory("ViewDestroyer")]
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum ViewDestroyerProcessCallback { Idle, Physics }
    
    [Export]
    public ViewDestroyerProcessCallback ProcessCallback { get; set; } = ViewDestroyerProcessCallback.Physics;

    public override void _EnterTree()
    {
#if TOOLS
        if (Engine.IsEditorHint()) return;        
#endif

        this.AddProcess(Process, () => ProcessCallback == ViewDestroyerProcessCallback.Physics);
    }

    public void Process()
    {
        if (Disabled) return;

        if (!IsInView()) MonitorNode.QueueFree();
    }

}