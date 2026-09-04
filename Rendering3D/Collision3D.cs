using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public struct AABB3D
{
    public Point3D Min;
    public Point3D Max;

    public Point3D Center => new((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2, (Min.Z + Max.Z) / 2);
    public Vector3D Size => new(Max.X - Min.X, Max.Y - Min.Y, Max.Z - Min.Z);
    public Vector3D Extents => Size * 0.5;

    public AABB3D(Point3D min, Point3D max)
    {
        Min = min;
        Max = max;
    }

    public AABB3D(Point3D center, double halfX, double halfY, double halfZ)
    {
        Min = new Point3D(center.X - halfX, center.Y - halfY, center.Z - halfZ);
        Max = new Point3D(center.X + halfX, center.Y + halfY, center.Z + halfZ);
    }

    public bool Intersects(in AABB3D other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
               Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    public double IntersectsDistance(in AABB3D other)
    {
        double dx = Math.Max(0, Math.Max(Min.X - other.Max.X, other.Min.X - Max.X));
        double dy = Math.Max(0, Math.Max(Min.Y - other.Max.Y, other.Min.Y - Max.Y));
        double dz = Math.Max(0, Math.Max(Min.Z - other.Max.Z, other.Min.Z - Max.Z));
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool ContainsPoint(Point3D point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public AABB3D Expanded(double amount)
    {
        return new AABB3D(
            new Point3D(Min.X - amount, Min.Y - amount, Min.Z - amount),
            new Point3D(Max.X + amount, Max.Y + amount, Max.Z + amount));
    }

    public AABB3D Merge(in AABB3D other)
    {
        return new AABB3D(
            new Point3D(Math.Min(Min.X, other.Min.X), Math.Min(Min.Y, other.Min.Y), Math.Min(Min.Z, other.Min.Z)),
            new Point3D(Math.Max(Max.X, other.Max.X), Math.Max(Max.Y, other.Max.Y), Math.Max(Max.Z, other.Max.Z)));
    }
}

public struct Sphere3D
{
    public Point3D Center;
    public double Radius;

    public Sphere3D(Point3D center, double radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool Intersects(in Sphere3D other)
    {
        double dx = Center.X - other.Center.X;
        double dy = Center.Y - other.Center.Y;
        double dz = Center.Z - other.Center.Z;
        double distSq = dx * dx + dy * dy + dz * dz;
        double radiiSum = Radius + other.Radius;
        return distSq <= radiiSum * radiiSum;
    }

    public bool IntersectsAABB(in AABB3D box)
    {
        double closestX = Math.Clamp(Center.X, box.Min.X, box.Max.X);
        double closestY = Math.Clamp(Center.Y, box.Min.Y, box.Max.Y);
        double closestZ = Math.Clamp(Center.Z, box.Min.Z, box.Max.Z);

        double dx = Center.X - closestX;
        double dy = Center.Y - closestY;
        double dz = Center.Z - closestZ;

        return (dx * dx + dy * dy + dz * dz) <= (Radius * Radius);
    }
}

public struct Ray3D
{
    public Point3D Origin;
    public Vector3D Direction;
    public double MaxDistance;

    public Ray3D(Point3D origin, Vector3D direction, double maxDistance = 1000)
    {
        Origin = origin;
        Direction = direction;
        MaxDistance = maxDistance;
    }

    public bool IntersectsAABB(in AABB3D box, out double t)
    {
        t = 0;
        double tmin = double.MinValue;
        double tmax = double.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            double origin = i switch { 0 => Origin.X, 1 => Origin.Y, _ => Origin.Z };
            double dir = i switch { 0 => Direction.X, 1 => Direction.Y, _ => Direction.Z };
            double min = i switch { 0 => box.Min.X, 1 => box.Min.Y, _ => box.Min.Z };
            double max = i switch { 0 => box.Max.X, 1 => box.Max.Y, _ => box.Max.Z };

            if (Math.Abs(dir) < 1e-8)
            {
                if (origin < min || origin > max) return false;
            }
            else
            {
                double t1 = (min - origin) / dir;
                double t2 = (max - origin) / dir;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tmin = Math.Max(tmin, t1);
                tmax = Math.Min(tmax, t2);
                if (tmin > tmax) return false;
            }
        }

        t = tmin >= 0 ? tmin : tmax;
        return t >= 0 && t <= MaxDistance;
    }
}

public struct SweepResult3D
{
    public bool Hit;
    public double TimeOfImpact;
    public Point3D ContactPoint;
    public Vector3D ContactNormal;
    public int HitEntityId;
}

public class CollisionBody3D
{
    public int EntityId { get; set; }
    public AABB3D Bounds { get; set; }
    public Vector3D Velocity { get; set; }
    public bool IsStatic { get; set; }
    public bool IsActive { get; set; } = true;
    public object? Owner { get; set; }

    public AABB3D GetSweptBounds(double dt)
    {
        var move = Velocity * dt;
        var nextMin = new Point3D(Bounds.Min.X + Math.Min(0, move.X), Bounds.Min.Y + Math.Min(0, move.Y), Bounds.Min.Z + Math.Min(0, move.Z));
        var nextMax = new Point3D(Bounds.Max.X + Math.Max(0, move.X), Bounds.Max.Y + Math.Max(0, move.Y), Bounds.Max.Z + Math.Max(0, move.Z));
        return new AABB3D(
            new Point3D(Math.Min(Bounds.Min.X, nextMin.X), Math.Min(Bounds.Min.Y, nextMin.Y), Math.Min(Bounds.Min.Z, nextMin.Z)),
            new Point3D(Math.Max(Bounds.Max.X, nextMax.X), Math.Max(Bounds.Max.Y, nextMax.Y), Math.Max(Bounds.Max.Z, nextMax.Z)));
    }
}

public static class CollisionMath3D
{
    public static bool SweepAABB(in AABB3D moving, Vector3D velocity, in AABB3D stationary, double maxTime, out SweepResult3D result)
    {
        result = default;
        if (Math.Abs(velocity.X) < 1e-8 && Math.Abs(velocity.Y) < 1e-8 && Math.Abs(velocity.Z) < 1e-8)
        {
            result.Hit = moving.Intersects(stationary);
            if (result.Hit)
            {
                result.TimeOfImpact = 0;
                result.ContactPoint = moving.Center;
                result.ContactNormal = new Vector3D(0, 1, 0);
            }
            return result.Hit;
        }

        double tFirst = 0;
        double tLast = maxTime;
        Vector3D normal = default;

        for (int i = 0; i < 3; i++)
        {
            double v = i switch { 0 => velocity.X, 1 => velocity.Y, _ => velocity.Z };
            double bmin = i switch { 0 => moving.Min.X, 1 => moving.Min.Y, _ => moving.Min.Z };
            double bmax = i switch { 0 => moving.Max.X, 1 => moving.Max.Y, _ => moving.Max.Z };
            double smin = i switch { 0 => stationary.Min.X, 1 => stationary.Min.Y, _ => stationary.Min.Z };
            double smax = i switch { 0 => stationary.Max.X, 1 => stationary.Max.Y, _ => stationary.Max.Z };

            if (v < 0)
            {
                if (bmax < smin) return false;
                double tEntry = (smax - bmin) / v;
                double tExit = (smin - bmax) / v;
                if (tEntry > tFirst) { tFirst = tEntry; normal = default; normal = i switch { 0 => new Vector3D(1, 0, 0), 1 => new Vector3D(0, 1, 0), _ => new Vector3D(0, 0, 1) }; }
                tLast = Math.Min(tLast, tExit);
            }
            else if (v > 0)
            {
                if (bmin > smax) return false;
                double tEntry = (smin - bmax) / v;
                double tExit = (smax - bmin) / v;
                if (tEntry > tFirst) { tFirst = tEntry; normal = default; normal = i switch { 0 => new Vector3D(-1, 0, 0), 1 => new Vector3D(0, -1, 0), _ => new Vector3D(0, 0, -1) }; }
                tLast = Math.Min(tLast, tExit);
            }
            else
            {
                if (bmin > smax || bmax < smin) return false;
            }

            if (tFirst > tLast) return false;
        }

        if (tFirst > maxTime) return false;

        result.Hit = true;
        result.TimeOfImpact = Math.Max(0, tFirst);
        result.ContactNormal = normal;
        result.ContactPoint = new Point3D(
            moving.Center.X + velocity.X * result.TimeOfImpact,
            moving.Center.Y + velocity.Y * result.TimeOfImpact,
            moving.Center.Z + velocity.Z * result.TimeOfImpact);
        return true;
    }

    public static Point3D ClosestPointOnAABB(Point3D point, in AABB3D box)
    {
        return new Point3D(
            Math.Clamp(point.X, box.Min.X, box.Max.X),
            Math.Clamp(point.Y, box.Min.Y, box.Max.Y),
            Math.Clamp(point.Z, box.Min.Z, box.Max.Z));
    }

    public static double DistanceSq(Point3D a, Point3D b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
