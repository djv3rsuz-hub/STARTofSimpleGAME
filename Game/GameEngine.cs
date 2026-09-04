using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SimpleWPFGame.Input;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;

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

    public event Action<double>? OnFrameUpdate;
    public event Action<double, int>? OnRenderReady;
    public event Action<int>? OnFpsUpdated;

    public double DeltaTime { get; private set; }
    public double ElapsedTime => _stopwatch.Elapsed.TotalSeconds;
    public int ObjectCount => _gameObjects.Count;
    public bool IsRunning => _running;
    public double Fps => _fps;

    private GameEngine()
    {
        _gameLoop = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1) // Max frequency, actual timing via Stopwatch
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
        Logger.Log($"Registered object: {obj.GetType().Name} at ({obj.Position.X}, {obj.Position.Y})", LogLevel.Debug);
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
            // Calculate delta time
            long currentTick = _stopwatch.ElapsedMilliseconds;
            DeltaTime = (currentTick - _lastTick) / 1000.0;
            _lastTick = currentTick;

            // Clamp delta time to prevent spiral of death
            DeltaTime = Math.Min(DeltaTime, 0.05);

            // FPS calculation
            _frameCount++;
            _fpsUpdateTimer += DeltaTime;
            if (_fpsUpdateTimer >= 1.0)
            {
                _fps = _frameCount / _fpsUpdateTimer;
                _frameCount = 0;
                _fpsUpdateTimer = 0;
                OnFpsUpdated?.Invoke((int)_fps);
            }

            // Input
            InputManager.Instance.BeginFrame();

            // Update
            foreach (var obj in _gameObjects)
            {
                if (obj.IsActive)
                    obj.Update(DeltaTime);
            }

            // Controller input check
            var controller = ControllerManager.Instance;
            if (controller.IsConnected && controller.IsAPressed)
            {
                // A button pressed feedback
            }

            InputManager.Instance.EndFrame();

            // Notify render
            OnFrameUpdate?.Invoke(DeltaTime);
            OnRenderReady?.Invoke(DeltaTime, (int)_fps);
        }
        catch (Exception ex)
        {
            Logger.LogError("Game loop error", ex);
        }
    }

    public void RenderFrame(DrawingContext context)
    {
        var settings = GameSettings.Instance;
        var bgColor = Config.CharSettings.ParseColor(settings.BackgroundColor);
        context.DrawRectangle(bgColor, null, new Rect(0, 0, settings.GameScreenWidth, settings.GameScreenHeight));

        foreach (var obj in _gameObjects)
        {
            obj.Render(context);
        }
    }
}
