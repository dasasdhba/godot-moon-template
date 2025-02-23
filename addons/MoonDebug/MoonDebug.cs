#if TOOLS

using Godot;

namespace Editor.Addon;

/// <summary>
/// helper to launch debug game
/// </summary>
[Tool]
public partial class MoonDebug : EditorPlugin
{
    public const string DebugFilePath = "res://moon_scene_path.debug";
    
    private string LastPath = "";
    
    public override void _Process(double delta)
    {
        var editor = EditorInterface.Singleton;
        var root = editor.GetEditedSceneRoot();
        if (root == null) return;
        
        var path = root.SceneFilePath;
        if (path == LastPath) return;
        LastPath = path;
        
        var f = FileAccess.Open(DebugFilePath, FileAccess.ModeFlags.Write);
        f.StoreString(path);
        f.Close();
    }
}
#endif