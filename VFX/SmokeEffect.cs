using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class SmokeEffect : VFXEffect
{
    private double _emitTimer;
    private double _emitRate;
    private double _spread;
    private double _baseSpeed;
    private double _baseSize;
    private double _baseLife;
    private Color _colorStart;
    private Color _colorEnd;
    private double _windX;

    public SmokeEffect(Vector position, SmokePreset preset = SmokePreset.Standard) : base(EffectType.Smoke, position)
    {
        switch (preset)
        {
            case SmokePreset.Standard:
                _emitRate = 0.06;
                _spread = 30;
                _baseSpeed = 25;
                _baseSize = 20;
                _baseLife = 1.5;
                _colorStart = Color.FromRgb(80, 80, 80);
                _colorEnd = Color.FromRgb(40, 40, 40);
                break;
            case SmokePreset.Cloud:
                _emitRate = 0.04;
                _spread = 50;
                _baseSpeed = 15;
                _baseSize = 40;
                _baseLife = 2.5;
                _colorStart = Color.FromRgb(200, 200, 210);
                _colorEnd = Color.FromRgb(120, 120, 130);
                break;
            case SmokePreset.Dark:
                _emitRate = 0.05;
                _spread = 25;
                _baseSpeed = 20;
                _baseSize = 15;
                _baseLife = 2.0;
                _colorStart = Color.FromRgb(40, 40, 40);
                _colorEnd = Color.FromRgb(15, 15, 15);
                break;
            case SmokePreset.Steam:
                _emitRate = 0.03;
                _spread = 40;
                _baseSpeed = 30;
                _baseSize = 12;
                _baseLife = 1.0;
                _colorStart = Color.FromRgb(220, 220, 230);
                _colorEnd = Color.FromRgb(180, 180, 190);
                break;
        }
    }

    public void SetWind(double windX) => _windX = windX;

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
            Particles[i].Velocity += new Vector(_windX * deltaTime * 10, 0);
            Particles[i].Update(deltaTime);
            Particles[i].Size += deltaTime * 5;
            if (!Particles[i].IsAlive)
                Particles.RemoveAt(i);
        }
    }

    private void EmitParticle()
    {
        var angle = Math.PI / 2 + RandomRange(-_spread, _spread) * Math.PI / 180;
        var speed = _baseSpeed * RandomRange(0.5, 1.5);
        var vel = new Vector(Math.Cos(angle) * speed, -Math.Abs(Math.Sin(angle) * speed));
        var pos = Position + new Vector(RandomRange(-6, 6), RandomRange(-4, 4));
        var size = _baseSize * RandomRange(0.5, 1.5);
        var life = _baseLife * RandomRange(0.6, 1.4);

        Particles.Add(new Particle(pos, vel, size, life, _colorStart)
        {
            SizeDecay = -size / life * 0.3,
            RotationSpeed = RandomRange(-30, 30)
        });
    }

    public override void Render(DrawingContext context)
    {
        foreach (var p in Particles)
        {
            var t = 1 - p.LifeRatio;
            var color = LerpColor(_colorStart, _colorEnd, t);
            var alpha = p.LifeRatio * 0.6;
            var finalColor = Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B);
            var brush = new SolidColorBrush(finalColor);

            var rect = new Rect(
                p.Position.X - p.Size / 2,
                p.Position.Y - p.Size / 2,
                p.Size, p.Size);

            context.DrawRectangle(brush, null, rect);
        }
    }
}

public enum SmokePreset
{
    Standard,
    Cloud,
    Dark,
    Steam
}
