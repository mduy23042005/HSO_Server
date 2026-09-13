using System;
using System.Collections.Generic;
using System.IO;
using HSO_Server.Models;

public enum TileType
{
    None = -1,
    Ground = 0,
    Water = 1,
    Wall = 2,
    Decoration = 3,
    MapTransition = 4
}
public class MapData
{
    public Map map;
    public List<MobData> mobsData;
    public List<NPCData> npcsData;

    public int width;
    public int height;

    public int offsetX;
    public int offsetY;

    public byte[,] tiles;
}

public class MapController
{
    public void InitMap(MapData mapData)
    {
        string? path = AppContext.BaseDirectory;

        while (path != null)
        {
            // Kiểm tra xem thư mục Maps có tồn tại trong cấp hiện tại không
            if (Directory.Exists(Path.Combine(path, "Maps")))
            {
                path = Path.Combine(path, "Maps", $"{mapData.map.Idmap}.bin");
                break;
            }

            // Đi ngược lên thư mục cha
            if (Directory.GetParent(path) == null) 
                break;

            path = Directory.GetParent(path)?.FullName;
        }

        if (!File.Exists(path))
        {
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            mapData.width = reader.ReadInt32();
            mapData.height = reader.ReadInt32();

            mapData.offsetX = reader.ReadInt32();
            mapData.offsetY = reader.ReadInt32();

            mapData.tiles = new byte[mapData.width, mapData.height];

            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    mapData.tiles[x, y] = reader.ReadByte();
                }
            }
        }
    }

    public bool IsWalkable(MapData mapData, float worldX, float worldY)
    {
        if (mapData == null)
        {
            return false;
        }

        int x = (int)Math.Floor(worldX) - mapData.offsetX;
        int y = (int)Math.Floor(worldY) - mapData.offsetY;

        if (x < 0 || y < 0 || x >= mapData.width || y >= mapData.height)
            return false;

        return mapData.tiles[x, y] == (byte)TileType.Ground || mapData.tiles[x, y] == (byte)TileType.Water || mapData.tiles[x, y] == (byte)TileType.MapTransition;
    }

    public bool IsTransitionMap(MapData mapData, float worldX, float worldY)
    {
        if (mapData == null)
        {
            return false;
        }

        int x = (int)Math.Floor(worldX) - mapData.offsetX;
        int y = (int)Math.Floor(worldY) - mapData.offsetY;

        if (x < 0 || y < 0 || x >= mapData.width || y >= mapData.height)
            return false;

        return mapData.tiles[x, y] == (byte)TileType.MapTransition;
    }
}

