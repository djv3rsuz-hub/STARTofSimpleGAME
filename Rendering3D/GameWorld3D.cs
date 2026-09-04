using System.Windows.Media;
using System.Windows.Media.Media3D;
using SimpleWPFGame.Game;
using SimpleWPFGame.Combat;

namespace SimpleWPFGame.Rendering3D;

public class GameWorld3D
{
    private static GameWorld3D? _instance;
    public static GameWorld3D Instance => _instance ??= new GameWorld3D();

    private Scene3D? _scene;
    private bool _initialized;
    private readonly Dictionary<int, double> _entityHP = new();
    private readonly Dictionary<int, double> _entityMaxHP = new();
    private readonly Dictionary<int, string> _entityNames = new();
    private readonly Dictionary<int, Color> _entityColors = new();
    private readonly CombatFrameTimer _combatTimer = new(60);
    private double _trailTimer;

    public bool IsInitialized => _initialized;
    public Scene3D? Scene => _scene;

    private GameWorld3D() { }

    public void Initialize(Scene3D scene, double gameW, double gameH)
    {
        _scene = scene;
        MeshRenderer.Instance.SetScene(scene);
        MeshSync3D.Instance.SetScene(scene);
        World3D.Instance.SetScene(scene);
        World3D.Instance.BuildGround(gameW, gameH);
        _initialized = true;

        AddArenaDecorations();
    }

    private void AddArenaDecorations()
    {
        var w = World3D.Instance;
        double hw = w.WorldWidth;
        double hd = w.WorldDepth;

        Color wallColor = Color.FromRgb(30, 40, 60);
        double wallH = 1.5;

        w.AddWall(new Point3D(0, wallH / 2, -hd), hw * 2, wallH, 0.2, wallColor);
        w.AddWall(new Point3D(0, wallH / 2, hd), hw * 2, wallH, 0.2, wallColor);
        w.AddWall(new Point3D(-hw, wallH / 2, 0), 0.2, wallH, hd * 2, wallColor);
        w.AddWall(new Point3D(hw, wallH / 2, 0), 0.2, wallH, hd * 2, wallColor);

        Color pillarColor = Color.FromRgb(50, 60, 80);
        double pillarR = 0.15, pillarH = 2.0;
        w.AddPillar(new Point3D(-hw * 0.7, 0, -hd * 0.7), pillarR, pillarH, pillarColor);
        w.AddPillar(new Point3D(hw * 0.7, 0, -hd * 0.7), pillarR, pillarH, pillarColor);
        w.AddPillar(new Point3D(-hw * 0.7, 0, hd * 0.7), pillarR, pillarH, pillarColor);
        w.AddPillar(new Point3D(hw * 0.7, 0, hd * 0.7), pillarR, pillarH, pillarColor);

        Color centerColor = Color.FromRgb(0, 100, 150);
        w.AddPillar(new Point3D(0, 0, 0), 0.25, 0.3, centerColor);
    }

    public void RegisterCube(Cube cube, int id, Color color, string name)
    {
        if (!_initialized) return;

        var size = cube.Width;
        var mesh = MeshFactory.CreateCube(size / World3D.Instance.Scale);
        mesh.Color = color;

        var meshObj = new MeshObject(mesh, new Transform3DComponent());
        var pos = World3D.Instance.ToWorld3D(cube.Position.X + size / 2, cube.Position.Y + size / 2, size / (2 * World3D.Instance.Scale));
        meshObj.Transform.Position = pos;
        meshObj.Transform.Scale = new Vector3D(size / World3D.Instance.Scale, size / World3D.Instance.Scale, size / World3D.Instance.Scale);

        MeshRenderer.Instance.AddCube(size / World3D.Instance.Scale, color, pos);
        MeshSync3D.Instance.RegisterEntity(id, meshObj);

        _entityColors[id] = color;
        _entityNames[id] = name;

        if (cube.Stats != null)
        {
            _entityHP[id] = cube.Stats.HP;
            _entityMaxHP[id] = cube.Stats.MaxHP;
            MeshSync3D.Instance.CreateHealthBar(id, cube.Position.X, cube.Position.Y, size, 8);
            MeshSync3D.Instance.CreateNamePlate(id, name, cube.Position.X, cube.Position.Y);
        }

        var bounds = new AABB3D(
            new Point3D(pos.X - size / (2 * World3D.Instance.Scale), 0, pos.Z - size / (2 * World3D.Instance.Scale)),
            new Point3D(pos.X + size / (2 * World3D.Instance.Scale), size / World3D.Instance.Scale, pos.Z + size / (2 * World3D.Instance.Scale)));
        Hitbox3DManager.Instance.RegisterEntity(id, bounds);
    }

