using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public struct Hitbox3D
{
    public AABB3D Bounds;
    public int ActiveFrameStart;
    public int ActiveFrameEnd;
    public int CurrentFrame;
    public double Damage;
    public double KnockbackForce;
    public Vector3D KnockbackDirection;
    public bool IsActive => CurrentFrame >= ActiveFrameStart && CurrentFrame <= ActiveFrameEnd;
    public bool Expired => CurrentFrame > ActiveFrameEnd;

    public Hitbox3D(AABB3D bounds, int startFrame, int endFrame, double damage = 0, double knockback = 0)
    {
        Bounds = bounds;
        ActiveFrameStart = startFrame;
        ActiveFrameEnd = endFrame;
        CurrentFrame = 0;
        Damage = damage;
        KnockbackForce = knockback;
        KnockbackDirection = default;
    }

    public void Advance() => CurrentFrame++;

    public bool CheckOverlap(in Hitbox3D other)
    {
        if (!IsActive || !other.IsActive) return false;
        return Bounds.Intersects(other.Bounds);
    }

    public bool CheckOverlap(in AABB3D box)
    {
        if (!IsActive) return false;
        return Bounds.Intersects(box);
    }

    public SweepResult3D CheckSweep(Vector3D velocity, in AABB3D target, double maxTime)
    {
        if (!IsActive) return default;
        return CollisionMath3D.SweepAABB(Bounds, velocity, target, maxTime, out var result) ? result : default;
    }
}

public class Hitbox3DManager
{
    private static Hitbox3DManager? _instance;
    public static Hitbox3DManager Instance => _instance ??= new Hitbox3DManager();

    private readonly Dictionary<int, List<Hitbox3D>> _entityHitboxes = new();
    private readonly Dictionary<int, AABB3D> _entityBounds = new();
    private readonly Dictionary<int, HashSet<int>> _hitRecords = new();
    private readonly SpatialHashGrid3D _spatialHash = new(1.5);

    private Hitbox3DManager() { }

    public void RegisterEntity(int entityId, AABB3D bounds)
    {
        _entityBounds[entityId] = bounds;
        if (!_entityHitboxes.ContainsKey(entityId))
            _entityHitboxes[entityId] = new List<Hitbox3D>();
        if (!_hitRecords.ContainsKey(entityId))
            _hitRecords[entityId] = new HashSet<int>();
    }

    public void UnregisterEntity(int entityId)
    {
        _entityBounds.Remove(entityId);
        _entityHitboxes.Remove(entityId);
        _hitRecords.Remove(entityId);
    }

    public void UpdateBounds(int entityId, AABB3D bounds)
    {
        _entityBounds[entityId] = bounds;
    }

    public void SetHitboxes(int entityId, List<Hitbox3D> hitboxes)
    {
        _entityHitboxes[entityId] = hitboxes;
    }

    public void ClearHitRecords(int entityId)
    {
        if (_hitRecords.TryGetValue(entityId, out var records))
            records.Clear();
    }

    public bool HasHitTarget(int attackerId, int targetId)
    {
        return _hitRecords.TryGetValue(attackerId, out var records) && records.Contains(targetId);
    }

    public void RegisterHit(int attackerId, int targetId)
    {
        if (_hitRecords.TryGetValue(attackerId, out var records))
            records.Add(targetId);
    }

    public void AdvanceAllFrames()
    {
        foreach (var hitboxes in _entityHitboxes.Values)
        {
            for (int i = 0; i < hitboxes.Count; i++)
            {
                var h = hitboxes[i];
                h.Advance();
                hitboxes[i] = h;
            }
        }
    }

    public void RebuildSpatialHash()
    {
        if (_entityBounds.Count == 0) return;

        Point3D worldMin = new(double.MaxValue, double.MaxValue, double.MaxValue);
        Point3D worldMax = new(double.MinValue, double.MinValue, double.MinValue);

        foreach (var bounds in _entityBounds.Values)
        {
            worldMin = new Point3D(Math.Min(worldMin.X, bounds.Min.X), Math.Min(worldMin.Y, bounds.Min.Y), Math.Min(worldMin.Z, bounds.Min.Z));
            worldMax = new Point3D(Math.Max(worldMax.X, bounds.Max.X), Math.Max(worldMax.Y, bounds.Max.Y), Math.Max(worldMax.Z, bounds.Max.Z));
        }

        worldMin = new Point3D(worldMin.X - 2, worldMin.Y - 2, worldMin.Z - 2);
        worldMax = new Point3D(worldMax.X + 2, worldMax.Y + 2, worldMax.Z + 2);

        var bodies = new List<CollisionBody3D>();
        foreach (var kv in _entityBounds)
        {
            bodies.Add(new CollisionBody3D
            {
                EntityId = kv.Key,
                Bounds = kv.Value,
                IsActive = true
            });
        }
        _spatialHash.Rebuild(bodies, worldMin, worldMax);
    }

    public List<(int attackerId, int targetId, Hitbox3D hitbox)> DetectHits()
    {
        var hits = new List<(int, int, Hitbox3D)>();

        foreach (var kv in _entityHitboxes)
        {
            int attackerId = kv.Key;
            var hitboxes = kv.Value;

            foreach (var hitbox in hitboxes)
            {
                if (!hitbox.IsActive) continue;

                var queryBox = hitbox.Bounds.Expanded(0.5);
                var nearby = _spatialHash.QueryAABB(queryBox);

                foreach (var nearbyBody in nearby)
                {
                    int targetId = nearbyBody.EntityId;
                    if (targetId == attackerId) continue;
                    if (HasHitTarget(attackerId, targetId)) continue;

                    if (_entityBounds.TryGetValue(targetId, out var targetBounds))
                    {
                        if (hitbox.Bounds.Intersects(targetBounds))
                        {
                            hits.Add((attackerId, targetId, hitbox));
                        }
                    }
                }
            }
        }

        return hits;
    }

    public List<(int entityId, AABB3D bounds)> GetAllEntities()
    {
        var result = new List<(int, AABB3D)>();
        foreach (var kv in _entityBounds)
            result.Add((kv.Key, kv.Value));
        return result;
    }

    public int ActiveHitboxCount
    {
        get
        {
            int count = 0;
            foreach (var hitboxes in _entityHitboxes.Values)
                foreach (var h in hitboxes)
                    if (h.IsActive) count++;
            return count;
        }
    }
}
