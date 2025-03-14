﻿using Godot;
using Godot.Collections;
using GodotTask;

namespace Global;

public partial class SaveSingleton
{
    [ExportGroup("Config")]
    [Export]
    public string ConfigFileName { get ;set; } = "Settings";
    
    [Export]
    public string ConfigFileSuffix { get ;set; } = "ini";
    
    [Export]
    public Array<string> SupportedLanguages { get ;set; } = [ "zh" ];
    
    [Export]
    public string FallbackLanguage { get; set; } = "zh";
    
    public void SetLanguage(string locale)
    {
        if (!SupportedLanguages.Contains(locale)) locale = FallbackLanguage;
        TranslationServer.SetLocale(locale);
    }
    
    public static bool Effect { get ;set; } = true;
    
    private string GetConfigPath()
        => GetGamePath() + "/" + ConfigFileName + "." + ConfigFileSuffix;

    public override void _EnterTree()
    {
        LoadConfig().Forget();
    }

    private async GDTask LoadConfig()
    {
        ConfigFile config = new();
        if (config.Load(GetConfigPath()) != Error.Ok)
        {
            // reset
            SetLanguage(OS.GetLocale());
            var locale = TranslationServer.GetLocale();
            config.SetValue("Setting", "Music", 100);
            config.SetValue("Setting", "Sound", 100);
            config.SetValue("Setting", "Fullscreen", false);
            config.SetValue("Setting", "VSync", true);
            config.SetValue("Setting", "Effect", true);
            config.SetValue("Setting", "Language", locale);
            foreach (string action in InputMap.GetActions())
            {
                // exclude build-in
                if (action.StartsWith("ui"))
                    continue;
                config.SetValue("Control", action, InputMap.ActionGetEvents(action));
            }

            await GDTask.RunOnThreadPool(() =>
            {
                config.Save(GetConfigPath());
            });
        }
        else
        {
            // load
            static float volumeDB(int volume) => Mathf.LinearToDb(volume / 100f);
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"),
                volumeDB((int)config.GetValue("Setting", "Music", 100)));
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Sound"),
                volumeDB((int)config.GetValue("Setting", "Sound", 100)));
            DisplayServer.WindowSetMode((bool)config.GetValue("Setting", "Fullscreen", false) ?
                DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetVsyncMode((bool)config.GetValue("Setting", "VSync", false) ?
                DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
            Effect = (bool)config.GetValue("Setting", "Effect", true);
            SetLanguage((string)config.GetValue("Setting", "Language", FallbackLanguage));
            foreach (string action in InputMap.GetActions())
            {
                // exclude build-in
                if (action.StartsWith("ui") || !config.HasSectionKey("Control", action))
                    continue;
                InputMap.ActionEraseEvents(action);
                foreach (var input in (Array<InputEvent>)config.GetValue("Control", action, new Array<InputEvent>()))
                    InputMap.ActionAddEvent(action, input);
            }
        }
    }

    public async GDTask SaveConfig()
    {
        ConfigFile config = new();
        static int volume(float volumeDb) => (int)(Mathf.DbToLinear(volumeDb) * 100f);
        config.SetValue("Setting", "Music",
            volume(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))));
        config.SetValue("Setting", "Sound",
            volume(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Sound"))));
        config.SetValue("Setting", "Fullscreen",
            DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen);
        config.SetValue("Setting", "VSync",
            DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Enabled);
        config.SetValue("Setting", "Effect", Effect);
        config.SetValue("Setting", "Language", TranslationServer.GetLocale());
        foreach (string action in InputMap.GetActions())
        {
            // exclude build-in
            if (action.StartsWith("ui"))
                continue;
            config.SetValue("Control", action, InputMap.ActionGetEvents(action));
        }

        await GDTask.RunOnThreadPool(() =>
        {
            config.Save(GetConfigPath());
        });
    }
}