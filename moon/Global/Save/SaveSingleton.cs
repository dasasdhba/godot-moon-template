using System;
using Godot;
using Godot.Collections;

namespace Global;

public partial class SaveSingleton : Node
{
    private const string SaveFileName = "Save";
    private const string SaveFileSuffix = "sav";
    private const string SaveKey = "GODOTMOONTEMPLATE";

    private const string SingletonPath = "res://system/save.tscn";

    public static string CurrentSection { get ;set; } = "1";
    
    // we recommend to do file access in a thread to avoid blocking the game

    #region FileAccess

    private static string GetGamePath() 
        => OS.GetExecutablePath().GetBaseDir();

    private static string GetSavePath()
        => GetGamePath() + "/" + SaveFileName + "." + SaveFileSuffix;

    // useful for temp save
    public PackedScene SavePacked()
    {
        var pack = new PackedScene();
        pack.Pack(this);
        return pack;
    }

    public void LoadPacked(PackedScene pack)
    {
        var save = pack.Instantiate<SaveSingleton>();
        foreach (var node in save.GetChildren())
        {
            if (node is SaveItem item)
                GetNode<SaveItem>((string)item.Name).Items = item.Items;
        }
        
        save.QueueFree();
    }

    public bool SaveSection(string section)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            Error err = config.LoadEncryptedPass(GetSavePath(), SaveKey);
            if (err != Error.Ok) return false;
        }

        if (config.HasSection(section))
            config.EraseSection(section);

        foreach (var node in GetChildren())
        {
            if (node is SaveItem item)
                config.SetValue(section, item.Name, item.Items);
        }

        return config.SaveEncryptedPass(GetSavePath(), SaveKey) == Error.Ok;
    }

    public bool Save() => SaveSection(CurrentSection);

    public bool LoadSection(string section)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            Error err = config.LoadEncryptedPass(GetSavePath(), SaveKey);
            if (err != Error.Ok) return false;
        }

        // set to default first
        LoadPacked(GD.Load<PackedScene>(SingletonPath));

        foreach (var node in GetChildren())
        {
            if (node is SaveItem item)
            {
                if (config.HasSectionKey(section, item.Name))
                {
                    var dict = (Dictionary<string, Variant>)
                        config.GetValue(section, item.Name);
                    foreach (var key in dict.Keys)
                    { 
                        item.Items[key] = dict[key];
                    }
                }
            }
                
        }

        return true;
    }

    public bool Load() => LoadSection(CurrentSection);

    public static bool SaveSectionDict(string section, string item, Action<Dictionary<string, Variant>> saveAction)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            Error err = config.LoadEncryptedPass(GetSavePath(), SaveKey);
            if (err != Error.Ok) return false;
        }

        var savedDict = (Dictionary<string, Variant>)config.GetValue(section, item);
        saveAction.Invoke(savedDict);
        config.SetValue(section, item, savedDict);

        return config.SaveEncryptedPass(GetSavePath(), SaveKey) == Error.Ok;
    }

    public static bool SaveDict(string item, Action<Dictionary<string, Variant>> saveAction)
        => SaveSectionDict(CurrentSection, item, saveAction);

    public static bool SaveSectionItem(string section, string item, string key, Variant value)
        => SaveSectionDict(section, item, (dict) => dict[key] = value);

    public static bool SaveItem(string item, string key, Variant value)
        => SaveSectionItem(CurrentSection, item, key, value);

    public static Dictionary<string, Variant> LoadSectionDict(string section, string item)
    {
        var config = new ConfigFile();
        if (FileAccess.FileExists(GetSavePath()))
        {
            Error err = config.LoadEncryptedPass(GetSavePath(), SaveKey);
            if (err != Error.Ok) return new();
        }

        return (Dictionary<string, Variant>)config.GetValue(section, item);
    }

    public static Dictionary<string, Variant> LoadDict(string item)
        => LoadSectionDict(CurrentSection, item);

    public static T LoadSectionItem<[MustBeVariant] T>(string section, string item, string key, T @default = default)
    {
        var savedDict = LoadSectionDict(section, item);
        return savedDict.ContainsKey(key) ? savedDict[key].As<T>() : @default;
    }

    public static T LoadItem<[MustBeVariant] T>(string item, string key, T @default = default)
        => LoadSectionItem(CurrentSection, item, key, @default);

    #endregion

    #region NodeAccess

    public Dictionary<string, Variant> GetItemDict(string item)
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