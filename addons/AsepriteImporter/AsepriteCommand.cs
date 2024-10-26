#if TOOLS

using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Editor.Addon;

/// <summary>
/// Aseprite command line tool.
/// </summary>
public partial class AsepriteCommand
{
    private AsepriteConfig Config;

    public AsepriteCommand(AsepriteConfig config) => Config = config;

    public bool IsAsepriteValid()
    {
        var asepritePath = Config.GetAsepritePath();
        
        try
        {
            OS.Execute(asepritePath, new string[] { "--version" }, new Godot.Collections.Array(), true);
        }
#pragma warning disable CS0168 // Variable is declared but never used
        catch(Exception _)
#pragma warning restore CS0168 // Variable is declared but never used
        {
            GD.PushWarning("invalid Aseprite exec path, please set it up in " +
                "Editor->Editor Setting->Aseprite->General");
            return false;
        }

        return true;
    }

    public Dictionary<string, string> ExportFile(string fileName, string outputFolder,
        Dictionary<string, Variant> options)
    {
        if (!IsAsepriteValid())
            return new();

        var exceptionPattern = "";
        if (options.TryGetValue("exception_pattern", out var var))
            exceptionPattern = (string)var;

        bool onlyVisibleLayers = false;
        if (options.TryGetValue("only_visible_layers", out var))
            onlyVisibleLayers = (bool)var;

        var outputName = GetFileBasename(fileName);
        var resFileName = outputFolder + "/" + outputName;
        var resData = resFileName + ".json";
        var resSprite = resFileName + ".png";

        var outputDir = ProjectSettings.GlobalizePath(outputFolder);
        var outputFile = outputDir + "/" + outputName;
        var dataFile = outputFile + ".json";
        var spriteSheet = outputFile + ".png";
        
        var sourceName = ProjectSettings.GlobalizePath(fileName);
        Godot.Collections.Array<string> arguments = 
            GetCommandArguments(sourceName, dataFile, spriteSheet);

        if (!onlyVisibleLayers)
            arguments.Insert(0, "--all-layers");

        AddSheetTypeArguments(arguments, options);
        AddIgnoreLayerArguments(sourceName, arguments, exceptionPattern);

        Godot.Collections.Array output = new();
        if (Execute(arguments.ToArray(), output) != 0)
        {
            GD.PushError("Aseprite: failed to export layer spritesheet");
            GD.PushError(output);
            return new Dictionary<string, string>();
        }

        return new Dictionary<string, string>()
        {
            {"data_file", resData },
            {"sprite_sheet", resSprite }
        };
    }

    private int Execute(string[] arguments, Godot.Collections.Array output)
        => OS.Execute(Config.GetAsepritePath(), arguments, output, true, true);

    private static Godot.Collections.Array<string> GetCommandArguments(
    string sourceName, string dataPath, string spritesheetPath)
    {
        if (spritesheetPath == "")
        {
            return new Godot.Collections.Array<string>()
            {
                "-b",
                "--list-tags",
                "--data",
                dataPath,
                "--format",
                "json-array",
                sourceName
            };
        }

        if (dataPath == "")
        {
            return new Godot.Collections.Array<string>()
            {
                "-b",
                "--sheet",
                spritesheetPath,
                sourceName
            };
        }

        return new Godot.Collections.Array<string>()
        {
            "-b",
            "--list-tags",
            "--data",
            dataPath,
            "--format",
            "json-array",
            "--sheet",
            spritesheetPath,
            sourceName
        };
    }

    private static string GetFileBasename(string filepath)
    {
        var fileName = StringExtensions.GetFile(filepath);
        var extLen = StringExtensions.GetExtension(fileName).Length;
        return fileName[..^(extLen + 1)];
    }

    private static void AddSheetTypeArguments(
        Godot.Collections.Array<string> arguments, Dictionary<string, Variant> options)
    {
        var count = 0;
        if (options.TryGetValue("column_count", out Variant var))
            count = (int)var;

        if (count > 0)
        {
            arguments.Add("--merge-duplicates");
            arguments.Add("--sheet-columns");
            arguments.Add(Convert.ToString(count));
        }
        else
        {
            arguments.Add("--sheet-pack");
        }
    }

    private void AddIgnoreLayerArguments(string sourceName,
        Godot.Collections.Array<string> arguments, string exceptionPattern)
    {
        Godot.Collections.Array<string> layers = 
            GetExceptionLayers(sourceName, exceptionPattern);

        if (layers.Count == 0)
            return;

        foreach (var str in layers)
        {
            arguments.Insert(0, str);
            arguments.Insert(0, "--ignore-layer");
        }

    }

    private Godot.Collections.Array<string> GetLayers(
        string sourceName, bool onlyVisible = false)
    {
        Godot.Collections.Array output = new();
        Godot.Collections.Array<string> arguments = new()
        {
            "-b",
            "--list-layers",
            sourceName
        };

        if (!onlyVisible)
            arguments.Insert(0, "--all-layers");

        if (Execute(arguments.ToArray(), output) != 0)
        {
            GD.PushError("Aseprite: failed listing layers.");
            GD.PushError(output);
            return new Godot.Collections.Array<string>();
        }

        if (output.Count == 0)
            return new Godot.Collections.Array<string>();

        Godot.Collections.Array<string> result = new();
        foreach (var str in output[0].ToString().Split('\n'))
            result.Add(StringExtensions.StripEdges(str));

        return result;
    }

    private Godot.Collections.Array<string> GetExceptionLayers(
        string sourceName, string exceptionPattern)
    {
        Godot.Collections.Array<string> result = new();

        var layers = GetLayers(sourceName);
        var regex = CompileRegex(exceptionPattern);

        if (regex == null)
            return result;

        foreach (var str in layers)
        {
            if (regex.Search(str) != null)
            {
                result.Add(str);
            }
        }

        return result;
    }

    private static RegEx CompileRegex(string pattern)
    {
        if (pattern == "")
            return null;

        RegEx rgx = new();
        if (rgx.Compile(pattern) == Error.Ok)
            return rgx;

        GD.PushError("Exception regex error.");
        return null;
    }
}

#endif