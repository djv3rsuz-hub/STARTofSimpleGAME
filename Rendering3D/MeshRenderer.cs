using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class MeshObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public MeshData MeshData { get; set; }
    public Transform3DComponent Transform { get; set; } = new();
    public Model3DGroup? ModelGroup { get; set; }
    public bool IsVisible { get; set; } = true;
    public double lifetime { get; set; } = -1;
    public double Age { get; set; }

    public MeshObject(MeshData mesh, Transform3DComponent? transform = null)
    {
        MeshData = mesh;
        if (transform != null) Transform = transform;
    }
}

public class MeshRenderer
{
    private static MeshRenderer? _instance;
    public static MeshRenderer Instance => _instance ??= new MeshRenderer();

    private readonly Dictionary<string, MeshObject> _objects = new();
    private Scene3D? _scene;

    public int ObjectCount => _objects.Count;

    private MeshRenderer() { }

    public void SetScene(Scene3D scene) => _scene = scene;

    public MeshObject AddCube(double size, Color color, Point3D position)
    {
        var mesh = MeshFactory.CreateCube(size);
        mesh.Color = color;
        var obj = new MeshObject(mesh, new Transform3DComponent { Position = position });

        if (_scene != null)
        {
            obj.ModelGroup = _scene.AddMesh(mesh, obj.Transform);
        }

        _objects[obj.Id] = obj;
        return obj;
    }

    public MeshObject AddSphere(double radius, Color color, Point3D position, int segments = 16)
    {
        var mesh = MeshFactory.CreateSphere(radius, segments);
        mesh.Color = color;
        var obj = new MeshObject(mesh, new Transform3DComponent { Position = position });

        if (_scene != null)
            obj.ModelGroup = _scene.AddMesh(mesh, obj.Transform);

        _objects[obj.Id] = obj;
        return obj;
    }

    public MeshObject AddCylinder(double radius, double height, Color color, Point3D position)
    {
        var mesh = MeshFactory.CreateCylinder(radius, height);
        mesh.Color = color;
        var obj = new MeshObject(mesh, new Transform3DComponent { Position = position });

        if (_scene != null)
            obj.ModelGroup = _scene.AddMesh(mesh, obj.Transform);

        _objects[obj.Id] = obj;
        return obj;
    }

    public MeshObject AddPlane(double width, double height, Color color, Point3D position)
    {
        var mesh = MeshFactory.CreatePlane(width, height);
        mesh.Color = color;
        var obj = new MeshObject(mesh, new Transform3DComponent { Position = position });

        if (_scene != null)
            obj.ModelGroup = _scene.AddMesh(mesh, obj.Transform);

        _objects[obj.Id] = obj;
        return obj;
    }

    public MeshObject AddPyramid(double size, Color color, Point3D position)
    {
        var mesh = MeshFactory.CreatePyramid(size);
        mesh.Color = color;
        var obj = new MeshObject(mesh, new Transform3DComponent { Position = position });

        if (_scene != null)
            obj.ModelGroup = _scene.AddMesh(mesh, obj.Transform);

        _objects[obj.Id] = obj;
        return obj;
    }

    public MeshObject? GetObject(string id)
    {
        _objects.TryGetValue(id, out var obj);
        return obj;
    }

    public void RemoveObject(string id)
    {
        if (_objects.TryGetValue(id, out var obj))
        {
            if (_scene != null && obj.ModelGroup != null)
                _scene.RemoveMesh(obj.ModelGroup);
            _objects.Remove(id);
        }
    }

    public void ClearAll()
    {
        if (_scene != null)
        {
            foreach (var obj in _objects.Values)
            {
                if (obj.ModelGroup != null)
                    _scene.RemoveMesh(obj.ModelGroup);
            }
        }
        _objects.Clear();
    }

    public void Update(double deltaTime)
    {
        foreach (var obj in _objects.Values)
        {
            if (obj.lifetime > 0)
            {
                obj.Age += deltaTime;
                if (obj.Age >= obj.lifetime)
                {
                    obj.IsVisible = false;
                }
            }

            if (obj.ModelGroup != null)
            {
                obj.ModelGroup.Transform = obj.Transform.GetTransform();
            }
        }

        var expired = _objects.Where(kv => kv.Value.lifetime > 0 && kv.Value.Age >= kv.Value.lifetime).Select(kv => kv.Key).ToList();
        foreach (var id in expired)
            RemoveObject(id);
    }
}
