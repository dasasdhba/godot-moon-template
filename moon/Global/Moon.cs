using Component;
using Godot;

namespace Global;

public partial class Moon : Node
{
    public Moon() : base()
    {
        Singleton = this;
    }
    
    public static Moon Singleton { get ;private set; }
    
    public static SaveSingleton Save 
        => Singleton.GetNode<SaveSingleton>("Save");

    public static MusicSingleton Music 
        => Singleton.GetNode<MusicSingleton>("Music");

    public static SceneSingleton Scene 
        => Singleton.GetNode<SceneSingleton>("Scene");
}