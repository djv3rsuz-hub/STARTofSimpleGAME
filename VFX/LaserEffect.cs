using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class LaserEffect : VFXEffect
{
    private Vector _endPoint;
    private double _width;
    private double _pulseSpeed;
    private double _pulseAmount;
    private Color _colorCore;
    private Color _colorGlow;
    private double _beamLife;
    private double _flicker;
    private double _glowSize;

    public LaserEffect(Vector start, Vector end, LaserPreset preset = LaserPreset.Standard) : base(EffectType.Laser, start)
    {
        _endPoint = end;
        switch (preset)
        {
            case LaserPreset.Standard:
                _width = 4;
                _pulseSpeed = 8;
                _pulseAmount = 0.3;
                _colorCore = Color.FromRgb(255, 50, 50);
                _colorGlow = Color.FromRgb(255, 100, 100);
                _glowSize = 12;
                break;
            case LaserPreset.Heavy:
                _width = 8;
                _pulseSpeed = 6;
                _pulseAmount = 0.4;
                _colorCore = Color.FromRgb(50, 255, 50);
                _colorGlow = Color.FromRgb(100, 255, 100);
                _glowSize = 20;
                break;
            case LaserPreset.Beam:
                _width = 12;
                _pulseSpeed = 4;
                _pulseAmount = 0.2;
                _colorCore = Colors.White;
                _colorGlow = Color.FromRgb(100, 150, 255);
                _glowSize = 30;
                break;
        }
    }

    public void SetEndpoints(Vector start, Vector end)
    {
        Position = start;
        _endPoint = end;
    }

    public override void Update(double deltaTime)
    {
        Elapsed += deltaTime;

        _flicker = 0.8 + Math.Sin(Elapsed * _pulseSpeed) * _pulseAmount;

        _beamLife += deltaTime;

        if (IsLooping)
        {
            var dir = _endPoint - Position;
            var len = dir.Length;
            if (len > 0)
            {
                dir.Normalize();
                var perp = new Vector(-dir.Y, dir.X);

                int sparkCount = 3;
                for (int i = 0; i < sparkCount; i++)
                {
                    var t = RandomRange(0.1, 0.9);
                    var basePos = Position + (_endPoint - Position) * t;
                    var offset = perp * RandomRange(-_glowSize, _glowSize);
                    Particles.Add(new Particle(
                        basePos + offset,
                        new Vector(RandomRange(-30, 30), RandomRange(-30, 30)),
                        RandomRange(1, 4), 0.1, _colorCore));
                }
            }
        }

        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            Particles[i].Update(deltaTime);
            if (!Particles[i].IsAlive)
                Particles.RemoveAt(i);
        }
    }

    public override void Render(DrawingContext context)
    {
        var dir = _endPoint - Position;
        var len = dir.Length;
        if (len < 1) return;
        dir.Normalize();
        var perp = new Vector(-dir.Y, dir.X);

        for (int pass = 0; pass < 3; pass++)
        {
            var width = pass switch
            {
                0 => _glowSize * 2 * _flicker,
                1 => _width * 3 * _flicker,
                _ => _width
            };
            var alpha = pass switch
            {
                0 => 40,
                1 => 120,
                _ => 255
            };
            var color = pass == 2 ? _colorCore : _colorGlow;
            var brush = new SolidColorBrush(Color.FromArgb((byte)(alpha * _flicker), color.R, color.G, color.B));
            var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            context.DrawLine(pen, new Point(Position.X, Position.Y), new Point(_endPoint.X, _endPoint.Y));
        }

        var coreBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * _flicker), 255, 255, 255));
        var corePen = new Pen(coreBrush, _width * 0.4);
        context.DrawLine(corePen, new Point(Position.X, Position.Y), new Point(_endPoint.X, _endPoint.Y));

        foreach (var p in Particles)
        {
            var alpha = p.LifeRatio * 200;
            var brush = new SolidColorBrush(Color.FromArgb((byte)alpha, _colorCore.R, _colorCore.G, _colorCore.B));
            context.DrawEllipse(brush, null, new Point(p.Position.X, p.Position.Y), p.Size, p.Size);
        }
    }
}

public enum LaserPreset
{
    Standard,
    Heavy,
    Beam
}
