using Godot;

namespace Global;

/// <summary>
/// ControllerIcons is powered by gdscript.
/// Here we provide some csharp access.
/// </summary>
public static class ControllerIcons
{
    public static Node Singleton
    {
        get => Moon.Singleton.GetNode("/root/ControllerIcons");
        private set { }
    }

    private const string TexturePath = "res://addons/controller_icons/objects/ControllerIconTexture.gd";
    private const string AssetRoot = "res://addons/controller_icons/assets/";
        
    public static Texture2D NewCtrlTexture()
        => (Texture2D)GD.Load<GDScript>(TexturePath).New();
            
    public static void CtrlTextureSetPath(Texture2D texture, string path)
        => texture.Set("path", path);
            
    public static void CtrlTextureSetEvent(Texture2D texture, InputEvent @event)
        => CtrlTextureSetPath(texture, ConvertEventToPath(@event));
        
    public static string ConvertEventToPath(InputEvent @event)
        => (string)Singleton.Call("_convert_event_to_path", @event);
            
    public static string ConvertPathToAsset(string path)
        => AssetRoot + (string)Singleton.Call("_convert_path_to_asset_file", path) + ".png";
            
    public static string ConvertEventToAsset(InputEvent @event)
        => ConvertPathToAsset(ConvertEventToPath(@event));
}