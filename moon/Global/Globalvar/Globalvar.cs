using Godot;

namespace Global;

public static partial class Globalvar
{
    // example of a saved global variable
    
    public static int GlobalSavedInt
    {
        get => (int)Singleton.Save.GetItemValue("Global", "SavedInt", 0);
        set => Singleton.Save.SetItemValue("Global", "SavedInt", value);
    }
}