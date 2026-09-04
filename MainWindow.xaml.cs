using System.Runtime.InteropServices;
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
    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const int TARGET_WIDTH = 1920;
    private const int TARGET_HEIGHT = 1080;

    private GameVisualHost? _gameHost;
    private Cube? _blueCube;
    private Cube? _redCube;
    private Cube? _greenCube;
    private readonly DispatcherTimer _uiUpdateTimer;
    private readonly DispatcherTimer _controllerCheckTimer;
    private bool _optionsMenuOpen;

    public MainWindow()
    {
        InitializeComponent();

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
            var settings = GameSettings.Instance;
            settings.Load();
            Logger.Log("GameSettings loaded", LogLevel.Info);

            // Load character settings
            var charSettingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Config", "charsettings.ini");
            CharSettings.Load(charSettingsPath);

            // Apply display settings
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            WindowSizeText.Text = $"{settings.WindowWidth}x{settings.WindowHeight} | {settings.GameScreenWidth}x{settings.GameScreenHeight}";

            // Create single game host (fixed duplicate bug)
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
                    DecelerationSpeed = playerData.DecelerationSpeed,
                    DashDistance = playerData.DashDistance,
                    DashRotationSpeed = playerData.DashRotationSpeed,
                    DashCooldown = playerData.DashCooldown,
                    DashDuration = playerData.DashDuration
                };
                engine.RegisterObject(_blueCube);
                Logger.Log($"Player: {playerData.Name} | Speed={playerData.MoveSpeed} Accel={playerData.AccelerationSpeed}", LogLevel.Info);
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
            }

            // Start engine
            engine.Start();

            // Start timers
            _uiUpdateTimer.Start();
            _controllerCheckTimer.Start();

            StatusText.Text = "Game running | ESC = Options";
            Logger.Log("All systems initialized", LogLevel.Info);
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

            // FPS
            FpsDisplay.Visibility = settings.ShowFps ? Visibility.Visible : Visibility.Collapsed;
            FpsDisplay.Text = $"FPS: {engine.Fps}";

            // Controller
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

            // Debug info
            ObjectCountText.Text = $"Objects: {engine.ObjectCount}";
            QualityText.Text = $"Quality: {settings.GraphicsQuality}";
            VSyncText.Text = $"VSync: {(settings.VSync ? "ON" : "OFF")}";

            // Collision
            CollisionDebugText.Text = settings.ShowCollision ? "Collision: ON [F3]" : "Collision: OFF [F3]";
            CollisionDebugText.Foreground = settings.ShowCollision ? Brushes.LimeGreen : Brushes.Gray;

            // Input
            InputStatusText.Text = controller.IsConnected ? "Input: Controller" : "Input: Keyboard/Mouse";

            // Cube position
            if (_blueCube != null)
                CubePositionText.Text = $"Position: {(int)_blueCube.Position.X}, {(int)_blueCube.Position.Y}";

            // Time
            var elapsed = TimeSpan.FromSeconds(engine.ElapsedTime);
            TimeDisplay.Text = elapsed.ToString(@"hh\:mm\:ss");

            StatusText.Text = _optionsMenuOpen
                ? "Options menu open"
                : "Game running | ESC = Options";
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
            // Start button toggles options menu
            if (controller.IsStartPressed && !_optionsMenuOpen)
                OpenOptionsMenu();
        }
    }

    // --- Options Menu ---

    private void OpenOptionsMenu()
    {
        var settings = GameSettings.Instance;
        _optionsMenuOpen = true;
        settings.OptionsMenuOpen = true;
        OptionsMenuOverlay.Visibility = Visibility.Visible;

        // Sync UI to current settings
        QualityLowBtn.IsEnabled = settings.GraphicsQuality != GraphicsQuality.Low;
        QualityMedBtn.IsEnabled = settings.GraphicsQuality != GraphicsQuality.Medium;
        QualityHighBtn.IsEnabled = settings.GraphicsQuality != GraphicsQuality.High;

        VSyncToggleBtn.Content = settings.VSync ? "ON" : "OFF";
        ShadowsToggleBtn.Content = settings.Shadows ? "ON" : "OFF";

        MasterVolumeSlider.Value = settings.MasterVolume * 100;
        MusicVolumeSlider.Value = settings.MusicVolume * 100;
        SfxVolumeSlider.Value = settings.SfxVolume * 100;
        VfxVolumeSlider.Value = settings.VfxVolume * 100;

        Logger.Log("Options menu opened", LogLevel.Info);
    }

    private void CloseOptionsMenu()
    {
        _optionsMenuOpen = false;
        GameSettings.Instance.OptionsMenuOpen = false;
        OptionsMenuOverlay.Visibility = Visibility.Collapsed;
    }

    private void OptionsMenuOverlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Click outside menu closes it
    }

    private void HandledTrue(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void QualityLow_Click(object sender, RoutedEventArgs e)
    {
        GameSettings.Instance.GraphicsQuality = GraphicsQuality.Low;
        UpdateQualityButtons();
        Logger.Log("Graphics quality: Low", LogLevel.Info);
    }

    private void QualityMed_Click(object sender, RoutedEventArgs e)
    {
        GameSettings.Instance.GraphicsQuality = GraphicsQuality.Medium;
        UpdateQualityButtons();
        Logger.Log("Graphics quality: Medium", LogLevel.Info);
    }

    private void QualityHigh_Click(object sender, RoutedEventArgs e)
    {
        GameSettings.Instance.GraphicsQuality = GraphicsQuality.High;
        UpdateQualityButtons();
        Logger.Log("Graphics quality: High", LogLevel.Info);
    }

    private void UpdateQualityButtons()
    {
        var q = GameSettings.Instance.GraphicsQuality;
        QualityLowBtn.IsEnabled = q != GraphicsQuality.Low;
        QualityMedBtn.IsEnabled = q != GraphicsQuality.Medium;
        QualityHighBtn.IsEnabled = q != GraphicsQuality.High;
    }

    private void VSyncToggle_Click(object sender, RoutedEventArgs e)
    {
        var s = GameSettings.Instance;
        s.VSync = !s.VSync;
        VSyncToggleBtn.Content = s.VSync ? "ON" : "OFF";
        Logger.Log($"VSync: {(s.VSync ? "ON" : "OFF")}", LogLevel.Info);
    }

    private void ShadowsToggle_Click(object sender, RoutedEventArgs e)
    {
        var s = GameSettings.Instance;
        s.Shadows = !s.Shadows;
        ShadowsToggleBtn.Content = s.Shadows ? "ON" : "OFF";
        Logger.Log($"Shadows: {(s.Shadows ? "ON" : "OFF")}", LogLevel.Info);
    }

    private void MasterVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        GameSettings.Instance.MasterVolume = (float)(e.NewValue / 100.0);
        MasterVolumeText.Text = $"{(int)e.NewValue}%";
    }

    private void MusicVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        GameSettings.Instance.MusicVolume = (float)(e.NewValue / 100.0);
        MusicVolumeText.Text = $"{(int)e.NewValue}%";
    }

    private void SfxVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        GameSettings.Instance.SfxVolume = (float)(e.NewValue / 100.0);
        SfxVolumeText.Text = $"{(int)e.NewValue}%";
    }

    private void VfxVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        GameSettings.Instance.VfxVolume = (float)(e.NewValue / 100.0);
        VfxVolumeText.Text = $"{(int)e.NewValue}%";
    }

    private void OptionsSaveClose_Click(object sender, RoutedEventArgs e)
    {
        GameSettings.Instance.Save();
        CloseOptionsMenu();
        ConsoleWrite("Settings saved", "#FF00FF88");
    }

    private void OptionsClose_Click(object sender, RoutedEventArgs e)
    {
        CloseOptionsMenu();
    }

    // --- Input ---

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_optionsMenuOpen)
        {
            if (e.Key == System.Windows.Input.Key.Escape || e.Key == System.Windows.Input.Key.O)
            {
                GameSettings.Instance.Save();
                CloseOptionsMenu();
                e.Handled = true;
            }
            return;
        }

        switch (e.Key)
        {
            case System.Windows.Input.Key.Escape:
            case System.Windows.Input.Key.O:
                OpenOptionsMenu();
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
        Logger.Log($"Collision debug: {settings.ShowCollision}", LogLevel.Info);
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

        ConsoleWrite($"> {raw}", "#FF555555");

        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        switch (command)
        {
            case "help":
                ConsoleWrite("Commands:", "#FF00D4FF");
                ConsoleWrite("  collision      - Toggle collision (F3)", "#FF888888");
                ConsoleWrite("  fps            - Toggle FPS display", "#FF888888");
                ConsoleWrite("  objects        - List objects", "#FF888888");
                ConsoleWrite("  position       - Show player pos", "#FF888888");
                ConsoleWrite("  speed <n>      - Set speed", "#FF888888");
                ConsoleWrite("  accel <n>      - Set acceleration", "#FF888888");
                ConsoleWrite("  decel <n>      - Set deceleration", "#FF888888");
                ConsoleWrite("  quality <l/m/h>- Set graphics", "#FF888888");
                ConsoleWrite("  vsync          - Toggle VSync", "#FF888888");
                ConsoleWrite("  shadows        - Toggle shadows", "#FF888888");
                ConsoleWrite("  volume <0-100> - Master volume", "#FF888888");
                ConsoleWrite("  clear          - Clear console", "#FF888888");
                ConsoleWrite("  save           - Save settings", "#FF888888");
                ConsoleWrite("  reset          - Reset player", "#FF888888");
                ConsoleWrite("  options        - Open options", "#FF888888");
                break;

            case "collision":
            case "col":
                ToggleCollision();
                break;

            case "fps":
                var s = GameSettings.Instance;
                s.ShowFps = !s.ShowFps;
                ConsoleWrite($"FPS: {(s.ShowFps ? "ON" : "OFF")}", "#FF00FF88");
                break;

            case "objects":
            case "obj":
                ConsoleWrite($"Objects: {GameEngine.Instance.ObjectCount}", "#FF00FF88");
                if (_blueCube != null)
                {
                    var dash = _blueCube.IsDashing ? "DASHING" : (_blueCube.DashCooldownRemaining > 0 ? $"CD:{_blueCube.DashCooldownRemaining:F1}s" : "Ready");
                    ConsoleWrite($"  Blue pos=({_blueCube.Position.X:F0},{_blueCube.Position.Y:F0}) spd={_blueCube.MoveSpeed} dash={dash}", "#FF888888");
                }
                if (_redCube != null)
                    ConsoleWrite($"  Red  pos=({_redCube.Position.X:F0},{_redCube.Position.Y:F0})", "#FF888888");
                if (_greenCube != null)
                    ConsoleWrite($"  Green pos=({_greenCube.Position.X:F0},{_greenCube.Position.Y:F0})", "#FF888888");
                break;

            case "position":
            case "pos":
                if (_blueCube != null)
                    ConsoleWrite($"pos=({(int)_blueCube.Position.X},{(int)_blueCube.Position.Y}) vel=({_blueCube.Velocity.X:F1},{_blueCube.Velocity.Y:F1})", "#FF00FF88");
                break;

            case "speed":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var spd) && _blueCube != null)
                { _blueCube.MoveSpeed = spd; ConsoleWrite($"Speed: {spd}", "#FF00FF88"); }
                else ConsoleWrite("Usage: speed <number>", "#FFFF4444");
                break;

            case "accel":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var acc) && _blueCube != null)
                { _blueCube.AccelerationSpeed = acc; ConsoleWrite($"Acceleration: {acc}", "#FF00FF88"); }
                else ConsoleWrite("Usage: accel <number>", "#FFFF4444");
                break;

            case "decel":
                if (args.Length > 0 && double.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec) && _blueCube != null)
                { _blueCube.DecelerationSpeed = dec; ConsoleWrite($"Deceleration: {dec}", "#FF00FF88"); }
                else ConsoleWrite("Usage: decel <number>", "#FFFF4444");
                break;

            case "quality":
                if (args.Length > 0)
                {
                    var q = args[0] switch
                    {
                        "l" or "low" => GraphicsQuality.Low,
                        "m" or "med" or "medium" => GraphicsQuality.Medium,
                        "h" or "high" => GraphicsQuality.High,
                        _ => (GraphicsQuality?)null
                    };
                    if (q.HasValue)
                    { GameSettings.Instance.GraphicsQuality = q.Value; ConsoleWrite($"Quality: {q}", "#FF00FF88"); }
                    else ConsoleWrite("Usage: quality <low/medium/high>", "#FFFF4444");
                }
                else ConsoleWrite("Usage: quality <low/medium/high>", "#FFFF4444");
                break;

            case "vsync":
                var vs = GameSettings.Instance;
                vs.VSync = !vs.VSync;
                ConsoleWrite($"VSync: {(vs.VSync ? "ON" : "OFF")}", "#FF00FF88");
                break;

            case "shadows":
                var sh = GameSettings.Instance;
                sh.Shadows = !sh.Shadows;
                ConsoleWrite($"Shadows: {(sh.Shadows ? "ON" : "OFF")}", "#FF00FF88");
                break;

            case "volume":
                if (args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var vol))
                { GameSettings.Instance.MasterVolume = Math.Clamp(vol / 100f, 0, 1); ConsoleWrite($"Volume: {vol}%", "#FF00FF88"); }
                else ConsoleWrite("Usage: volume <0-100>", "#FFFF4444");
                break;

            case "clear":
                ConsoleOutput.Text = "";
                break;

            case "log":
                Logger.Log($"Console: {string.Join(' ', args)}", LogLevel.Info);
                ConsoleWrite("Logged", "#FF00FF88");
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
                        ConsoleWrite($"Reset to ({pData.StartX}, {pData.StartY})", "#FF00FF88");
                    }
                }
                break;

            case "options":
                OpenOptionsMenu();
                break;

            default:
                ConsoleWrite($"Unknown: {command}. Type 'help'.", "#FFFF4444");
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

        if (LogDisplay.Text.Length > 5000)
            LogDisplay.Text = LogDisplay.Text[^3000..];
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Force exact 1920x1080 total window size, centered on screen
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            int style = GetWindowLong(helper.Handle, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME);
            SetWindowLong(helper.Handle, GWL_STYLE, style);

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            int x = (int)((screenW - TARGET_WIDTH) / 2);
            int y = (int)((screenH - TARGET_HEIGHT) / 2);
            SetWindowPos(helper.Handle, IntPtr.Zero, x, y, TARGET_WIDTH, TARGET_HEIGHT, SWP_FRAMECHANGED);

            Logger.Log($"Window: {TARGET_WIDTH}x{TARGET_HEIGHT} centered on screen", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to set exact window size", ex);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Allow dragging the borderless window
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }
}
