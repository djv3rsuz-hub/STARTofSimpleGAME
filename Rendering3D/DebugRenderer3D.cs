using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class DebugRenderer3D
{
    private static DebugRenderer3D? _instance;
    public static DebugRenderer3D Instance => _instance ??= new DebugRenderer3D();

    private Scene3D? _scene;
    private readonly Dictionary<int, Model3DGroup> _hitboxModels = new();
    private readonly Dictionary<int, Model3DGroup> _boundsModels = new();
    private Model3DGroup? _spatialHashOverlay;
    private bool _showHitboxes;
    private bool _showBounds;
    private bool _showSpatialHash;

    private DebugRenderer3D() { }

    public void SetScene(Scene3D scene) => _scene = scene;

    public void ToggleHitboxes()
    {
        _showHitboxes = !_showHitboxes;
        if (!_showHitboxes) ClearHitboxes();
    }

    public void ToggleBounds()
    {
        _showBounds = !_showBounds;
        if (!_showBounds) ClearBounds();
    }

    public void ToggleSpatialHash()
    {
        _showSpatialHash = !_showSpatialHash;
        if (!_showSpatialHash) ClearSpatialHash();
    }

    public void UpdateHitboxVisualization(int entityId, List<Hitbox3D> hitboxes)
    {
        if (_scene == null || !_showHitboxes) return;
        ClearHitboxModel(entityId);

        var group = new Model3DGroup();
        foreach (var hb in hitboxes)
        {
            Color color = hb.IsActive ? Color.FromArgb(80, 255, 0, 0) : Color.FromArgb(40, 255, 255, 0);
            var mesh = CreateWireframeBox(hb.Bounds, color);
            group.Children.Add(mesh);
        }

        if (group.Children.Count > 0)
        {
            _scene.SceneModels.Children.Add(group);
            _hitboxModels[entityId] = group;
        }
    }

    public void UpdateBoundsVisualization(int entityId, AABB3D bounds)
    {
        if (_scene == null || !_showBounds) return;
        ClearBoundsModel(entityId);

        var mesh = CreateWireframeBox(bounds, Color.FromArgb(60, 0, 255, 0));
        _scene.SceneModels.Children.Add(mesh);
        _boundsModels[entityId] = mesh;
    }

    public void UpdateSpatialHashVisualization(SpatialHashGrid3D grid, Point3D worldMin, Point3D worldMax)
    {
        if (_scene == null || !_showSpatialHash) return;
        ClearSpatialHash();

        _spatialHashOverlay = new Model3DGroup();
        var mat = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(20, 0, 100, 255)));

        double cellSize = 1.5;
        for (double x = worldMin.X; x <= worldMax.X; x += cellSize)
        {
            for (double z = worldMin.Z; z <= worldMax.Z; z += cellSize)
            {
                var mesh = new MeshGeometry3D
                {
                    Positions = new Point3DCollection
                    {
                        new(x, 0.01, z),
                        new(x + cellSize, 0.01, z),
                        new(x + cellSize, 0.01, z + cellSize),
                        new(x, 0.01, z + cellSize)
                    },
                    TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
                };
                _spatialHashOverlay.Children.Add(new GeometryModel3D(mesh, mat));
            }
        }

        _scene.SceneModels.Children.Add(_spatialHashOverlay);
    }

    private Model3DGroup CreateWireframeBox(AABB3D box, Color color)
    {
        var group = new Model3DGroup();
        var size = box.Size;
        var center = box.Center;

        var mesh = MeshFactory.CreateCube(1);
        mesh.Color = color;
        var model = mesh.ToGeometryModel3D();
        model.Material = new DiffuseMaterial(new SolidColorBrush(color) { Opacity = 0.3 });

        var tf = new Transform3DGroup();
        tf.Children.Add(new ScaleTransform3D(size.X, size.Y, size.Z));
        tf.Children.Add(new TranslateTransform3D(center.X, center.Y, center.Z));
        model.Transform = tf;
        group.Children.Add(model);

        return group;
    }

    public void ClearHitboxModel(int entityId)
    {
        if (_hitboxModels.TryGetValue(entityId, out var model))
        {
            _scene?.SceneModels.Children.Remove(model);
            _hitboxModels.Remove(entityId);
        }
    }

    public void ClearBoundsModel(int entityId)
    {
        if (_boundsModels.TryGetValue(entityId, out var model))
        {
            _scene?.SceneModels.Children.Remove(model);
            _boundsModels.Remove(entityId);
        }
    }

    private void ClearHitboxes()
    {
        foreach (var model in _hitboxModels.Values)
            _scene?.SceneModels.Children.Remove(model);
        _hitboxModels.Clear();
    }

    private void ClearBounds()
    {
        foreach (var model in _boundsModels.Values)
            _scene?.SceneModels.Children.Remove(model);
        _boundsModels.Clear();
    }

    private void ClearSpatialHash()
    {
        if (_spatialHashOverlay != null)
        {
            _scene?.SceneModels.Children.Remove(_spatialHashOverlay);
            _spatialHashOverlay = null;
        }
    }

    public void ClearAll()
    {
        ClearHitboxes();
        ClearBounds();
        ClearSpatialHash();
    }
}
