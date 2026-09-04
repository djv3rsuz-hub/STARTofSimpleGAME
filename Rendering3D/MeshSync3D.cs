using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class MeshSync3D
{
    private static MeshSync3D? _instance;
    public static MeshSync3D Instance => _instance ??= new MeshSync3D();

    private readonly Dictionary<int, MeshObject> _entityMeshes = new();
    private readonly Dictionary<int, Model3DGroup> _healthBarModels = new();
    private readonly Dictionary<int, Model3DGroup> _namePlateModels = new();
    private readonly Dictionary<int, Model3DGroup> _weaponTrailModels = new();
    private Scene3D? _scene;

    private MeshSync3D() { }

    public void SetScene(Scene3D scene) => _scene = scene;

    public void RegisterEntity(int entityId, MeshObject mesh)
    {
        _entityMeshes[entityId] = mesh;
    }

    public void UnregisterEntity(int entityId)
    {
        if (_entityMeshes.TryGetValue(entityId, out var mesh))
        {
            MeshRenderer.Instance.RemoveObject(mesh.Id);
            _entityMeshes.Remove(entityId);
        }
        RemoveHealthBar(entityId);
        RemoveNamePlate(entityId);
        RemoveWeaponTrail(entityId);
    }

    public void UpdateEntityPosition(int entityId, double gameX, double gameY, double size, Color color, bool facingLeft)
    {
        if (!_entityMeshes.TryGetValue(entityId, out var mesh)) return;

        double halfSize = size / (2.0 * World3D.Instance.Scale);
        var pos = World3D.Instance.ToWorld3D(gameX + size / 2, gameY + size / 2, halfSize);
        mesh.Transform.Position = pos;

        double scaleX = facingLeft ? -1 : 1;
        mesh.Transform.Scale = new Vector3D(halfSize * 2 * scaleX, halfSize * 2, halfSize * 2);
    }

    public void CreateHealthBar(int entityId, double gameX, double gameY, double width, double height)
    {
        if (_scene == null) return;
        RemoveHealthBar(entityId);

        var group = new Model3DGroup();

        var bgMesh = MeshFactory.CreatePlane(width / 100.0, 0.08);
        bgMesh.Color = Color.FromRgb(40, 40, 40);
        var bgModel = bgMesh.ToGeometryModel3D();
        group.Children.Add(bgModel);

        var fillMesh = MeshFactory.CreatePlane(width / 100.0, 0.06);
        fillMesh.Color = Colors.LimeGreen;
        var fillModel = fillMesh.ToGeometryModel3D();
        group.Children.Add(fillModel);

        var pos = World3D.Instance.ToWorld3D(gameX + width / 2, gameY, height / 100.0 + 0.3);
        group.Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z);

        _scene.SceneModels.Children.Add(group);
        _healthBarModels[entityId] = group;
    }

    public void UpdateHealthBar(int entityId, double gameX, double gameY, double width, double hpPercent)
    {
        if (!_healthBarModels.TryGetValue(entityId, out var group)) return;
        if (group.Children.Count < 2) return;

        var fillModel = (GeometryModel3D)group.Children[1];
        if (fillModel.Geometry is MeshGeometry3D fillMesh)
        {
            double fillWidth = (width / 100.0) * Math.Max(0, Math.Min(1, hpPercent));
            double offsetX = -(width / 100.0 - fillWidth) / 2;

            fillMesh.Positions.Clear();
            fillMesh.Positions.Add(new Point3D(-fillWidth / 2 + offsetX, 0, -0.03));
            fillMesh.Positions.Add(new Point3D(fillWidth / 2 + offsetX, 0, -0.03));
            fillMesh.Positions.Add(new Point3D(fillWidth / 2 + offsetX, 0, 0.03));
            fillMesh.Positions.Add(new Point3D(-fillWidth / 2 + offsetX, 0, 0.03));

            Color barColor;
            if (hpPercent > 0.6) barColor = Colors.LimeGreen;
            else if (hpPercent > 0.3) barColor = Colors.Yellow;
            else barColor = Colors.Red;

            fillModel.Material = new DiffuseMaterial(new SolidColorBrush(barColor));
        }

        var pos = World3D.Instance.ToWorld3D(gameX + width / 2, gameY, 0.9);
        group.Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z);
    }

    public void RemoveHealthBar(int entityId)
    {
        if (_healthBarModels.TryGetValue(entityId, out var group))
        {
            _scene?.SceneModels.Children.Remove(group);
            _healthBarModels.Remove(entityId);
        }
    }

    public void CreateNamePlate(int entityId, string name, double gameX, double gameY)
    {
        if (_scene == null) return;
        RemoveNamePlate(entityId);

        var group = new Model3DGroup();

        var bgMesh = MeshFactory.CreatePlane(name.Length * 0.12 + 0.2, 0.15);
        bgMesh.Color = Color.FromArgb(120, 0, 0, 0);
        var bgModel = bgMesh.ToGeometryModel3D();
        bgModel.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)));
        group.Children.Add(bgModel);

        var pos = World3D.Instance.ToWorld3D(gameX + 30, gameY - 10, 1.0);
        group.Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z);

        _scene.SceneModels.Children.Add(group);
        _namePlateModels[entityId] = group;
    }

    public void UpdateNamePlate(int entityId, double gameX, double gameY)
    {
        if (!_namePlateModels.TryGetValue(entityId, out var group)) return;
        var pos = World3D.Instance.ToWorld3D(gameX + 30, gameY - 10, 1.0);
        group.Transform = new TranslateTransform3D(pos.X, pos.Y, pos.Z);
    }

    public void RemoveNamePlate(int entityId)
    {
        if (_namePlateModels.TryGetValue(entityId, out var group))
        {
            _scene?.SceneModels.Children.Remove(group);
            _namePlateModels.Remove(entityId);
        }
    }

    public void ShowWeaponTrail(int entityId, Point3D start, Point3D end, Color color, double width = 0.05)
    {
        if (_scene == null) return;
        RemoveWeaponTrail(entityId);

        var group = new Model3DGroup();
        var dir = end - start;
        double length = dir.Length;
        if (length < 0.01) return;

        var mesh = MeshFactory.CreatePlane(length, width);
        mesh.Color = color;
        var model = mesh.ToGeometryModel3D();
        group.Children.Add(model);

        var mid = new Point3D((start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2);
        group.Transform = new TranslateTransform3D(mid.X, mid.Y, mid.Z);

        _scene.SceneModels.Children.Add(group);
        _weaponTrailModels[entityId] = group;
    }

    public void RemoveWeaponTrail(int entityId)
    {
        if (_weaponTrailModels.TryGetValue(entityId, out var group))
        {
            _scene?.SceneModels.Children.Remove(group);
            _weaponTrailModels.Remove(entityId);
        }
    }

    public void ClearAll()
    {
        foreach (var mesh in _entityMeshes.Values)
            MeshRenderer.Instance.RemoveObject(mesh.Id);
        _entityMeshes.Clear();

        foreach (var group in _healthBarModels.Values)
            _scene?.SceneModels.Children.Remove(group);
        _healthBarModels.Clear();

        foreach (var group in _namePlateModels.Values)
            _scene?.SceneModels.Children.Remove(group);
        _namePlateModels.Clear();

        foreach (var group in _weaponTrailModels.Values)
            _scene?.SceneModels.Children.Remove(group);
        _weaponTrailModels.Clear();
    }
}
