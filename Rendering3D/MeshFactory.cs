using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SimpleWPFGame.Rendering3D;

public static class MeshFactory
{
    public static MeshData CreateCube(double size = 1.0)
    {
        double h = size / 2;
        var positions = new Point3DCollection
        {
            // Front face
            new(-h, -h,  h), new( h, -h,  h), new( h,  h,  h), new(-h,  h,  h),
            // Back face
            new(-h, -h, -h), new(-h,  h, -h), new( h,  h, -h), new( h, -h, -h),
            // Top face
            new(-h,  h, -h), new(-h,  h,  h), new( h,  h,  h), new( h,  h, -h),
            // Bottom face
            new(-h, -h, -h), new( h, -h, -h), new( h, -h,  h), new(-h, -h,  h),
            // Right face
            new( h, -h, -h), new( h,  h, -h), new( h,  h,  h), new( h, -h,  h),
            // Left face
            new(-h, -h, -h), new(-h, -h,  h), new(-h,  h,  h), new(-h,  h, -h)
        };

        var indices = new Int32Collection
        {
            0,1,2, 0,2,3,       // front
            4,5,6, 4,6,7,       // back
            8,9,10, 8,10,11,    // top
            12,13,14, 12,14,15, // bottom
            16,17,18, 16,18,19, // right
            20,21,22, 20,22,23  // left
        };

        var normals = new Vector3DCollection
        {
            new(0,0,1), new(0,0,1), new(0,0,1), new(0,0,1),
            new(0,0,-1), new(0,0,-1), new(0,0,-1), new(0,0,-1),
            new(0,1,0), new(0,1,0), new(0,1,0), new(0,1,0),
            new(0,-1,0), new(0,-1,0), new(0,-1,0), new(0,-1,0),
            new(1,0,0), new(1,0,0), new(1,0,0), new(1,0,0),
            new(-1,0,0), new(-1,0,0), new(-1,0,0), new(-1,0,0)
        };

        var texCoords = new PointCollection
        {
            new(0,1), new(1,1), new(1,0), new(0,0),
            new(0,1), new(1,1), new(1,0), new(0,0),
            new(0,1), new(1,1), new(1,0), new(0,0),
            new(0,1), new(1,1), new(1,0), new(0,0),
            new(0,1), new(1,1), new(1,0), new(0,0),
            new(0,1), new(1,1), new(1,0), new(0,0)
        };

        return new MeshData(positions, indices) { Normals = normals, TextureCoordinates = texCoords };
    }

