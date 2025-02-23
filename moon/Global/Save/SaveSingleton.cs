using System;
using Godot;
using System.Collections.Generic;
using GodotTask;
using Utils;

namespace Global;

public partial class SaveSingleton : Node
{
    [ExportCategory("SaveSingleton")]
    [Export]
    public string SaveFileName { get ;set; } = "Save";
    
    [Export]
    public string SaveFileSuffix { get ;set; } = "sav";
    
    [Export]
    public string SaveKey { get ;set; } = "GODOTMOONTEMPLATE";

    public static string CurrentSection { get ;set; } = "1";

    #region FileAccess

    private static string GetGamePath() 
        => OS.GetExecutablePath().GetBaseDir();

    private string GetSavePath()
        => GetGamePath() + "/" + SaveFileName + "." + SaveFileSuffix;

    private void Reset()
    {
        foreach (var node in GetChildren())
        {
            if (node is SaveItem item)
                item.Items.Clear();
        }
    }

    private Dictionary<string, Variant> SaveAsDict()
    {
        var result = new Dictionary<string, Variant>();
        foreach (var node in GetChildren())
        {
            if (node is SaveItem item)
            {
                result.Add(item.Name, item.Items.Duplicate());
            }
        }
        return result;
    }

    private void LoadFromDict(Dictionary<string, Variant> dict)
    {
        // reset first
        Reset();

        foreach (var node in GetChildren())
        {
            if (node is SaveItem item)
            {
                if (dict.TryGetValue(item.Name, out var value))
                {
                    var godotDict = value.As<Godot.Collections.Dictionary<string, Variant>>();
                    item.Items = godotDict;
                }
            }
        }
    }

    public async GDTask<bool> SaveSection(string section)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            var err = await GDTask.RunOnThreadPool(() => 
                config.LoadEncryptedPass(GetSavePath(), SaveKey));
            if (err != Error.Ok) return false;
        }

        if (config.HasSection(section)) config.EraseSection(section);
        config.SetSection(section, SaveAsDict());
        
        var result = await GDTask.RunOnThreadPool(() =>
            config.SaveEncryptedPass(GetSavePath(), SaveKey));
            
        #if TOOLS
        if (OS.IsDebugBuild())
        {
            await GDTask.RunOnThreadPool(() =>
                config.Save(GetSavePath() + ".txt"));
        }
        #endif

        return result == Error.Ok;
    }

    public GDTask<bool> Save() => SaveSection(CurrentSection);

    public async GDTask<bool> LoadSection(string section)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            var err = await GDTask.RunOnThreadPool(() => 
                config.LoadEncryptedPass(GetSavePath(), SaveKey));
            if (err != Error.Ok) return false;
        }
        
        LoadFromDict(config.GetSection(section));

        return true;
    }

    public GDTask<bool> Load() => LoadSection(CurrentSection);

    public async GDTask<bool> SaveSectionDict(string section, string item, 
        Action<Godot.Collections.Dictionary<string, Variant>> saveAction)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            var err = await GDTask.RunOnThreadPool(() =>
                config.LoadEncryptedPass(GetSavePath(), SaveKey));
            if (err != Error.Ok) return false;
        }

        var savedDict = (Godot.Collections.Dictionary<string, Variant>)config.GetValue(section, item);
        saveAction.Invoke(savedDict);
        config.SetValue(section, item, savedDict);
        
        var result = await GDTask.RunOnThreadPool(() =>
            config.SaveEncryptedPass(GetSavePath(), SaveKey));
        return result == Error.Ok;
    }

    public GDTask<bool> SaveDict(string item, Action<Godot.Collections.Dictionary<string, Variant>> saveAction)
        => SaveSectionDict(CurrentSection, item, saveAction);

    public GDTask<bool> SaveSectionItem(string section, string item, string key, Variant value)
        => SaveSectionDict(section, item, (dict) => dict[key] = value);

    public GDTask<bool> SaveItem(string item, string key, Variant value)
        => SaveSectionItem(CurrentSection, item, key, value);

    public async GDTask<Godot.Collections.Dictionary<string, Variant>> LoadSectionDict(string section, string item)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            var err = await GDTask.RunOnThreadPool(() =>
                config.LoadEncryptedPass(GetSavePath(), SaveKey));
            if (err != Error.Ok) return new();
        }

        return (Godot.Collections.Dictionary<string, Variant>)config.GetValue(section, item);
    }

    public GDTask<Godot.Collections.Dictionary<string, Variant>> LoadDict(string item)
        => LoadSectionDict(CurrentSection, item);

    public async GDTask<T> LoadSectionItem<[MustBeVariant] T>(string section, string item, string key, T @default = default)
    {
        var savedDict = await LoadSectionDict(section, item);
        return savedDict.ContainsKey(key) ? savedDict[key].As<T>() : @default;
    }

    public GDTask<T> LoadItem<[MustBeVariant] T>(string item, string key, T @default = default)
        => LoadSectionItem(CurrentSection, item, key, @default);

    #endregion

    #region NodeAccess

    public Godot.Collections.Dictionary<string, Variant> GetItemDict(string item)
        => GetNode<SaveItem>(item).Items;

    public void SetItemValue(string item, string key, Variant value)
        => GetItemDict(item)[key] = value;

    public T GetItemValue<[MustBeVariant] T>(string item, string key, T @default = default)
    {
        var dict = GetItemDict(item);
        return dict.ContainsKey(key) ? dict[key].As<T>() : @default;
    }

    #endregion
}