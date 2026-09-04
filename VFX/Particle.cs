using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public enum EffectType
{
    Fire,
    Lightning,
    Laser,
    Smoke,
    Cloud,
    Water,
    Fountain,
    Glow,
    Distortion
}

public class Particle
{
    public Vector Position { get; set; }
    public Vector Velocity { get; set; }
    public double Size { get; set; }
    public double SizeDecay { get; set; }
    public double Life { get; set; }
    public double MaxLife { get; set; }
    public Color Color { get; set; }
    public double Alpha { get; set; } = 1.0;
    public double Rotation { get; set; }
    public double RotationSpeed { get; set; }
    public bool IsAlive => Life > 0;
    public double LifeRatio => MaxLife > 0 ? Life / MaxLife : 0;

    public Particle(Vector pos, Vector vel, double size, double life, Color color)
    {
        Position = pos;
        Velocity = vel;
        Size = size;
        MaxLife = life;
        Life = life;
        Color = color;
    }

    public void Update(double dt)
    {
        Life -= dt;
        Position += Velocity * dt;
        Size = Math.Max(0, Size - SizeDecay * dt);
        Rotation += RotationSpeed * dt;
        Alpha = Math.Max(0, Life / MaxLife);
    }
}

public abstract class VFXEffect
{
    public EffectType Type { get; }
    public Vector Position { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLooping { get; set; } = true;
    public double Elapsed { get; protected set; }
    public List<Particle> Particles { get; } = new();

    protected Random _rng = new();

    protected VFXEffect(EffectType type, Vector position)
    {
        Type = type;
        Position = position;
    }

    public abstract void Update(double deltaTime);
    public abstract void Render(DrawingContext context);

    protected double RandomRange(double min, double max)
        => min + _rng.NextDouble() * (max - min);

    protected Color LerpColor(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    protected void RenderParticle(DrawingContext context, Particle p)
    {
        if (!p.IsAlive || p.Size < 0.5) return;

        var color = Color.FromArgb((byte)(p.Alpha * 255), p.Color.R, p.Color.G, p.Color.B);
        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, 0);

        var rect = new Rect(
            p.Position.X - p.Size / 2,
            p.Position.Y - p.Size / 2,
            p.Size, p.Size);

        if (Math.Abs(p.Rotation) > 0.01)
        {
            var center = p.Position;
            var group = new TransformGroup();
            group.Children.Add(new RotateTransform(p.Rotation, center.X, center.Y));
            context.PushTransform(group);
            context.DrawRectangle(brush, pen, rect);
            context.Pop();
        }
        else
        {
            context.DrawRectangle(brush, pen, rect);
        }
    }
}
