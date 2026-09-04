using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class LightningEffect : VFXEffect
{
    private Vector _endPoint;
    private double _strikeTimer;
    private double _strikeInterval;
    private int _segments;
    private double _jitter;
    private double _boltWidth;
    private Color _colorCore;
    private Color _colorGlow;
    private List<Point> _currentBolt = new();
    private double _boltLife;
    private double _flashIntensity;

    public LightningEffect(Vector start, Vector end, LightningPreset preset = LightningPreset.Standard) : base(EffectType.Lightning, start)
    {
        _endPoint = end;
        switch (preset)
        {
            case LightningPreset.Standard:
                _strikeInterval = 0.15;
                _segments = 8;
                _jitter = 20;
                _boltWidth = 3;
                _colorCore = Color.FromRgb(200, 220, 255);
                _colorGlow = Color.FromRgb(80, 100, 255);
                break;
            case LightningPreset.Heavy:
                _strikeInterval = 0.1;
                _segments = 12;
                _jitter = 35;
                _boltWidth = 5;
                _colorCore = Colors.White;
                _colorGlow = Color.FromRgb(100, 150, 255);
                break;
            case LightningPreset.Minor:
                _strikeInterval = 0.2;
                _segments = 5;
                _jitter = 12;
                _boltWidth = 2;
                _colorCore = Color.FromRgb(180, 200, 255);
                _colorGlow = Color.FromRgb(60, 80, 200);
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
        _strikeTimer += deltaTime;

        if (_flashIntensity > 0)
            _flashIntensity = Math.Max(0, _flashIntensity - deltaTime * 8);

        if (_boltLife > 0)
        {
            _boltLife -= deltaTime;
        }

        if (_strikeTimer >= _strikeInterval && IsLooping)
        {
            _strikeTimer = 0;
            GenerateBolt();
            _flashIntensity = 1.0;
            _boltLife = 0.12;
        }

        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            Particles[i].Update(deltaTime);
            if (!Particles[i].IsAlive)
                Particles.RemoveAt(i);
        }
    }

    private void GenerateBolt()
    {
        _currentBolt.Clear();
        _currentBolt.Add(new Point(Position.X, Position.Y));

        var dir = _endPoint - Position;
        var len = dir.Length;
        var perp = new Vector(-dir.Y, dir.X);
        if (perp.Length > 0) perp.Normalize();

        for (int i = 1; i < _segments; i++)
        {
            var t = (double)i / _segments;
            var basePoint = Position + dir * t;
            var offset = perp * RandomRange(-_jitter, _jitter);
            _currentBolt.Add(new Point(basePoint.X + offset.X, basePoint.Y + offset.Y));
        }

        _currentBolt.Add(new Point(_endPoint.X, _endPoint.Y));

        foreach (var pt in _currentBolt)
        {
            Particles.Add(new Particle(
                new Vector(pt.X, pt.Y),
                new Vector(RandomRange(-20, 20), RandomRange(-20, 20)),
                RandomRange(2, 6), 0.15, _colorCore));
        }
    }

    public override void Render(DrawingContext context)
    {
        if (_currentBolt.Count < 2 || _boltLife <= 0) return;

        var points = _currentBolt.ToArray();

        for (int pass = 0; pass < 3; pass++)
        {
            var width = pass switch
            {
                0 => _boltWidth * 4,
                1 => _boltWidth * 2,
                _ => _boltWidth
            };
            var color = pass switch
            {
                0 => Color.FromArgb((byte)(_flashIntensity * 60), _colorGlow.R, _colorGlow.G, _colorGlow.B),
                1 => Color.FromArgb((byte)(_flashIntensity * 150), _colorGlow.R, _colorGlow.G, _colorGlow.B),
                _ => Color.FromArgb((byte)(_flashIntensity * 255), _colorCore.R, _colorCore.G, _colorCore.B)
            };
            var brush = new SolidColorBrush(color);
            var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            for (int i = 0; i < points.Length - 1; i++)
            {
                context.DrawLine(pen, points[i], points[i + 1]);
            }
        }

        foreach (var p in Particles)
        {
            var alpha = p.LifeRatio * _flashIntensity;
            var brush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), _colorCore.R, _colorCore.G, _colorCore.B));
            context.DrawEllipse(brush, null, new Point(p.Position.X, p.Position.Y), p.Size / 2, p.Size / 2);
        }
    }
}

public enum LightningPreset
{
    Standard,
    Heavy,
    Minor
}