    public static MeshData CreateSphere(double radius = 0.5, int segments = 16)
    {
        var positions = new Point3DCollection();
        var indices = new Int32Collection();
        var normals = new Vector3DCollection();
        var texCoords = new PointCollection();

        for (int lat = 0; lat <= segments; lat++)
        {
            double theta = lat * Math.PI / segments;
            double sinTheta = Math.Sin(theta);
            double cosTheta = Math.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                double phi = lon * 2 * Math.PI / segments;
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);

                double x = cosPhi * sinTheta;
                double y = cosTheta;
                double z = sinPhi * sinTheta;

                positions.Add(new Point3D(x * radius, y * radius, z * radius));
                normals.Add(new Vector3D(x, y, z));
                texCoords.Add(new Point((double)lon / segments, (double)lat / segments));
            }
        }

        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);

                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return new MeshData(positions, indices) { Normals = normals, TextureCoordinates = texCoords };
    }

    public static MeshData CreateCylinder(double radius = 0.5, double height = 1.0, int segments = 16)
    {
        var positions = new Point3DCollection();
        var indices = new Int32Collection();
        var normals = new Vector3DCollection();
        var texCoords = new PointCollection();

        double halfH = height / 2;

        // Side vertices
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2 * Math.PI / segments;
            double x = Math.Cos(angle) * radius;
            double z = Math.Sin(angle) * radius;

            positions.Add(new Point3D(x, halfH, z));
            normals.Add(new Vector3D(Math.Cos(angle), 0, Math.Sin(angle)));
            texCoords.Add(new Point((double)i / segments, 0));

            positions.Add(new Point3D(x, -halfH, z));
            normals.Add(new Vector3D(Math.Cos(angle), 0, Math.Sin(angle)));
            texCoords.Add(new Point((double)i / segments, 1));
        }

        // Side indices
        for (int i = 0; i < segments; i++)
        {
            int top = i * 2;
            int bottom = i * 2 + 1;
            int nextTop = (i + 1) * 2;
            int nextBottom = (i + 1) * 2 + 1;

            indices.Add(top);
            indices.Add(nextTop);
            indices.Add(bottom);

            indices.Add(nextTop);
            indices.Add(nextBottom);
            indices.Add(bottom);
        }

        // Cap center
        int capCenter = positions.Count;
        positions.Add(new Point3D(0, halfH, 0));
        normals.Add(new Vector3D(0, 1, 0));
        texCoords.Add(new Point(0.5, 0.5));

        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2 * Math.PI / segments;
            positions.Add(new Point3D(Math.Cos(angle) * radius, halfH, Math.Sin(angle) * radius));
            normals.Add(new Vector3D(0, 1, 0));
            texCoords.Add(new Point(0.5 + Math.Cos(angle) * 0.5, 0.5 + Math.Sin(angle) * 0.5));
        }

        for (int i = 0; i < segments; i++)
        {
            indices.Add(capCenter);
            indices.Add(capCenter + 1 + i);
            indices.Add(capCenter + 1 + i + 1);
        }

        // Bottom cap
        int botCapCenter = positions.Count;
        positions.Add(new Point3D(0, -halfH, 0));
        normals.Add(new Vector3D(0, -1, 0));
        texCoords.Add(new Point(0.5, 0.5));

        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2 * Math.PI / segments;
            positions.Add(new Point3D(Math.Cos(angle) * radius, -halfH, Math.Sin(angle) * radius));
            normals.Add(new Vector3D(0, -1, 0));
            texCoords.Add(new Point(0.5 + Math.Cos(angle) * 0.5, 0.5 + Math.Sin(angle) * 0.5));
        }

        for (int i = 0; i < segments; i++)
        {
            indices.Add(botCapCenter);
            indices.Add(botCapCenter + 1 + i + 1);
            indices.Add(botCapCenter + 1 + i);
        }

        return new MeshData(positions, indices) { Normals = normals, TextureCoordinates = texCoords };
    }

    public static MeshData CreatePlane(double width = 1.0, double height = 1.0)
    {
        double hw = width / 2;
        double hh = height / 2;

        var positions = new Point3DCollection
        {
            new(-hw, 0, -hh),
            new( hw, 0, -hh),
            new( hw, 0,  hh),
            new(-hw, 0,  hh)
        };

        var indices = new Int32Collection { 0, 1, 2, 0, 2, 3 };
        var normals = new Vector3DCollection
        {
            new(0, 1, 0), new(0, 1, 0), new(0, 1, 0), new(0, 1, 0)
        };
        var texCoords = new PointCollection
        {
            new(0, 1), new(1, 1), new(1, 0), new(0, 0)
        };

        return new MeshData(positions, indices) { Normals = normals, TextureCoordinates = texCoords };
    }

    public static MeshData CreatePyramid(double size = 1.0)
    {
        double h = size / 2;
        double tip = size * 0.8;

        var positions = new Point3DCollection
        {
            // Base
            new(-h, 0, -h), new( h, 0, -h), new( h, 0,  h), new(-h, 0,  h),
            // Tip
            new(0, tip, 0)
        };

        var indices = new Int32Collection
        {
            0, 2, 1, 0, 3, 2, // base
            0, 1, 4, // front
            1, 2, 4, // right
            2, 3, 4, // back
            3, 0, 4  // left
        };

        var normals = new Vector3DCollection
        {
            new(0, -1, 0), new(0, -1, 0), new(0, -1, 0), new(0, -1, 0),
            new(0, 1, 0)
        };

        return new MeshData(positions, indices) { Normals = normals };
    }
}
