using Godot;

namespace Global;

public static partial class Globalvar
{
    // example of a saved global variable
    
    public static int GlobalSavedInt
    {
        get => Moon.Save.GetItemValue("Global", "SavedInt", 0);
        set => Moon.Save.SetItemValue("Global", "SavedInt", value);
    }
}