#pragma warning disable CS0618 // Type or member is obsolete
namespace Godot;

public struct OverlapResult2D<T> where T : GodotObject
{
    public T Collider { get ;set; }
    public int Id { get; set; }
    public Rid Rid { get; set; }
    public int ShapeIndex { get ;set; }

    private static bool HasCustomData(TileSet tileset, string tag)
    {
        for (int i = 0; i < tileset.GetCustomDataLayersCount(); i++)
        {
            if (tileset.GetCustomDataLayerName(i) == tag)
                return true;
        }
        
        return false;
    }
    
    public bool HasData(string tag)
    {
        if (Collider is TileMap tilemap)
        {
            return HasCustomData(tilemap.TileSet, tag);
        }

        if (Collider is TileMapLayer tilelayer)
        {
            return HasCustomData(tilelayer.TileSet, tag);
        }
        
        return Collider.HasMeta(tag);
    }

    public Variant GetData(string tag, Variant defaultValue = default)
    {
        if (Collider is TileMap tilemap)
        {
            if (!HasCustomData(tilemap.TileSet, tag)) return defaultValue;
            
            var layer = tilemap.GetLayerForBodyRid(Rid);
            var coord = tilemap.GetCoordsForBodyRid(Rid);
            var data = tilemap.GetCellTileData(layer, coord);
            return data.GetCustomData(tag);
        }

        if (Collider is TileMapLayer tilelayer)
        {
            if (!HasCustomData(tilelayer.TileSet, tag)) return defaultValue;
        
            var coord = tilelayer.GetCoordsForBodyRid(Rid);
            var data = tilelayer.GetCellTileData(coord);
            return data.GetCustomData(tag);
        }
        
        return Collider.GetMeta(tag, defaultValue);
    }
}