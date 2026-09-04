using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SimpleWPFGame.Input;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;
using SimpleWPFGame.VFX;
using SimpleWPFGame.Combat;
using SimpleWPFGame.Rendering3D;

namespace SimpleWPFGame.Game;

public sealed class GameEngine
{
    private static readonly Lazy<GameEngine> _instance = new(() => new GameEngine());
    public static GameEngine Instance => _instance.Value;

    private readonly List<GameObject> _gameObjects = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _gameLoop;
    private long _lastTick;
    private int _frameCount;
    private double _fps;
    private double _fpsUpdateTimer;
    private bool _running;

    // Performance monitoring
    private double _updateTime;
    private double _renderTime;

    public event Action<double>? OnFrameUpdate;
    public event Action<double, int>? OnRenderReady;
    public event Action<int>? OnFpsUpdated;

    public double DeltaTime { get; private set; }
    public double ElapsedTime => _stopwatch.Elapsed.TotalSeconds;
    public int ObjectCount => _gameObjects.Count;
    public bool IsRunning => _running;
    public double Fps => _fps;
    public double UpdateTime => _updateTime;
    public double RenderTime => _renderTime;

    // Cached pens/brushes for performance
    private static readonly Pen _borderPen = new(Brushes.DodgerBlue, 2);
    private static readonly Pen _cornerPen = new(Brushes.DodgerBlue, 2);
    private static readonly Brush _shadowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
    private static readonly Pen _shadowPen = new(Brushes.Transparent, 0);

