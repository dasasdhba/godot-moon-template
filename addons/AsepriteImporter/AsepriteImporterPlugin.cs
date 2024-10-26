#if TOOLS

using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text;

namespace Editor.Addon;

/// <summary>
/// Aseprite Importer that imports aseprite files as SpriteFrames
/// </summary>
[Tool]
public partial class AsepriteImporterPlugin : EditorImportPlugin
{
    public AsepriteImporterPlugin() : base() { }

    public override bool _CanImportThreaded()
        => false;
    
    public override string _GetImporterName()
        => "aseprite_importer.plugin";

    public override string _GetVisibleName()
        => "Aseprite Importer";

    public override string[] _GetRecognizedExtensions()
        => new string[] { "aseprite", "ase" };

    public override string _GetSaveExtension()
        => "tres";

    public override string _GetResourceType()
        => "SpriteFrames";

    public override int _GetPresetCount() => 1;

    public override string _GetPresetName(int _)
        => "Default";

    public override float _GetPriority() => 1.0f;

    public override int _GetImportOrder() => 1;

    public override bool _GetOptionVisibility(string _, StringName __, Dictionary ___)
        => true;

    public override Array<Dictionary> _GetImportOptions(string _, int __)
        => new()
        {
            new()
            {
                {"name", "exclude_layers_pattern" },
                {"default_value", AsepriteConfig.GetExclusionPattern() }
            },
            new()
            {
                {"name", "only_visible_layers" },
                {"default_value", false }
            },
            new()
            {
                {"name", "tag_only" },
                {"default_value", true }
            },
            new()
            {
                {"name", "loop" },
                {"default_value", true }
            },
            new()
            {
                {"name", "sheet_type" },
                {"default_value", "Packed" },
                {"property_hint", (int)PropertyHint.Enum },
                {"hint_string", GetSheetTypeHintString() }
            }
        };

    private static string GetSheetTypeHintString()
    {
        var hint = new StringBuilder();
        hint.Append("Packed");
        foreach (var i in new int[] { 2, 4, 8, 16, 32 })
            hint.Append("," + i.ToString() + " columns");
        hint.Append(",Strip");
        return hint.ToString();
    }

    private static int GetSheetTypeColumns(string str)
        => str switch
        {
            "Packed" => 0,
            "2 columns" => 2,
            "4 columns" => 4,
            "8 columns" => 8,
            "16 columns" => 16,
            "32 columns" => 32,
            "Strip" => 128,
            _ => 0
        };

    public override Error _Import(string sourceFile, string savePath,
        Dictionary options, Array<string> _, Array<string> __)
    {
        var spr = GenerateImportFiles(sourceFile, options);

        if (spr == null) return Error.Failed;
            
        ResourceSaver.Save(spr, savePath + ".tres");

        return Error.Ok;
    }

    private SpriteFrames GenerateImportFiles(string sourceFile, Dictionary options)
    {
        var Command = AsepriteImporter.Command;
        var ResourceFilesystem = AsepriteImporter.ResourceFilesystem;

        if (Command == null || ResourceFilesystem == null)
            return null;

        System.Collections.Generic.Dictionary<string, Variant> aseOpt = new()
        {
            {"exception_pattern", options["exclude_layers_pattern"] },
            {"only_visible_layers", options["only_visible_layers"] },
            {"column_count",  GetSheetTypeColumns((string)options["sheet_type"]) }
        };

        // generate texture and json
        
        List<AsepriteFrames.FramesInfo> infos = new();

        foreach (var file in GetRelevantFiles(sourceFile))
        {
            System.Collections.Generic.Dictionary<string, Variant> opt;
            var loop = (bool)options["loop"];
            var tagOnly = (bool)options["tag_only"];

            if (file != sourceFile && FileAccess.FileExists(file + ".import"))
            {
                ConfigFile config = new();
                config.Load(file + ".import");

                opt = new()
                    {
                        {"exception_pattern", config.GetValue(
                            "params", "exclude_layers_pattern", "") },
                        {"only_visible_layers", config.GetValue(
                            "params", "only_visible_layers", false) },
                        {"column_count",  GetSheetTypeColumns((string)config.GetValue(
                            "params", "sheet_type", "Packed")) }
                    };

                loop = (bool)config.GetValue("params", "loop", true);
                tagOnly = (bool)config.GetValue("params", "tag_only", true);
            }
            else
            {
                opt = aseOpt;
            }

            // generate
            
            var outFile = Command.ExportFile(
                file, file.GetBaseDir(), opt);

            if (!outFile.ContainsKey("sprite_sheet"))
            {
                GD.PushError("Importing aseprite file failed, please check" +
                    "Aseprite Exec Path in editor settings.");
                return null;
            }
            
            var texPath = outFile["sprite_sheet"];
            var jsonPath = outFile["data_file"];

            infos.Add(new AsepriteFrames.FramesInfo(
                texPath,
                LoadJson(jsonPath),
                GetAnimName(file),
                loop,
                tagOnly
                ));

            // try import images
            
            var imported = FileAccess.FileExists(texPath + ".import");
            if (imported)
            {
                AsepriteImporter.Plugin.ScheduleScan();
            }
            else
            {
                ResourceFilesystem.UpdateFile(texPath);
                AppendImportExternalResource(texPath);
            }
            
            // remove json
            
            if (AsepriteConfig.GetRemoveJson())
            {
                DirAccess.RemoveAbsolute(jsonPath);
            }
        }

        var spr = new AsepriteFrames(infos).Create();

        return spr;
    }

    private static string GetBaseName(string sourceFile)
    {
        var fileName = sourceFile.GetFile();
        var extLen = fileName.GetExtension().Length;
        var BaseName = fileName[..^(extLen + 1)];
        var dotPos = BaseName.LastIndexOf('.');
        if (dotPos == -1)
        {
            return BaseName;
        }
        return BaseName[..dotPos];
    }

    private static string GetAnimName(string sourceFile)
    {
        var fileName = sourceFile.GetFile();
        var extLen = fileName.GetExtension().Length;
        var BaseName = fileName[..^(extLen + 1)];
        var dotPos = BaseName.LastIndexOf('.');
        if (dotPos == -1)
        {
            return "Default";
        }
        return BaseName[(dotPos + 1)..^0];
    }

    private IEnumerable<string> GetRelevantFiles(string sourceFile)
    {
        var path = sourceFile.GetBaseDir();
        var baseName = GetBaseName(sourceFile);

        foreach (var file in DirAccess.GetFilesAt(path))
        {
            if (System.Array.Exists(_GetRecognizedExtensions(),
                str => str == file.GetExtension())
                && GetBaseName(file) == baseName)
            {
                yield return path + "/" + file;
            }
        }
    }

    private static Dictionary LoadJson(string json)
    {
        var file = FileAccess.Open(json, FileAccess.ModeFlags.Read);
        Json jsonObj = new();
        jsonObj.Parse(file.GetAsText());
        file.Close();

        return (Dictionary)jsonObj.Data;
    }
}

#endif