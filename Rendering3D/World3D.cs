using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class World3D
{
    private static World3D? _instance;
    public static World3D Instance => _instance ??= new World3D();

    public double WorldWidth { get; set; } = 16;
    public double WorldDepth { get; set; } = 9;
    public double GroundY { get; set; } = 0;
    public double Scale { get; set; } = 100.0;

    private Scene3D? _scene;
    private Model3DGroup? _gridGroup;
    private Model3DGroup? _groundGroup;
    private readonly List<Model3DGroup> _decorationModels = new();

    private World3D() { }

    public void SetScene(Scene3D scene) => _scene = scene;

    public Point3D ToWorld3D(double gameX, double gameY, double elevation = 0)
    {
        double x = (gameX / Scale) - (WorldWidth / 2);
        double y = GroundY + elevation;
        double z = (gameY / Scale) - (WorldDepth / 2);
        return new Point3D(x, y, -z);
    }

    public Point3D ToWorld3D(System.Windows.Vector gamePos, double elevation = 0)
        => ToWorld3D(gamePos.X, gamePos.Y, elevation);

    public System.Windows.Vector ToGame2D(Point3D worldPos)
    {
        double gameX = (worldPos.X + WorldWidth / 2) * Scale;
        double gameY = (-worldPos.Z + WorldDepth / 2) * Scale;
        return new System.Windows.Vector(gameX, gameY);
    }

    public void BuildGround(double gameW, double gameH)
    {
        if (_scene == null) return;
        WorldWidth = gameW / Scale;
        WorldDepth = gameH / Scale;

        RemoveGround();

        var groundMesh = MeshFactory.CreatePlane(WorldWidth * 2, WorldDepth * 2);
        groundMesh.Color = Color.FromRgb(18, 18, 28);
        _groundGroup = new Model3DGroup();
        _groundGroup.Children.Add(groundMesh.ToGeometryModel3D());
        _groundGroup.Transform = new TranslateTransform3D(0, GroundY - 0.01, 0);
        _scene.SceneModels.Children.Add(_groundGroup);

        BuildGrid();
    }

    private void BuildGrid()
    {
        if (_scene == null) return;
        _gridGroup = new Model3DGroup();

        var gridMat = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(40, 0, 180, 255)));
        double step = 1.0;
        double halfW = WorldWidth;
        double halfD = WorldDepth;

        for (double x = -halfW; x <= halfW; x += step)
        {
            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection
                {
                    new(x, GroundY + 0.001, -halfD),
                    new(x + 0.02, GroundY + 0.001, -halfD),
                    new(x + 0.02, GroundY + 0.001, halfD),
                    new(x, GroundY + 0.001, halfD)
                },
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
            };
            _gridGroup.Children.Add(new GeometryModel3D(mesh, gridMat));
        }

        for (double z = -halfD; z <= halfD; z += step)
        {
            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection
                {
                    new(-halfW, GroundY + 0.001, z),
                    new(halfW, GroundY + 0.001, z),
                    new(halfW, GroundY + 0.001, z + 0.02),
                    new(-halfW, GroundY + 0.001, z + 0.02)
                },
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
            };
            _gridGroup.Children.Add(new GeometryModel3D(mesh, gridMat));
        }

        _scene.SceneModels.Children.Add(_gridGroup);
    }

    public void RemoveGround()
    {
        if (_scene == null) return;
        if (_groundGroup != null) { _scene.SceneModels.Children.Remove(_groundGroup); _groundGroup = null; }
        if (_gridGroup != null) { _scene.SceneModels.Children.Remove(_gridGroup); _gridGroup = null; }
        foreach (var m in _decorationModels) _scene.SceneModels.Children.Remove(m);
        _decorationModels.Clear();
    }

    public void AddWall(Point3D pos, double width, double height, double depth, Color color)
    {
        if (_scene == null) return;
        var mesh = MeshFactory.CreateCube(1);
        mesh.Color = color;
        var model = mesh.ToGeometryModel3D();
        var group = new Model3DGroup();
        group.Children.Add(model);
        group.Transform = new ScaleTransform3D(width, height, depth);
        var tf = new Transform3DGroup();
        tf.Children.Add(new ScaleTransform3D(width, height, depth));
        tf.Children.Add(new TranslateTransform3D(pos.X, pos.Y, pos.Z));
        group.Transform = tf;
        _scene.SceneModels.Children.Add(group);
        _decorationModels.Add(group);
    }

    public void AddPillar(Point3D pos, double radius, double height, Color color)
    {
        if (_scene == null) return;
        var mesh = MeshFactory.CreateCylinder(radius, height, 12);
        mesh.Color = color;
        var model = mesh.ToGeometryModel3D();
        var group = new Model3DGroup();
        group.Children.Add(model);
        group.Transform = new TranslateTransform3D(pos.X, pos.Y + height / 2, pos.Z);
        _scene.SceneModels.Children.Add(group);
        _decorationModels.Add(group);
    }
}
