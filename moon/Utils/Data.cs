#pragma warning disable CS0618 // Type or member is obsolete

using Godot;

namespace Utils;

// better metadata for godot objects

public static class Data
{
    public static void SetData(this GodotObject obj, string tag, Variant value)
        => obj.SetMeta(tag, value);

    public static void SetData(this GodotObject obj, Rid rid, string tag, Variant value)
    {
        if (obj is TileMap tilemap)
        {
            var layer = tilemap.GetLayerForBodyRid(rid);
            var coord = tilemap.GetCoordsForBodyRid(rid);
            var data = tilemap.GetCellTileData(layer, coord);
            data.SetCustomData(tag, value);
            return;
        }

        if (obj is TileMapLayer tilelayer)
        {
            var coord = tilelayer.GetCoordsForBodyRid(rid);
            var data = tilelayer.GetCellTileData(coord);
            data.SetCustomData(tag, value);
            return;
        }
        
        obj.Set(tag, value);
    }
    
    public static bool HasCustomData(this TileSet tileset, string tag)
    {
        for (int i = 0; i < tileset.GetCustomDataLayersCount(); i++)
        {
            if (tileset.GetCustomDataLayerName(i) == tag)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// If the obj is TileMap or TileMapLayer, this method checks custom layer data instead.
    /// To check the metadata, using HasMeta Instead.
    /// </summary>
    /// <returns></returns>
    public static bool HasData(this GodotObject obj, string tag)
    {
        if (obj is TileMap tilemap) return tilemap.TileSet.HasCustomData(tag);
        if (obj is TileMapLayer tilelayer) return tilelayer.TileSet.HasCustomData(tag);
        return obj.HasMeta(tag);
    }

    public static T GetData<[MustBeVariant] T>(this GodotObject obj, string tag, T defaultValue = default)
    {
        if (obj.HasMeta(tag))
            return obj.GetMeta(tag).As<T>();
            
        return defaultValue;
    }
    
    public static T GetData<[MustBeVariant] T>(this GodotObject obj, Rid rid, string tag, T defaultValue = default)
    {
        if (obj is TileMap tilemap)
        {
            if (!tilemap.TileSet.HasCustomData(tag)) return defaultValue;
            
            var layer = tilemap.GetLayerForBodyRid(rid);
            var coord = tilemap.GetCoordsForBodyRid(rid);
            var data = tilemap.GetCellTileData(layer, coord);
            return data.GetCustomData(tag).As<T>();
        }

        if (obj is TileMapLayer tilelayer)
        {
            if (!tilelayer.TileSet.HasCustomData(tag)) return defaultValue;
        
            var coord = tilelayer.GetCoordsForBodyRid(rid);
            var data = tilelayer.GetCellTileData(coord);
            return data.GetCustomData(tag).As<T>();
        }
        
        return obj.GetData(tag, defaultValue);
    }
}