using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class SpatialHashGrid3D
{
    private readonly Dictionary<long, List<CollisionBody3D>> _cells = new();
    private double _cellSize;
    private int _cellsX, _cellsY, _cellsZ;
    private Point3D _worldMin;

    public int CellCount => _cells.Count;
    public int TotalObjects { get; private set; }

    public SpatialHashGrid3D(double cellSize = 2.0)
    {
        _cellSize = cellSize;
    }

    public void Clear()
    {
        _cells.Clear();
        TotalObjects = 0;
    }

    public void Rebuild(IEnumerable<CollisionBody3D> bodies, Point3D worldMin, Point3D worldMax)
    {
        Clear();
        _worldMin = worldMin;
        _cellsX = (int)Math.Ceiling((worldMax.X - worldMin.X) / _cellSize) + 1;
        _cellsY = (int)Math.Ceiling((worldMax.Y - worldMin.Y) / _cellSize) + 1;
        _cellsZ = (int)Math.Ceiling((worldMax.Z - worldMin.Z) / _cellSize) + 1;

        foreach (var body in bodies)
        {
            if (!body.IsActive) continue;
            Insert(body);
        }
    }

    public void Insert(CollisionBody3D body)
    {
        int minCX = GetCellX(body.Bounds.Min.X);
        int minCY = GetCellY(body.Bounds.Min.Y);
        int minCZ = GetCellZ(body.Bounds.Min.Z);
        int maxCX = GetCellX(body.Bounds.Max.X);
        int maxCY = GetCellY(body.Bounds.Max.Y);
        int maxCZ = GetCellZ(body.Bounds.Max.Z);

        for (int x = minCX; x <= maxCX; x++)
        {
            for (int y = minCY; y <= maxCY; y++)
            {
                for (int z = minCZ; z <= maxCZ; z++)
                {
                    long key = GetKey(x, y, z);
                    if (!_cells.TryGetValue(key, out var list))
                    {
                        list = new List<CollisionBody3D>();
                        _cells[key] = list;
                    }
                    list.Add(body);
                }
            }
        }
        TotalObjects++;
    }

    public void QueryPotentialCollisions(CollisionBody3D body, List<CollisionBody3D> results)
    {
        results.Clear();
        int minCX = GetCellX(body.Bounds.Min.X);
        int minCY = GetCellY(body.Bounds.Min.Y);
        int minCZ = GetCellZ(body.Bounds.Min.Z);
        int maxCX = GetCellX(body.Bounds.Max.X);
        int maxCY = GetCellY(body.Bounds.Max.Y);
        int maxCZ = GetCellZ(body.Bounds.Max.Z);

        var seen = new HashSet<int>();
        for (int x = minCX; x <= maxCX; x++)
        {
            for (int y = minCY; y <= maxCY; y++)
            {
                for (int z = minCZ; z <= maxCZ; z++)
                {
                    long key = GetKey(x, y, z);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        foreach (var other in list)
                        {
                            if (other.EntityId == body.EntityId) continue;
                            if (!other.IsActive) continue;
                            if (seen.Add(other.EntityId))
                                results.Add(other);
                        }
                    }
                }
            }
        }
    }

    public List<CollisionBody3D> QueryAABB(AABB3D box)
    {
        var results = new List<CollisionBody3D>();
        int minCX = GetCellX(box.Min.X);
        int minCY = GetCellY(box.Min.Y);
        int minCZ = GetCellZ(box.Min.Z);
        int maxCX = GetCellX(box.Max.X);
        int maxCY = GetCellY(box.Max.Y);
        int maxCZ = GetCellZ(box.Max.Z);

        var seen = new HashSet<int>();
        for (int x = minCX; x <= maxCX; x++)
        {
            for (int y = minCY; y <= maxCY; y++)
            {
                for (int z = minCZ; z <= maxCZ; z++)
                {
                    long key = GetKey(x, y, z);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        foreach (var other in list)
                        {
                            if (!other.IsActive) continue;
                            if (seen.Add(other.EntityId) && other.Bounds.Intersects(box))
                                results.Add(other);
                        }
                    }
                }
            }
        }
        return results;
    }

    private int GetCellX(double x) => Math.Max(0, Math.Min(_cellsX - 1, (int)((x - _worldMin.X) / _cellSize)));
    private int GetCellY(double y) => Math.Max(0, Math.Min(_cellsY - 1, (int)((y - _worldMin.Y) / _cellSize)));
    private int GetCellZ(double z) => Math.Max(0, Math.Min(_cellsZ - 1, (int)((z - _worldMin.Z) / _cellSize)));

    private long GetKey(int x, int y, int z)
    {
        return ((long)x * 73856093) ^ ((long)y * 19349669) ^ ((long)z * 83492791);
    }
}
