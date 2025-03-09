using Godot;
using GodotTask;

namespace Global;

public partial class GameEntry : Node
{
    [ExportCategory("GameEntry")]
    [Export]
    public string FirstScenePath { get ;set; }

    protected virtual async GDTask GameInit()
    {
        await GDTask.RunOnThreadPool(() => { });
    }

    public override void _Ready()
    {
        GameStart().Forget();
    }

    protected string GetFirstScene()
    {
    #if TOOLS
        var path = Editor.Addon.MoonDebug.DebugFilePath;
        if (OS.IsDebugBuild() && FileAccess.FileExists(path))
        {
            var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var r = f.GetLine();
            f.Close();
            if (FileAccess.FileExists(r))
                return r;
        }
    #endif
    
        return FirstScenePath;    
    }

    protected virtual async GDTask GameStart()
    {
        await GameInit();
        var scene = GetFirstScene();
        Moon.Scene.FirstScenePath = scene;
        Moon.Scene.ChangeTo(scene, new ColorTrans());
        QueueFree();
    }
}