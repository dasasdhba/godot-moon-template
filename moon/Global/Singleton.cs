using Godot;

namespace Global;

public static class Singleton
{
    public static SingletonRoot Root { get ;set; }

    public static SaveSingleton Save
    {
        get => Root.GetNode<SaveSingleton>("Save");
        private set { }
    }

    public static MusicSingleton Music
    {
        get => Root.GetNode<MusicSingleton>("Music");
        private set { }
    }

    public static SceneSingleton Scene
    {
        get => Root.GetNode<SceneSingleton>("Scene");
        private set { }
    }
}