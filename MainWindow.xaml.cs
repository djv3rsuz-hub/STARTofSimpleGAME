using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SimpleWPFGame.Config;
using SimpleWPFGame.Game;
using SimpleWPFGame.Input;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;

namespace SimpleWPFGame;

public class GameVisualHost : FrameworkElement
{
    private readonly VisualCollection _visuals;
    private readonly DrawingVisual _visual = new();

    public GameVisualHost()
    {
        _visuals = new VisualCollection(this) { _visual };
    }

    public DrawingVisual Visual => _visual;

    protected override int VisualChildrenCount => _visuals.Count;
    protected override Visual GetVisualChild(int index) => _visuals[index];
}

public partial class MainWindow : Window
{
    private GameVisualHost? _gameHost;
    private Cube? _blueCube;
    private Cube? _redCube;
    private Cube? _greenCube;
    private readonly DispatcherTimer _uiUpdateTimer;
    private readonly DispatcherTimer _controllerCheckTimer;

    public MainWindow()
    {
        InitializeComponent();

        _gameHost = new GameVisualHost { Width = 1280, Height = 720 };
        GameCanvas.Children.Add(_gameHost);

        // UI update timer (30fps for UI elements, game renders at full speed)
        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _uiUpdateTimer.Tick += UpdateUI;

        // Controller connection check (every 2 seconds)
        _controllerCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _controllerCheckTimer.Tick += CheckController;

        Logger.OnLogMessage += OnLogReceived;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Log("MainWindow loaded", LogLevel.Info);

            // Load game settings
            GameSettings.Instance.Load();
            Logger.Log("GameSettings loaded", LogLevel.Info);

            // Load character settings
            var charSettingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Config", "charsettings.ini");
            CharSettings.Load(charSettingsPath);

            // Apply display settings
            var settings = GameSettings.Instance;
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            _gameHost = new GameVisualHost
            {
                Width = settings.GameScreenWidth,
                Height = settings.GameScreenHeight
            };
            GameCanvas.Children.Add(_gameHost);

            // Initialize input
            InputManager.Instance.Initialize(GameCanvas);
            Logger.Log("InputManager ready", LogLevel.Info);

            // Initialize controller
            ControllerManager.Instance.Initialize();
            Logger.Log("ControllerManager ready", LogLevel.Info);

            // Initialize game engine
            var engine = GameEngine.Instance;
            engine.OnRenderReady += OnRenderReady;

            // Create game objects from charsettings.ini
            var playerData = CharSettings.GetCharacter("PlayerCube");
            var enemyData = CharSettings.GetCharacter("EnemyCube");
            var greenData = CharSettings.GetCharacter("GreenCube");

            if (playerData != null)
            {
                _blueCube = new Cube(
                    playerData.StartX, playerData.StartY,
                    playerData.Size, playerData.Color,
                    playerData.Controllable)
                {
                    MoveSpeed = playerData.MoveSpeed,
                    AccelerationSpeed = playerData.AccelerationSpeed,
                    DecelerationSpeed = playerData.DecelerationSpeed
                };
                engine.RegisterObject(_blueCube);
                Logger.Log($"Player cube: {playerData.Name} | Speed={playerData.MoveSpeed} Accel={playerData.AccelerationSpeed} Decel={playerData.DecelerationSpeed}", LogLevel.Info);
            }

            if (enemyData != null)
            {
                _redCube = new Cube(
                    enemyData.StartX, enemyData.StartY,
                    enemyData.Size, enemyData.Color,
                    enemyData.Controllable)
                {
                    MoveSpeed = enemyData.MoveSpeed,
                    AccelerationSpeed = enemyData.AccelerationSpeed,
                    DecelerationSpeed = enemyData.DecelerationSpeed
                };
                engine.RegisterObject(_redCube);
                Logger.Log($"Enemy cube: {enemyData.Name} at ({enemyData.StartX}, {enemyData.StartY})", LogLevel.Info);
            }

            if (greenData != null)
            {
                _greenCube = new Cube(
                    greenData.StartX, greenData.StartY,
                    greenData.Size, greenData.Color,
                    greenData.Controllable)
                {
                    MoveSpeed = greenData.MoveSpeed,
                    AccelerationSpeed = greenData.AccelerationSpeed,
                    DecelerationSpeed = greenData.DecelerationSpeed
                };
                engine.RegisterObject(_greenCube);
                Logger.Log($"Green cube: {greenData.Name} at ({greenData.StartX}, {greenData.StartY})", LogLevel.Info);
            }

            // Start engine
            engine.Start();

            // Start timers
            _uiUpdateTimer.Start();
            _controllerCheckTimer.Start();

            StatusText.Text = "Game running";
            Logger.Log("All systems initialized successfully", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to initialize game", ex);
            MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnRenderReady(double deltaTime, int fps)
    {
        try
        {
            if (_gameHost == null) return;
            using var dc = _gameHost.Visual.RenderOpen();
            GameEngine.Instance.RenderFrame(dc);
        }
        catch (Exception ex)
        {
            Logger.LogError("Render error", ex);
        }
    }

    private void UpdateUI(object? sender, EventArgs e)
    {
        try
        {
            var engine = GameEngine.Instance;
            var controller = ControllerManager.Instance;
            var settings = GameSettings.Instance;

            // FPS display
            FpsDisplay.Visibility = settings.ShowFps ? Visibility.Visible : Visibility.Collapsed;
            FpsDisplay.Text = $"FPS: {engine.Fps}";

            // Controller display
            if (controller.IsConnected)
            {
                ControllerDisplay.Text = $"Controller: {controller.ActiveControllerType}";
                ControllerDisplay.Foreground = Brushes.LimeGreen;
                ControllerButtonText.Text = $"Controller: {controller.ActiveControllerType}";
            }
            else
            {
                ControllerDisplay.Text = "No Controller";
                ControllerDisplay.Foreground = Brushes.OrangeRed;
                ControllerButtonText.Text = "Controller: None";
            }

            // Debug info visibility
            var debugVis = settings.ShowDebugInfo ? Visibility.Visible : Visibility.Collapsed;
            ObjectCountText.Visibility = debugVis;
            EngineStatusText.Visibility = debugVis;
            ControllerButtonText.Visibility = debugVis;
            ObjectCountText.Text = $"Objects: {engine.ObjectCount}";
            EngineStatusText.Text = engine.IsRunning ? "Engine: Running" : "Engine: Stopped";

            // Log panel visibility
            LogScrollViewer.Visibility = settings.ShowLogPanel ? Visibility.Visible : Visibility.Collapsed;

            // Collision debug status
            CollisionDebugText.Text = settings.ShowCollision ? "Collision: ON [F3]" : "Collision: OFF [F3]";
            CollisionDebugText.Foreground = settings.ShowCollision ? Brushes.LimeGreen : Brushes.Gray;

            // Input status
            var input = InputManager.Instance;
            InputStatusText.Text = controller.IsConnected ? "Input: Controller" : "Input: Keyboard/Mouse";

            // Cube position
            if (_blueCube != null)
                CubePositionText.Text = $"Position: {(int)_blueCube.Position.X}, {(int)_blueCube.Position.Y}";

            // Time
            var elapsed = TimeSpan.FromSeconds(engine.ElapsedTime);
            TimeDisplay.Text = elapsed.ToString(@"hh\:mm\:ss");

            StatusText.Text = engine.IsRunning ? "Game running | Press ESC to exit" : "Game stopped";
        }
        catch (Exception ex)
        {
            Logger.LogError("UI update error", ex);
        }
    }

    private void CheckController(object? sender, EventArgs e)
    {
        var controller = ControllerManager.Instance;
        if (controller.IsConnected)
        {
            var buttons = new List<string>();
            if (controller.IsAPressed) buttons.Add("A");
            if (controller.IsBPressed) buttons.Add("B");
            if (controller.IsXPressed) buttons.Add("X");
            if (controller.IsYPressed) buttons.Add("Y");
            if (controller.IsStartPressed) buttons.Add("Start");
            if (controller.IsBackPressed) buttons.Add("Back");
            if (controller.IsLeftShoulder) buttons.Add("LB");
            if (controller.IsRightShoulder) buttons.Add("RB");
        }
    }

    private void OnLogReceived(string message, LogLevel level)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnLogReceived(message, level));
            return;
        }

