using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class FireEffect : VFXEffect
{
    private double _emitTimer;
    private double _emitRate;
    private double _spread;
    private double _baseSpeed;
    private double _baseSize;
    private double _baseLife;
    private Color _colorStart;
    private Color _colorEnd;

    public FireEffect(Vector position, FirePreset preset = FirePreset.Standard) : base(EffectType.Fire, position)
    {
        switch (preset)
        {
            case FirePreset.Standard:
                _emitRate = 0.02;
                _spread = 40;
                _baseSpeed = 80;
                _baseSize = 12;
                _baseLife = 0.6;
                _colorStart = Colors.Yellow;
                _colorEnd = Color.FromRgb(180, 30, 0);
                break;
            case FirePreset.Big:
                _emitRate = 0.015;
                _spread = 60;
                _baseSpeed = 100;
                _baseSize = 18;
                _baseLife = 0.8;
                _colorStart = Color.FromRgb(255, 200, 50);
                _colorEnd = Color.FromRgb(200, 20, 0);
                break;
            case FirePreset.Inferno:
                _emitRate = 0.008;
                _spread = 80;
                _baseSpeed = 140;
                _baseSize = 22;
                _baseLife = 1.0;
                _colorStart = Color.FromRgb(255, 255, 100);
                _colorEnd = Color.FromRgb(150, 0, 0);
                break;
            case FirePreset.Torch:
                _emitRate = 0.04;
                _spread = 20;
                _baseSpeed = 50;
                _baseSize = 8;
                _baseLife = 0.5;
                _colorStart = Colors.Orange;
                _colorEnd = Color.FromRgb(100, 20, 0);
                break;
        }
    }

    public void SetPosition(Vector pos) => Position = pos;

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
            Particles[i].Update(deltaTime);
            Particles[i].Velocity *= 0.98;
            if (!Particles[i].IsAlive)
                Particles.RemoveAt(i);
        }
    }

    private void EmitParticle()
    {
        var angle = Math.PI / 2 + RandomRange(-_spread, _spread) * Math.PI / 180;
        var speed = _baseSpeed * RandomRange(0.7, 1.3);
        var vel = new Vector(Math.Cos(angle) * speed, -Math.Sin(angle) * speed);
        var pos = Position + new Vector(RandomRange(-4, 4), RandomRange(-2, 2));
        var size = _baseSize * RandomRange(0.6, 1.4);
        var life = _baseLife * RandomRange(0.7, 1.3);

        var p = new Particle(pos, vel, size, life, _colorStart)
        {
            SizeDecay = size / life * 0.5,
            RotationSpeed = RandomRange(-180, 180),
            Color = _colorStart
        };

        Particles.Add(p);
    }

    public override void Render(DrawingContext context)
    {
        foreach (var p in Particles)
        {
            var color = LerpColor(_colorStart, _colorEnd, 1 - p.LifeRatio);
            var alpha = p.LifeRatio * p.Alpha;
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

public enum FirePreset
{
    Standard,
    Big,
    Inferno,
    Torch
}
