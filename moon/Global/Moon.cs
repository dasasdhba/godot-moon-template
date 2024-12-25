using Component;
using Godot;

namespace Global;

public partial class Moon : Node
{
    public Moon() : base()
    {
        Singleton = this;
    }
    
    public static Moon Singleton { get ;set; }
    
    public static SaveSingleton Save
    {
        get => Singleton.GetNode<SaveSingleton>("Save");
        private set { }
    }

    public static MusicSingleton Music
    {
        get => Singleton.GetNode<MusicSingleton>("Music");
        private set { }
    }

    public static SceneSingleton Scene
    {
        get => Singleton.GetNode<SceneSingleton>("Scene");
        private set { }
    }
}