using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public class MeshData
{
    public Point3DCollection Positions { get; set; } = new();
    public Int32Collection TriangleIndices { get; set; } = new();
    public Vector3DCollection Normals { get; set; } = new();
    public PointCollection TextureCoordinates { get; set; } = new();
    public Color Color { get; set; } = Colors.White;
    public double Opacity { get; set; } = 1.0;
    public double SpecularPower { get; set; } = 32.0;
    public Color EmissiveColor { get; set; } = Colors.Black;

    public MeshData() { }

    public MeshData(Point3DCollection positions, Int32Collection indices)
    {
        Positions = positions;
        TriangleIndices = indices;
    }

    public GeometryModel3D ToGeometryModel3D()
    {
        var mesh = new MeshGeometry3D
        {
            Positions = Positions,
            TriangleIndices = TriangleIndices,
            Normals = Normals,
            TextureCoordinates = TextureCoordinates
        };

        var materialGroup = new MaterialGroup();
        materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color)));
        
        if (SpecularPower > 0)
            materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), SpecularPower));
        
        if (EmissiveColor != Colors.Black)
            materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(EmissiveColor)));

        if (Opacity < 1.0)
        {
            var brush = new SolidColorBrush(Color) { Opacity = Opacity };
            materialGroup.Children.Clear();
            materialGroup.Children.Add(new DiffuseMaterial(brush));
            if (SpecularPower > 0)
                materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), SpecularPower));
        }

        var model = new GeometryModel3D(mesh, materialGroup)
        {
            BackMaterial = materialGroup
        };

        return model;
    }
}

public enum PrimitiveType
{
    Cube,
    Sphere,
    Cylinder,
    Plane,
    Pyramid
}

public class Transform3DComponent
{
    public Point3D Position { get; set; }
    public Vector3D Rotation { get; set; }
    public Vector3D Scale { get; set; } = new(1, 1, 1);

    public System.Windows.Media.Media3D.Transform3D GetTransform()
    {
        var group = new Transform3DGroup();
        group.Children.Add(new ScaleTransform3D(Scale.X, Scale.Y, Scale.Z));

        if (Math.Abs(Rotation.X) > 0.001)
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), Rotation.X)));
        if (Math.Abs(Rotation.Y) > 0.001)
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), Rotation.Y)));
        if (Math.Abs(Rotation.Z) > 0.001)
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), Rotation.Z)));

        group.Children.Add(new TranslateTransform3D(Position.X, Position.Y, Position.Z));
        return group;
    }
}