    private GameEngine()
    {
        _gameLoop = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1)
        };
        _gameLoop.Tick += GameLoopTick;
    }

    public void Start()
    {
        if (_running) return;

        _stopwatch.Start();
        _lastTick = _stopwatch.ElapsedMilliseconds;
        _running = true;
        _gameLoop.Start();

        Logger.Log("Game engine started", LogLevel.Info);
    }

    public void Stop()
    {
        _running = false;
        _gameLoop.Stop();
        _stopwatch.Stop();
        Logger.Log("Game engine stopped", LogLevel.Info);
    }

    public void RegisterObject(GameObject obj)
    {
        _gameObjects.Add(obj);
        Logger.Log($"Registered: {obj.GetType().Name} at ({obj.Position.X},{obj.Position.Y})", LogLevel.Debug);
    }

    public void UnregisterObject(GameObject obj)
    {
        _gameObjects.Remove(obj);
    }

    public T? GetObject<T>() where T : GameObject
    {
        return _gameObjects.OfType<T>().FirstOrDefault();
    }

    private void GameLoopTick(object? sender, EventArgs e)
    {
        if (!_running) return;

        try
        {
            // Delta time
            long currentTick = _stopwatch.ElapsedMilliseconds;
            DeltaTime = (currentTick - _lastTick) / 1000.0;
            _lastTick = currentTick;
            DeltaTime = Math.Min(DeltaTime, 0.05);

            // FPS
            _frameCount++;
            _fpsUpdateTimer += DeltaTime;
            if (_fpsUpdateTimer >= 1.0)
            {
                _fps = _frameCount / _fpsUpdateTimer;
                _frameCount = 0;
                _fpsUpdateTimer = 0;
                OnFpsUpdated?.Invoke((int)_fps);
            }

            // Skip frame if options menu is open
            if (GameSettings.Instance.OptionsMenuOpen)
            {
                OnRenderReady?.Invoke(DeltaTime, (int)_fps);
                return;
            }

            // Update phase
            var updateSw = Stopwatch.StartNew();
            InputManager.Instance.BeginFrame();

            foreach (var obj in _gameObjects)
            {
                if (obj.IsActive)
                    obj.Update(DeltaTime);
            }

            VFXSystem.Instance.Update(DeltaTime);
            DamageNumberSystem.Instance.Update(DeltaTime);
            MeshRenderer.Instance.Update(DeltaTime);

            // Combat hit detection
            foreach (var attacker in _gameObjects.OfType<Cube>())
            {
                if (!attacker.IsActive || !attacker.Combat.IsAttacking) continue;
                if (attacker.Combat.CurrentHitboxes.Length == 0) continue;

                int startup = attacker.Combat.CurrentHitboxes[0].ActiveFrameStart;
                int activeEnd = attacker.Combat.CurrentHitboxes[0].ActiveFrameEnd;
                if (attacker.Combat.CurrentFrame < startup || attacker.Combat.CurrentFrame > activeEnd)
                    continue;

                foreach (var defender in _gameObjects.OfType<Cube>())
                {
                    if (!defender.IsActive || defender == attacker) continue;
                    if (attacker.Combat.CheckHitTarget(defender.GetHashCode())) continue;

                    var defenderRect = defender.Bounds;
                    bool hit = false;
                    foreach (var hb in attacker.Combat.CurrentHitboxes)
                    {
                        if (hb.CheckFrame(attacker.Combat.CurrentFrame) && hb.Bounds.IntersectsWith(defenderRect))
                        {
                            hit = true;
                            break;
                        }
                    }

                    if (hit)
                    {
                        attacker.Combat.RegisterHit(defender.GetHashCode());

                        var result = defender.Combat.ProcessIncomingAttack(
                            attacker.Stats,
                            new Vector(attacker.Position.X + attacker.Width / 2, attacker.Position.Y + attacker.Height / 2),
                            new Vector(defender.Position.X + defender.Width / 2, defender.Position.Y + defender.Height / 2),
                            attacker.Combat.ComboIndex);

                        if (result.Hit)
                        {
                            defender.TakeDamage(result.DamageDealt, result.Knockback, result.HitEffect);
                            Logger.Log($"{attacker.Stats.GetType().Name} hit {defender.Stats.GetType().Name} for {result.DamageDealt:F1} dmg (crit={result.Crit})", LogLevel.Debug);
                        }
                        else if (result.PerfectParried || result.Parried)
                        {
                            var pos = new Vector(defender.Position.X + defender.Width / 2, defender.Position.Y);
                            DamageNumberSystem.Instance.Add(pos, 0, false, false, result.PerfectParried, false);
                            if (result.PerfectParried) defender.Combat.TriggerCounter();
                        }
                        else if (result.Dodged)
                        {
                            var pos = new Vector(defender.Position.X + defender.Width / 2, defender.Position.Y);
                            DamageNumberSystem.Instance.Add(pos, 0, false, false, false, true);
                        }
                    }
                }
            }

            // Collision
            ResolveCollisions();
            updateSw.Stop();
            _updateTime = updateSw.Elapsed.TotalMilliseconds;

            InputManager.Instance.EndFrame();

            // Render phase
            var renderSw = Stopwatch.StartNew();
            OnFrameUpdate?.Invoke(DeltaTime);
            OnRenderReady?.Invoke(DeltaTime, (int)_fps);
            renderSw.Stop();
            _renderTime = renderSw.Elapsed.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            Logger.LogError("Game loop error", ex);
        }
    }

    public void RenderFrame(DrawingContext context)
    {
        var settings = GameSettings.Instance;
        double w = settings.GameScreenWidth;
        double h = settings.GameScreenHeight;
        var quality = settings.GraphicsQuality;

        // Background
        var bgColor = Config.CharSettings.ParseColor(settings.BackgroundColor);
        context.DrawRectangle(bgColor, null, new Rect(0, 0, w, h));

        // Shadows (only on Medium/High)
        if (settings.Shadows && quality != GraphicsQuality.Low)
        {
            double shadowOffset = quality == GraphicsQuality.High ? 6 : 3;
            double shadowBlur = quality == GraphicsQuality.High ? 4 : 2;

            foreach (var obj in _gameObjects)
            {
                if (!obj.IsActive) continue;
                var shadowRect = new Rect(
                    obj.Position.X + shadowOffset,
                    obj.Position.Y + shadowOffset,
                    obj.Width, obj.Height);
                context.DrawRectangle(_shadowBrush, _shadowPen, shadowRect);
            }
        }

        // Render all objects
        foreach (var obj in _gameObjects)
        {
            obj.Render(context);
        }

        // VFX
        VFXSystem.Instance.Render(context);
        VFXSystem.Instance.RenderSceneFlash(context, w, h);

        // Damage numbers
        if (GameSettings.Instance.ShowDamageNumbers)
            DamageNumberSystem.Instance.Render(context);

        // Game border
        if (settings.ShowGameBorder)
        {
            double inset = 1;
            context.DrawRectangle(null, _borderPen, new Rect(inset, inset, w - inset * 2, h - inset * 2));

            // Corner ticks
            double tick = 12;
            context.DrawLine(_cornerPen, new Point(0, 0), new Point(tick, 0));
            context.DrawLine(_cornerPen, new Point(0, 0), new Point(0, tick));
            context.DrawLine(_cornerPen, new Point(w, 0), new Point(w - tick, 0));
            context.DrawLine(_cornerPen, new Point(w, 0), new Point(w, tick));
            context.DrawLine(_cornerPen, new Point(0, h), new Point(tick, h));
            context.DrawLine(_cornerPen, new Point(0, h), new Point(0, h - tick));
            context.DrawLine(_cornerPen, new Point(w, h), new Point(w - tick, h));
            context.DrawLine(_cornerPen, new Point(w, h), new Point(w, h - tick));
        }
    }

    private void ResolveCollisions()
    {
        for (int i = 0; i < _gameObjects.Count; i++)
        {
            for (int j = i + 1; j < _gameObjects.Count; j++)
            {
                var a = _gameObjects[i];
                var b = _gameObjects[j];

                if (!a.IsActive || !b.IsActive) continue;
                if (!a.HasCollision || !b.HasCollision) continue;
                if (!a.Intersects(b)) continue;

                var ab = a.CollisionBounds;
                var bb = b.CollisionBounds;

                double overlapLeft = ab.Right - bb.Left;
                double overlapRight = bb.Right - ab.Left;
                double overlapTop = ab.Bottom - bb.Top;
                double overlapBottom = bb.Bottom - ab.Top;

                double minOverlapX = overlapLeft < overlapRight ? -overlapLeft : overlapRight;
                double minOverlapY = overlapTop < overlapBottom ? -overlapTop : overlapBottom;

                if (Math.Abs(minOverlapX) < Math.Abs(minOverlapY))
                {
                    a.Position = new Vector(a.Position.X + minOverlapX, a.Position.Y);
                    a.Velocity = new Vector(0, a.Velocity.Y);
                    b.Velocity = new Vector(0, b.Velocity.Y);
                }
                else
                {
                    a.Position = new Vector(a.Position.X, a.Position.Y + minOverlapY);
                    a.Velocity = new Vector(a.Velocity.X, 0);
                    b.Velocity = new Vector(b.Velocity.X, 0);
                }
            }
        }
    }
}
