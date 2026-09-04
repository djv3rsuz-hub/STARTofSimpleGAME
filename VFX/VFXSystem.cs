using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.VFX;

public class VFXSystem
{
    private static VFXSystem? _instance;
    public static VFXSystem Instance => _instance ??= new VFXSystem();

    private readonly List<VFXEffect> _effects = new();
    private readonly List<VFXEffect> _toAdd = new();
    private readonly List<VFXEffect> _toRemove = new();
    private readonly Random _rng = new();

    public int ActiveEffectCount => _effects.Count;
    public int TotalParticleCount => _effects.Sum(e => e.Particles.Count);

    private VFXSystem() { }

    public FireEffect CreateFire(Vector position, FirePreset preset = FirePreset.Standard)
    {
        var effect = new FireEffect(position, preset);
        _toAdd.Add(effect);
        return effect;
    }

    public LightningEffect CreateLightning(Vector start, Vector end, LightningPreset preset = LightningPreset.Standard)
    {
        var effect = new LightningEffect(start, end, preset);
        _toAdd.Add(effect);
        return effect;
    }

    public LaserEffect CreateLaser(Vector start, Vector end, LaserPreset preset = LaserPreset.Standard)
    {
        var effect = new LaserEffect(start, end, preset);
        _toAdd.Add(effect);
        return effect;
    }

    public SmokeEffect CreateSmoke(Vector position, SmokePreset preset = SmokePreset.Standard)
    {
        var effect = new SmokeEffect(position, preset);
        _toAdd.Add(effect);
        return effect;
    }

    public WaterEffect CreateWater(Vector position, WaterPreset preset = WaterPreset.Standard)
    {
        var effect = new WaterEffect(position, preset);
        _toAdd.Add(effect);
        return effect;
    }

    public void AddEffect(VFXEffect effect)
    {
        _toAdd.Add(effect);
    }

    public void RemoveEffect(VFXEffect effect)
    {
        _toRemove.Add(effect);
    }

    public void RemoveAll()
    {
        _toRemove.AddRange(_effects);
    }

    public void Update(double deltaTime)
    {
        foreach (var effect in _effects)
        {
            if (effect.IsActive)
                effect.Update(deltaTime);
        }

        foreach (var effect in _toAdd)
            _effects.Add(effect);
        _toAdd.Clear();

        foreach (var effect in _toRemove)
            _effects.Remove(effect);
        _toRemove.Clear();

        _effects.RemoveAll(e => !e.IsLooping && !e.IsActive && e.Particles.Count == 0);
    }

    public void Render(DrawingContext context)
    {
        foreach (var effect in _effects)
        {
            if (effect.IsActive)
                effect.Render(context);
        }
    }

    public void RenderSceneFlash(DrawingContext context, double gameScreenWidth, double gameScreenHeight)
    {
        int flashCount = 0;
        foreach (var effect in _effects)
        {
            if (effect is LightningEffect lightning && lightning.IsActive)
                flashCount++;
        }

        if (flashCount > 0)
        {
            var flashAlpha = Math.Min(0.15, flashCount * 0.05);
            var flashBrush = new SolidColorBrush(Color.FromArgb((byte)(flashAlpha * 255), 200, 220, 255));
            context.DrawRectangle(flashBrush, null, new Rect(0, 0, gameScreenWidth, gameScreenHeight));
        }
    }

    public void CreateExplosion(Vector center, double radius, int particleCount = 30)
    {
        for (int i = 0; i < particleCount; i++)
        {
            var angle = _rng.NextDouble() * Math.PI * 2;
            var speed = 50 + _rng.NextDouble() * 150;
            var vel = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
            var size = 3 + _rng.NextDouble() * 6;
            var life = 0.3 + _rng.NextDouble() * 0.5;

            var colors = new[] {
                Color.FromRgb(255, 200, 50),
                Color.FromRgb(255, 100, 20),
                Color.FromRgb(255, 50, 0),
                Color.FromRgb(200, 200, 255)
            };
            var color = colors[_rng.Next(colors.Length)];

            var offset = new Vector(
                (_rng.NextDouble() - 0.5) * radius * 0.3,
                (_rng.NextDouble() - 0.5) * radius * 0.3);

            _toAdd.Add(new FireEffect(center + offset, FirePreset.Inferno));
        }
    }

    public void CreateWaterSplash(Vector position, int count = 10)
    {
        for (int i = 0; i < count; i++)
        {
            var angle = -Math.PI / 2 + (_rng.NextDouble() - 0.5) * Math.PI * 0.8;
            var speed = 60 + _rng.NextDouble() * 80;
            var vel = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
            var size = 2 + _rng.NextDouble() * 4;
            var life = 0.3 + _rng.NextDouble() * 0.4;

            _toAdd.Add(new WaterEffect(position, WaterPreset.Drip));
        }
    }

    public void CreateLightningBolt(Vector start, Vector end, int branches = 2)
    {
        CreateLightning(start, end, LightningPreset.Standard);

        for (int b = 0; b < branches; b++)
        {
            var branchStart = start + (end - start) * (0.3 + _rng.NextDouble() * 0.4);
            var branchDir = (end - start);
            if (branchDir.Length > 0) branchDir.Normalize();
            var perp = new Vector(-branchDir.Y, branchDir.X);
            var branchEnd = branchStart + perp * (30 + _rng.NextDouble() * 50) *
                           (_rng.NextDouble() > 0.5 ? 1 : -1);
            CreateLightning(branchStart, branchEnd, LightningPreset.Minor);
        }
    }
}