        var color = level switch
        {
            LogLevel.Error => "#FFFF4444",
            LogLevel.Warning => "#FFFFFF44",
            LogLevel.Info => "#FF00FF88",
            _ => "#FF666666"
        };

        LogDisplay.Text += $"<{color}>{message}</{color}>\n";
        LogScrollViewer.ScrollToEnd();

        // Keep log size manageable
        if (LogDisplay.Text.Length > 5000)
            LogDisplay.Text = LogDisplay.Text[^3000..];
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Escape:
                Close();
                break;
            case System.Windows.Input.Key.F3:
                ToggleCollision();
                break;
        }
    }

    private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
    }

    private void ToggleCollision()
    {
        var settings = GameSettings.Instance;
        settings.ShowCollision = !settings.ShowCollision;
        CollisionDebugText.Text = settings.ShowCollision ? "Collision: ON [F3]" : "Collision: OFF [F3]";
        CollisionDebugText.Foreground = settings.ShowCollision ? Brushes.LimeGreen : Brushes.Gray;
        ConsoleWrite(settings.ShowCollision ? "Collision debug: ON" : "Collision debug: OFF", "#FF00FF88");
        Logger.Log($"Collision debug toggled: {settings.ShowCollision}", LogLevel.Info);
    }

    // --- Debug Console ---

    private void ConsoleInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            RunConsoleCommand(ConsoleInput.Text);
    }

    private void ConsoleSend_Click(object sender, RoutedEventArgs e)
    {
        RunConsoleCommand(ConsoleInput.Text);
    }

    private void RunConsoleCommand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        var cmd = raw.Trim().ToLowerInvariant();
        ConsoleInput.Text = "";

        // Echo the command
        ConsoleWrite($"> {raw}", "#FF555555");

        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        switch (command)
        {
            case "help":
                ConsoleWrite("Commands:", "#FF00D4FF");
                ConsoleWrite("  help           - Show this list", "#FF888888");
                ConsoleWrite("  collision      - Toggle collision debug (F3)", "#FF888888");
                ConsoleWrite("  fps            - Toggle FPS display", "#FF888888");
                ConsoleWrite("  objects        - List all game objects", "#FF888888");
                ConsoleWrite("  position       - Show blue cube position", "#FF888888");
                ConsoleWrite("  speed <value>  - Set blue cube speed", "#FF888888");
                ConsoleWrite("  accel <value>  - Set blue cube acceleration", "#FF888888");
                ConsoleWrite("  decel <value>  - Set blue cube deceleration", "#FF888888");
                ConsoleWrite("  clear          - Clear console", "#FF888888");
                ConsoleWrite("  log <msg>      - Write to log", "#FF888888");
                ConsoleWrite("  save           - Save settings", "#FF888888");
                ConsoleWrite("  reset          - Reset cube to start", "#FF888888");
                break;

            case "collision":
            case "col":
                ToggleCollision();
                break;

            case "fps":
                var s = GameSettings.Instance;
                s.ShowFps = !s.ShowFps;
                ConsoleWrite($"FPS display: {(s.ShowFps ? "ON" : "OFF")}", "#FF00FF88");
                break;

            case "objects":
            case "obj":
                var objs = GameEngine.Instance.ObjectCount;
                ConsoleWrite($"Game objects: {objs}", "#FF00FF88");
                if (_blueCube != null)
                    ConsoleWrite($"  BlueCube pos=({_blueCube.Position.X:F0},{_blueCube.Position.Y:F0}) size={_blueCube.Width} speed={_blueCube.MoveSpeed}", "#FF888888");
                if (_redCube != null)
                    ConsoleWrite($"  RedCube  pos=({_redCube.Position.X:F0},{_redCube.Position.Y:F0}) size={_redCube.Width}", "#FF888888");
                break;

            case "position":
            case "pos":
                if (_blueCube != null)
                    ConsoleWrite($"Blue cube: ({(int)_blueCube.Position.X}, {(int)_blueCube.Position.Y}) vel=({_blueCube.Velocity.X:F1},{_blueCube.Velocity.Y:F1})", "#FF00FF88");
                break;

            case "speed":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var spd) && _blueCube != null)
                {
                    _blueCube.MoveSpeed = spd;
                    ConsoleWrite($"Blue cube speed: {spd}", "#FF00FF88");
                }
                else
                    ConsoleWrite("Usage: speed <number>", "#FFFF4444");
                break;

            case "accel":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var acc) && _blueCube != null)
                {
                    _blueCube.AccelerationSpeed = acc;
                    ConsoleWrite($"Blue cube acceleration: {acc}", "#FF00FF88");
                }
                else
                    ConsoleWrite("Usage: accel <number>", "#FFFF4444");
                break;

            case "decel":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec) && _blueCube != null)
                {
                    _blueCube.DecelerationSpeed = dec;
                    ConsoleWrite($"Blue cube deceleration: {dec}", "#FF00FF88");
                }
                else
                    ConsoleWrite("Usage: decel <number>", "#FFFF4444");
                break;

            case "clear":
                ConsoleOutput.Text = "";
                break;

            case "log":
                var msg = string.Join(' ', args);
                Logger.Log($"Console: {msg}", LogLevel.Info);
                ConsoleWrite($"Logged: {msg}", "#FF00FF88");
                break;

            case "save":
                GameSettings.Instance.Save();
                ConsoleWrite("Settings saved", "#FF00FF88");
                break;

            case "reset":
                if (_blueCube != null)
                {
                    var pData = CharSettings.GetCharacter("PlayerCube");
                    if (pData != null)
                    {
                        _blueCube.Position = new System.Windows.Vector(pData.StartX, pData.StartY);
                        _blueCube.Velocity = new System.Windows.Vector(0, 0);
                        ConsoleWrite($"Blue cube reset to ({pData.StartX}, {pData.StartY})", "#FF00FF88");
                    }
                }
                break;

            default:
                ConsoleWrite($"Unknown command: {command}. Type 'help'.", "#FFFF4444");
                break;
        }
    }

    private void ConsoleWrite(string text, string color = "#FF888888")
    {
        ConsoleOutput.Text += $"<{color}>{text}</{color}>\n";
        ConsoleScrollViewer.ScrollToEnd();

        if (ConsoleOutput.Text.Length > 4000)
            ConsoleOutput.Text = ConsoleOutput.Text[^2000..];
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Logger.Log("MainWindow closing", LogLevel.Info);
        GameSettings.Instance.Save();
        GameEngine.Instance.Stop();
        ControllerManager.Instance.Shutdown();
        InputManager.Instance.EndFrame();
        Logger.Shutdown();
    }
}