    public void Update(double deltaTime, IEnumerable<Cube> cubes)
    {
        if (!_initialized) return;

        _combatTimer.Advance(deltaTime);
        Hitbox3DManager.Instance.RebuildSpatialHash();

        foreach (var cube in cubes)
        {
            if (!cube.IsActive) continue;
            int id = cube.GetHashCode();
            if (!_entityColors.ContainsKey(id)) continue;

            double size = cube.Width;
            bool facingLeft = cube.Velocity.X < -0.1 || (cube.Velocity.X == 0 && Math.Cos(cube.FacingAngle) < 0);
            MeshSync3D.Instance.UpdateEntityPosition(id, cube.Position.X, cube.Position.Y, size, _entityColors[id], facingLeft);

            if (cube.Stats != null)
            {
                double hp = Math.Max(0, Math.Min(cube.Stats.HP, cube.Stats.MaxHP));
                _entityHP[id] = hp;
                _entityMaxHP[id] = cube.Stats.MaxHP;
                MeshSync3D.Instance.UpdateHealthBar(id, cube.Position.X, cube.Position.Y, size, hp / cube.Stats.MaxHP);
                MeshSync3D.Instance.UpdateNamePlate(id, cube.Position.X, cube.Position.Y);
            }

            var meshPos = World3D.Instance.ToWorld3D(cube.Position.X + size / 2, cube.Position.Y + size / 2, size / (2 * World3D.Instance.Scale));
            double half = size / (2 * World3D.Instance.Scale);
            var bounds = new AABB3D(
                new Point3D(meshPos.X - half, 0, meshPos.Z - half),
                new Point3D(meshPos.X + half, size / World3D.Instance.Scale, meshPos.Z + half));
            Hitbox3DManager.Instance.UpdateBounds(id, bounds);
        }

        UpdateCombatEffects(deltaTime, cubes);
    }

    private void UpdateCombatEffects(double deltaTime, IEnumerable<Cube> cubes)
    {
        _trailTimer += deltaTime;
        if (_trailTimer < 0.03) return;
        _trailTimer = 0;

        foreach (var cube in cubes)
        {
            if (!cube.IsActive) continue;
            int id = cube.GetHashCode();
            if (!cube.Combat.IsAttacking && cube.Combat.State != CombatState.Blocking && cube.Combat.State != CombatState.Parrying)
            {
                MeshSync3D.Instance.RemoveWeaponTrail(id);
                continue;
            }

            if (cube.Combat.IsAttacking && cube.Combat.EquippedWeapon != null)
            {
                double size = cube.Width;
                double centerX = cube.Position.X + size / 2;
                double centerY = cube.Position.Y + size / 2;
                bool left = Math.Cos(cube.FacingAngle) < 0;

                double slashLen = size * 1.2;
                double startX = centerX + (left ? -size * 0.3 : size * 0.3);
                double endX = centerX + (left ? -slashLen : slashLen);
                double arcY = centerY - size * 0.2;

                var start = World3D.Instance.ToWorld3D(startX, centerY, size / (2 * World3D.Instance.Scale));
                var end = World3D.Instance.ToWorld3D(endX, arcY, size / (2 * World3D.Instance.Scale));
                MeshSync3D.Instance.ShowWeaponTrail(id, start, end, Colors.White, 0.04);
            }
            else
            {
                MeshSync3D.Instance.RemoveWeaponTrail(id);
            }
        }
    }

    public void SyncCombatHitboxes(Cube cube, int entityId)
    {
        if (cube.Combat.CurrentHitboxes.Length == 0)
        {
            Hitbox3DManager.Instance.SetHitboxes(entityId, new List<Hitbox3D>());
            return;
        }

        var hitbox3Ds = new List<Hitbox3D>();
        double size = cube.Width;
        double scale = World3D.Instance.Scale;

        foreach (var hb2D in cube.Combat.CurrentHitboxes)
        {
            var hbBounds2D = hb2D.Bounds;
            double hbCenterX = hbBounds2D.X + hbBounds2D.Width / 2;
            double hbCenterY = hbBounds2D.Y + hbBounds2D.Height / 2;

            var center3D = World3D.Instance.ToWorld3D(hbCenterX, hbCenterY, size / (2 * scale));
            double halfW = hbBounds2D.Width / (2 * scale);
            double halfH = hbBounds2D.Height / (2 * scale);
            double halfD = size / (2 * scale);

            var box3D = new AABB3D(center3D, halfW, halfH, halfD);
            var hitbox3D = new Hitbox3D(box3D, hb2D.ActiveFrameStart, hb2D.ActiveFrameEnd, 0, hb2D.KnockbackForce);
            hitbox3Ds.Add(hitbox3D);
        }

        Hitbox3DManager.Instance.SetHitboxes(entityId, hitbox3Ds);
    }

    public List<(int attackerId, int targetId, double damage, bool crit, Vector3D knockback)> ProcessCombat3D()
    {
        var results = new List<(int, int, double, bool, Vector3D)>();
        var hits = Hitbox3DManager.Instance.DetectHits();

        foreach (var (attackerId, targetId, hitbox) in hits)
        {
            Hitbox3DManager.Instance.RegisterHit(attackerId, targetId);

            double damage = 0.1;
            bool crit = false;
            var knockback = new Vector3D(hitbox.KnockbackForce, 0, 0);

            results.Add((attackerId, targetId, damage, crit, knockback));
        }

        return results;
    }

    public void Cleanup()
    {
        MeshSync3D.Instance.ClearAll();
        Hitbox3DManager.Instance.UnregisterEntity(0);
        _entityHP.Clear();
        _entityMaxHP.Clear();
        _entityNames.Clear();
        _entityColors.Clear();
    }
}
