using System;
using System.Collections.Generic;

public class NodeAStar
{
    public int x;
    public int y;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public NodeAStar parent;

    public NodeAStar(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class AStarManager
{
    private MapController mapController = new MapController();
    private float Heuristic(int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x1 - x2);
        int dy = Math.Abs(y1 - y2);
        return (float)(Math.Max(dx, dy) + (Math.Sqrt(2) - 1) * Math.Min(dx, dy));
    }

    private List<(int dx, int dy, float cost)> directions = new List<(int, int, float)>
    {
        (0, 1, 1f),
        (1, 0, 1f),
        (0, -1, 1f),
        (-1, 0, 1f),

        (1, 1, 1.4142f),
        (-1, 1, 1.4142f),
        (1, -1, 1.4142f),
        (-1, -1, 1.4142f)
    };

    public List<(int x, int y)> FindPath(MapData mapData, int startX, int startY, int endX, int endY)
    {
        var openSet = new List<NodeAStar>();
        var closedSet = new HashSet<(int, int)>();
        var nodes = new Dictionary<(int, int), NodeAStar>();

        NodeAStar startNode = new NodeAStar(startX, startY);
        NodeAStar endNode = new NodeAStar(endX, endY);

        openSet.Add(startNode);
        nodes[(startX, startY)] = startNode;

        while (openSet.Count > 0)
        {
            // lấy node có fCost nhỏ nhất
            NodeAStar current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost)
                    current = openSet[i];
            }

            openSet.Remove(current);
            closedSet.Add((current.x, current.y));

            // tới đích
            if (current.x == endX && current.y == endY)
            {
                return ReconstructPath(current);
            }

            foreach (var dir in directions)
            {
                int nx = current.x + dir.dx;
                int ny = current.y + dir.dy;

                if (closedSet.Contains((nx, ny)))
                    continue;

                // check walkable
                if (!mapController.IsWalkable(mapData, nx, ny))
                    continue;

                // chống cắt góc khi đi chéo
                if (dir.dx != 0 && dir.dy != 0)
                {
                    if (!mapController.IsWalkable(mapData, current.x + dir.dx, current.y) || !mapController.IsWalkable(mapData, current.x, current.y + dir.dy))
                        continue;
                }

                float newG = current.gCost + dir.cost;

                if (!nodes.TryGetValue((nx, ny), out NodeAStar neighbor))
                {
                    neighbor = new NodeAStar(nx, ny);
                    nodes[(nx, ny)] = neighbor;
                }

                if (!openSet.Contains(neighbor) || newG < neighbor.gCost)
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = Heuristic(nx, ny, endX, endY);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null; // không tìm được đường
    }
    public List<(int x, int y)> FindPath(MapData mapData, MoveArea moveArea, int startX, int startY, int endX, int endY)
    {
        var openSet = new List<NodeAStar>();
        var closedSet = new HashSet<(int, int)>();
        var nodes = new Dictionary<(int, int), NodeAStar>();

        NodeAStar startNode = new NodeAStar(startX, startY);
        NodeAStar endNode = new NodeAStar(endX, endY);

        openSet.Add(startNode);
        nodes[(startX, startY)] = startNode;

        while (openSet.Count > 0)
        {
            // lấy node có fCost nhỏ nhất
            NodeAStar current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost)
                    current = openSet[i];
            }

            openSet.Remove(current);
            closedSet.Add((current.x, current.y));

            // tới đích
            if (current.x == endX && current.y == endY)
            {
                return ReconstructPath(current);
            }

            foreach (var dir in directions)
            {
                int nx = current.x + dir.dx;
                int ny = current.y + dir.dy;

                if (closedSet.Contains((nx, ny)))
                    continue;

                // check move area
                if (nx < moveArea.minX || nx > moveArea.maxX || ny < moveArea.minY || ny > moveArea.maxY)
                    continue;

                // check walkable
                if (!mapController.IsWalkable(mapData, nx, ny))
                    continue;

                // chống cắt góc khi đi chéo
                if (dir.dx != 0 && dir.dy != 0)
                {
                    if (!mapController.IsWalkable(mapData, current.x + dir.dx, current.y) || !mapController.IsWalkable(mapData, current.x, current.y + dir.dy))
                        continue;
                }

                float newG = current.gCost + dir.cost;

                if (!nodes.TryGetValue((nx, ny), out NodeAStar neighbor))
                {
                    neighbor = new NodeAStar(nx, ny);
                    nodes[(nx, ny)] = neighbor;
                }

                if (!openSet.Contains(neighbor) || newG < neighbor.gCost)
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = Heuristic(nx, ny, endX, endY);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null; // không tìm được đường
    }

    private List<(int x, int y)> ReconstructPath(NodeAStar node)
    {
        var path = new List<(int, int)>();

        NodeAStar current = node;
        while (current != null)
        {
            path.Add((current.x, current.y));
            current = current.parent;
        }

        path.Reverse();
        return path;
    }
}