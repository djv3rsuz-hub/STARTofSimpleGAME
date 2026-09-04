using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class WaterEffect : VFXEffect
{
    private double _emitTimer;
    private double _emitRate;
    private double _gravity;
    private double _baseSpeed;
    private double _baseSize;
    private double _baseLife;
    private Color _colorTop;
    private Color _colorBottom;
    private double _splashRadius;

    public WaterEffect(Vector position, WaterPreset preset = WaterPreset.Standard) : base(EffectType.Water, position)
    {
        _gravity = 400;
        switch (preset)
        {
            case WaterPreset.Standard:
                _emitRate = 0.03;
                _baseSpeed = 120;
                _baseSize = 6;
                _baseLife = 1.0;
                _colorTop = Color.FromRgb(100, 180, 255);
                _colorBottom = Color.FromRgb(40, 100, 200);
                _splashRadius = 20;
                break;
            case WaterPreset.Fountain:
                _emitRate = 0.015;
                _baseSpeed = 200;
                _baseSize = 4;
                _baseLife = 1.2;
                _colorTop = Color.FromRgb(150, 220, 255);
                _colorBottom = Color.FromRgb(60, 120, 220);
                _splashRadius = 30;
                break;
            case WaterPreset.Drip:
                _emitRate = 0.15;
                _baseSpeed = 40;
                _baseSize = 5;
                _baseLife = 0.6;
                _colorTop = Color.FromRgb(120, 200, 255);
                _colorBottom = Color.FromRgb(50, 110, 210);
                _splashRadius = 10;
                break;
            case WaterPreset.Ocean:
                _emitRate = 0.01;
                _baseSpeed = 80;
                _baseSize = 8;
                _baseLife = 1.5;
                _colorTop = Color.FromRgb(80, 160, 240);
                _colorBottom = Color.FromRgb(20, 60, 140);
                _splashRadius = 40;
                break;
        }
    }

    public override void Update(double deltaTime)
    {
        Elapsed += deltaTime;
        _emitTimer += deltaTime;

        while (_emitTimer >= _emitRate)
        {
            _emitTimer -= _emitRate;
            EmitParticle();
        }

        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            p.Velocity += new Vector(0, _gravity * deltaTime);
            p.Update(deltaTime);

            if (p.Position.Y > Position.Y + _splashRadius * 2)
            {
                for (int s = 0; s < 2; s++)
                {
                    var splashVel = new Vector(RandomRange(-30, 30), RandomRange(-60, -20));
                    Particles.Add(new Particle(
                        new Vector(p.Position.X, Position.Y + _splashRadius),
                        splashVel, p.Size * 0.4, 0.3, _colorBottom));
                }
                Particles.RemoveAt(i);
            }
            else if (!p.IsAlive)
            {
                Particles.RemoveAt(i);
            }
        }
    }

    private void EmitParticle()
    {
        var angle = RandomRange(-60, 60) * Math.PI / 180 - Math.PI / 2;
        var speed = _baseSpeed * RandomRange(0.7, 1.3);
        var vel = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
        var pos = Position + new Vector(RandomRange(-3, 3), RandomRange(-2, 2));
        var size = _baseSize * RandomRange(0.5, 1.5);
        var life = _baseLife * RandomRange(0.7, 1.3);

        Particles.Add(new Particle(pos, vel, size, life, _colorTop));
    }

    public override void Render(DrawingContext context)
    {
        foreach (var p in Particles)
        {
            var t = 1 - p.LifeRatio;
            var color = LerpColor(_colorTop, _colorBottom, t);
            var alpha = Math.Min(1, p.LifeRatio * 1.5);
            var finalColor = Color.FromArgb((byte)(alpha * 200), color.R, color.G, color.B);
            var brush = new SolidColorBrush(finalColor);

            var rect = new Rect(
                p.Position.X - p.Size / 2,
                p.Position.Y - p.Size / 2,
                p.Size, p.Size);

            context.DrawRectangle(brush, null, rect);
        }
    }
}

public enum WaterPreset
{
    Standard,
    Fountain,
    Drip,
    Ocean
}
