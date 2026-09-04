using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class Scene3D
{
    public Viewport3D Viewport { get; }
    public PerspectiveCamera Camera { get; }
    public Model3DGroup SceneLights { get; }
    public Model3DGroup SceneModels { get; }
    public ModelVisual3D Visual { get; }

    private readonly Random _rng = new();

    public Scene3D(double width = 800, double height = 600)
    {
        Viewport = new Viewport3D();

        Camera = new PerspectiveCamera
        {
            Position = new Point3D(0, 5, 10),
            LookDirection = new Vector3D(0, -1, -3),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 45
        };
        Viewport.Camera = Camera;

        SceneLights = new Model3DGroup();
        SceneLights.Children.Add(new AmbientLight(Color.FromRgb(80, 80, 80)));
        SceneLights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -2, -1)));
        SceneLights.Children.Add(new DirectionalLight(Color.FromRgb(60, 60, 80), new Vector3D(1, -1, 1)));

        SceneModels = new Model3DGroup();

        Visual = new ModelVisual3D();
        Visual.Content = new Model3DGroup();
        ((Model3DGroup)Visual.Content).Children.Add(SceneLights);
        ((Model3DGroup)Visual.Content).Children.Add(SceneModels);

        Viewport.Children.Add(Visual);
    }

    public Model3DGroup AddMesh(MeshData mesh, Transform3DComponent? transform = null)
    {
        var model = mesh.ToGeometryModel3D();
        if (transform != null)
            model.Transform = transform.GetTransform();

        var group = new Model3DGroup();
        group.Children.Add(model);
        SceneModels.Children.Add(group);
        return group;
    }

    public Model3DGroup AddColoredMesh(MeshData mesh, Color color, Transform3DComponent? transform = null)
    {
        mesh.Color = color;
        return AddMesh(mesh, transform);
    }

    public void RemoveMesh(Model3DGroup model)
    {
        SceneModels.Children.Remove(model);
    }

    public void ClearScene()
    {
        SceneModels.Children.Clear();
    }

    public void SetCameraPosition(double x, double y, double z)
    {
        Camera.Position = new Point3D(x, y, z);
    }

    public void SetCameraLookAt(double x, double y, double z)
    {
        Camera.LookDirection = new Vector3D(x - Camera.Position.X, y - Camera.Position.Y, z - Camera.Position.Z);
    }

    public void AddPointLight(Color color, Point3D position, double range = 10)
    {
        var light = new PointLight(color, position);
        light.Range = range;
        SceneLights.Children.Add(light);
    }

    public void AddSpotLight(Color color, Point3D position, Vector3D direction, double angle = 45)
    {
        var light = new SpotLight(color, position, direction, angle, 1);
        SceneLights.Children.Add(light);
    }
}
