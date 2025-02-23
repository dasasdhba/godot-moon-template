using Godot;

namespace Global;

public partial class Moon : Node
{
    public Moon() : base()
    {
        Singleton = this;
        
        TreeEntered += () =>
        {
            Save = GetNode<SaveSingleton>("Save");
            Music = GetNode<MusicSingleton>("Music");
            Scene = GetNode<SceneSingleton>("Scene");
        };
    }
    
    public static Moon Singleton { get ;private set; }
    public static SaveSingleton Save { get; private set; }
    public static MusicSingleton Music { get; private set; }
    public static SceneSingleton Scene { get; private set; }
}