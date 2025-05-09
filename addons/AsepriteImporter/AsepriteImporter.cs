#if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Editor.Addon;

/// <summary>
/// Aseprite Importer main plugin.
/// </summary>
[Tool]
public partial class AsepriteImporter : EditorPlugin, ISerializationListener
{
    private AsepriteConfig Config;
    private AsepriteImporterPlugin Importer;

    // since importer plugin has to be constructed parameterlessly
    // (weary)
    // we set up the required instance as static
    public static AsepriteCommand Command { get; private set; }
    public static EditorFileSystem ResourceFilesystem { get; private set; }
    public static AsepriteImporter Plugin { get; private set; }

    public AsepriteImporter() : base()
    {
        Plugin = this;
        Config = new(EditorInterface.Singleton.GetEditorSettings());
        Command = new(Config);
        ResourceFilesystem = EditorInterface.Singleton.GetResourceFilesystem();
        
        ResourceFilesystem.ResourcesReimported += OnResourcesReimported;
    }

    public void OnBeforeSerialize()
    {
        ResourceFilesystem.ResourcesReimported -= OnResourcesReimported;
    }

    public void OnAfterDeserialize() {}

    private void OnResourcesReimported(string[] f)
    {
        if (ReimportFiles.Count > 0)
        {
            ReimportScheduled = true;
            ReimportTimer = ReimportDelayTime;
        }

        if (ScanTimer > 0d)
        {
            ScanScheduled = true;
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        Config.AddSettings();
        Importer = new();
        AddImportPlugin(Importer);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        RemoveImportPlugin(Importer);
    }

    public override void _DisablePlugin()
    {
        base._DisablePlugin();
        
        Config.RemoveSettings();
    }

    private const double ScanDelayTime = 0.5d;
    private double ScanTimer = 0d;
    private bool ScanScheduled = false;
    public void ScheduleScan() 
        => ScanTimer = ScanDelayTime;
    
    private const double ReimportDelayTime = 0.5d;
    private double ReimportTimer = 0d;
    private bool ReimportScheduled = false;
    private HashSet<string> ReimportFiles = [];
    public void ScheduleReimport(string file)
        => ReimportFiles.Add(file);

    public override void _Process(double delta)
    {
        if (ResourceFilesystem.IsScanning()) return;
    
        if (ReimportScheduled && !ScanScheduled)
        {
            ReimportTimer -= delta;
            if (ReimportTimer <= 0d)
            {
                ReimportScheduled = false;
                if (ReimportFiles.Count > 0)
                {
                    ResourceFilesystem.ReimportFiles(ReimportFiles.ToArray());
                    ReimportFiles.Clear();
                }
            }
        }
    
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