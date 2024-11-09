#if TOOLS

using Godot;

namespace Editor.Addon;

/// <summary>
/// Aseprite Importer main plugin.
/// </summary>
[Tool]
public partial class AsepriteImporter : EditorPlugin
{
    private AsepriteConfig Config;
    private AsepriteImporterPlugin Importer;

    // since importer plugin has to be constructed parameterlessly
    // (weary)
    // we set up the required instance as static
    public static AsepriteCommand Command { get; set; }
    public static EditorFileSystem ResourceFilesystem { get; set; }
    public static AsepriteImporter Plugin { get; set; }

    public AsepriteImporter() : base()
    {
        Config = new(EditorInterface.Singleton.GetEditorSettings());
        Config.AddSettings();

        Command = new(Config);
        ResourceFilesystem = EditorInterface.Singleton.GetResourceFilesystem();
        
        Plugin = this;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        Importer = new();
        AddImportPlugin(Importer);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Config.RemoveSettings();
        RemoveImportPlugin(Importer);
    }
    
    private const double ScanDelayTime = 0.8d;
    private double ScanTimer = 0d;
    private bool ScanScheduled = false;

    public void ScheduleScan()
    {
        ScanScheduled = true;
        ScanTimer = ScanDelayTime;
    }

    public override void _Process(double delta)
    {
        if (!ScanScheduled) return;
        
        ScanTimer -= delta;
        if (ScanTimer <= 0d)
        {
            ScanScheduled = false;
            ResourceFilesystem.CallDeferred(EditorFileSystem.MethodName.Scan);
        }
    }
}

#endif