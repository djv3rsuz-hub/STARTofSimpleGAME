using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using SimpleWPFGame.Config;
using SimpleWPFGame.Game;
using SimpleWPFGame.Input;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;
using SimpleWPFGame.UI;
using SimpleWPFGame.SaveSystem;
using SimpleWPFGame.VFX;
using SimpleWPFGame.Combat;
using SimpleWPFGame.Rendering3D;

namespace SimpleWPFGame;

using InputManager = SimpleWPFGame.Input.InputManager;

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
    private int _targetWidth = 1920;
    private int _targetHeight = 1080;

    private GameVisualHost? _gameHost;
    private Cube? _blueCube;
    private Cube? _redCube;
    private Cube? _greenCube;
    private Cube? _dummyCube;
    private bool _hitboxDebug;
    private Scene3D? _scene3D;
    private bool _3DMouseDragging;
    private System.Windows.Point _3DLastMousePos;
    private readonly DispatcherTimer _uiUpdateTimer;
    private readonly DispatcherTimer _controllerCheckTimer;

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

            // Initialize tooltip system
            TooltipManager.Instance.Initialize(this);
            Logger.Log("TooltipManager ready", LogLevel.Info);

            // Initialize tab icon buttons
            InitializeTabIcons();

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
                    DashDuration = playerData.DashDuration,
                    Stats = playerData.Stats,
                    Actions = playerData.Actions
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
                    DecelerationSpeed = enemyData.DecelerationSpeed,
                    DashDistance = enemyData.DashDistance,
                    DashRotationSpeed = enemyData.DashRotationSpeed,
                    DashCooldown = enemyData.DashCooldown,
                    DashDuration = enemyData.DashDuration,
                    Stats = enemyData.Stats,
                    Actions = enemyData.Actions
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
                    DecelerationSpeed = greenData.DecelerationSpeed,
                    DashDistance = greenData.DashDistance,
                    DashRotationSpeed = greenData.DashRotationSpeed,
                    DashCooldown = greenData.DashCooldown,
                    DashDuration = greenData.DashDuration,
                    Stats = greenData.Stats,
                    Actions = greenData.Actions
                };
                engine.RegisterObject(_greenCube);
            }

            // Load dummy cube
            var dummyData = CharSettings.GetCharacter("DummyCube");
            if (dummyData != null)
            {
                _dummyCube = new Cube(
                    dummyData.StartX, dummyData.StartY,
                    dummyData.Size, dummyData.Color,
                    dummyData.Controllable)
                {
                    MoveSpeed = dummyData.MoveSpeed,
                    AccelerationSpeed = dummyData.AccelerationSpeed,
                    DecelerationSpeed = dummyData.DecelerationSpeed,
                    DashDistance = dummyData.DashDistance,
                    DashRotationSpeed = dummyData.DashRotationSpeed,
                    DashCooldown = dummyData.DashCooldown,
                    DashDuration = dummyData.DashDuration
                };
                _dummyCube.SetStats(dummyData.Stats, dummyData.Actions, dummyData.AI);
                _dummyCube.SetBorderColor(dummyData.BorderColor, dummyData.BorderThickness);
                engine.RegisterObject(_dummyCube);
                _dummyCube.EquipWeapon(new SimpleWPFGame.Combat.Sword());
                Logger.Log($"Dummy: {dummyData.Name} | HP={dummyData.Stats.HP:F2}", LogLevel.Info);
            }

            // Equip player with sword
            _blueCube?.EquipWeapon(new SimpleWPFGame.Combat.Sword());

            // Start engine
            engine.Start();

            // Initialize 3D scene
            Initialize3D();

            // Demo VFX effects
            var vfx = VFXSystem.Instance;
            double gw = settings.GameScreenWidth;
            double gh = settings.GameScreenHeight;

            vfx.CreateFire(new System.Windows.Vector(gw * 0.15, gh * 0.85), FirePreset.Torch);
            vfx.CreateFire(new System.Windows.Vector(gw * 0.85, gh * 0.85), FirePreset.Torch);
            vfx.CreateSmoke(new System.Windows.Vector(gw * 0.15, gh * 0.7), SmokePreset.Steam);
            vfx.CreateSmoke(new System.Windows.Vector(gw * 0.85, gh * 0.7), SmokePreset.Steam);
            vfx.CreateLightning(
                new System.Windows.Vector(gw * 0.3, gh * 0.05),
                new System.Windows.Vector(gw * 0.35, gh * 0.3),
                LightningPreset.Standard);
            vfx.CreateLaser(
                new System.Windows.Vector(gw * 0.5, gh * 0.45),
                new System.Windows.Vector(gw * 0.7, gh * 0.45),
                LaserPreset.Standard);
            vfx.CreateWater(new System.Windows.Vector(gw * 0.5, gh * 0.15), WaterPreset.Fountain);

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

            // Player stats
            if (_blueCube != null)
            {
                var s = _blueCube.Stats;
                StatHP.Text = $"{s.HP * 100:F2}%";
                StatDefence.Text = $"{s.Defence * 100:F2}%";
                StatAttack.Text = $"{s.Attack * 100:F2}%";
                StatAttackSpeed.Text = $"{s.AttackSpeed:F2}x";
                StatResistance.Text = $"{s.Resistance * 100:F2}%";
                StatDodge.Text = $"{s.DodgeChance * 100:F2}%";
                StatCritChance.Text = $"{s.CriticalChance * 100:F2}%";
                StatCritDamage.Text = $"{s.CriticalDamage * 100:F2}%";
                StatMainDmg.Text = $"{s.MainDamage * 100:F2}%";
                StatSkillDmg.Text = $"{s.SkillDamage * 100:F2}%";
                StatParryDmg.Text = $"{s.ParryDamage * 100:F2}%";
                StatCounterDmg.Text = $"{s.CounterDamage * 100:F2}%";
            }

            // Enemy/NPC stats
            if (_redCube != null)
            {
                EnemyHP.Text = $"HP: {_redCube.Stats.HP * 100:F2}%";
                EnemyAtk.Text = $"Attack: {_redCube.Stats.Attack * 100:F2}%";
                EnemyDef.Text = $"Defence: {_redCube.Stats.Defence * 100:F2}%";
                EnemyCrit.Text = $"Crit: {_redCube.Stats.CriticalChance * 100:F2}%";
                EnemyDodge.Text = $"Dodge: {_redCube.Stats.DodgeChance * 100:F2}%";
                EnemyDashText.Text = $"Dash: {(_redCube.Actions.DashEnabled ? "ON" : "OFF")}";
            }
            if (_greenCube != null)
            {
                GreenHP.Text = $"HP: {_greenCube.Stats.HP * 100:F2}%";
                GreenAtk.Text = $"Attack: {_greenCube.Stats.Attack * 100:F2}%";
                GreenDashText.Text = $"Dash: {(_greenCube.Actions.DashEnabled ? "ON" : "OFF")}";
            }
            if (_dummyCube != null)
            {
                var ds = _dummyCube.Stats;
                var dState = _dummyCube.Combat.State;
                DummyHP.Text = $"HP: {ds.HP * 100:F1}%";
                DummyState.Text = $"State: {dState}";
                DummyState.Foreground = dState == SimpleWPFGame.Combat.CombatState.Parrying
                    ? Brushes.Yellow : dState == SimpleWPFGame.Combat.CombatState.Blocking
                    ? Brushes.DodgerBlue : Brushes.Gray;
            }

            // Combat state
            if (_blueCube != null)
            {
                var cs = _blueCube.Combat;
                CombatStateText.Text = $"Combat: {cs.State} F:{cs.CurrentFrame}";
                CombatStateText.Foreground = cs.State == SimpleWPFGame.Combat.CombatState.Attacking
                    ? Brushes.Red : cs.State == SimpleWPFGame.Combat.CombatState.Dodging
                    ? Brushes.LimeGreen : cs.State == SimpleWPFGame.Combat.CombatState.Parrying
                    ? Brushes.Yellow : Brushes.White;
                WeaponText.Text = $"Weapon: {cs.EquippedWeapon?.Name ?? "None"}";
            }

            // Time
            var elapsed = TimeSpan.FromSeconds(engine.ElapsedTime);
            TimeDisplay.Text = elapsed.ToString(@"hh\:mm\:ss");

            StatusText.Text = _activeTab != "StatsPanel"
                ? $"Menu: {_activeTab}"
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
            if (controller.IsStartPressed && _activeTab == "StatsPanel")
                OpenOptionsMenu();
        }
    }

    // --- Options Menu (via tabs) ---

    private void OpenOptionsMenu()
    {
        SwitchTab("OptionsPanel");
        Logger.Log("Options opened via tab", LogLevel.Info);
    }

    private void CloseOptionsMenu()
    {
        SwitchTab("StatsPanel");
    }

    private void OptionsMenuOverlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
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

    private void Size720_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowSize(1280, 720);
    }

    private void Size900_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowSize(1600, 900);
    }

    private void Size1080_Click(object sender, RoutedEventArgs e)
    {
        ApplyWindowSize(1920, 1080);
    }

    private void ApplyWindowSize(int width, int height)
    {
        try
        {
            var settings = GameSettings.Instance;
            settings.WindowWidth = width;
            settings.WindowHeight = height;
            settings.GameScreenWidth = width - (settings.GameScreenBorderInset * 2);
            settings.GameScreenHeight = height - (settings.GameScreenBorderInset * 2) - 65;
            settings.Save();

            _targetWidth = width;
            _targetHeight = height;

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            int x = (int)((screenW - width) / 2);
            int y = (int)((screenH - height) / 2);
            SetWindowPos(helper.Handle, IntPtr.Zero, x, y, width, height, SWP_FRAMECHANGED);

            Width = width;
            Height = height;
            GameCanvas.Width = settings.GameScreenWidth;
            GameCanvas.Height = settings.GameScreenHeight;
            _gameHost!.Width = settings.GameScreenWidth;
            _gameHost.Height = settings.GameScreenHeight;

            WindowSizeText.Text = $"{width}x{height} | {settings.GameScreenWidth}x{settings.GameScreenHeight}";
            ConsoleWrite($"Window: {width}x{height}", "#FF00FF88");
            Logger.Log($"Window resized to {width}x{height}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to resize window", ex);
        }
    }

    private void OptionsSaveClose_Click(object sender, RoutedEventArgs e)
    {
        GameSettings.Instance.Save();
        ConsoleWrite("Settings saved", "#FF00FF88");
        SwitchTab("StatsPanel");
    }

    private void OptionsClose_Click(object sender, RoutedEventArgs e)
    {
        SwitchTab("StatsPanel");
    }

    // --- Input ---

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_activeTab == "OptionsPanel")
        {
            if (e.Key == System.Windows.Input.Key.Escape || e.Key == System.Windows.Input.Key.O)
            {
                GameSettings.Instance.Save();
                SwitchTab("StatsPanel");
                e.Handled = true;
            }
            return;
        }

        if (_activeTab != "StatsPanel")
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                SwitchTab("StatsPanel");
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
            case System.Windows.Input.Key.F6:
                ToggleHitboxDebug();
                break;
            case System.Windows.Input.Key.F5:
                QuickSave();
                break;
            case System.Windows.Input.Key.F7:
                Toggle3DView();
                break;
            case System.Windows.Input.Key.F9:
                QuickLoad();
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

    private void ToggleHitboxDebug()
    {
        _hitboxDebug = !_hitboxDebug;
        if (_blueCube != null) _blueCube.Combat.ShowHitboxDebug = _hitboxDebug;
        if (_redCube != null) _redCube.Combat.ShowHitboxDebug = _hitboxDebug;
        if (_greenCube != null) _greenCube.Combat.ShowHitboxDebug = _hitboxDebug;
        if (_dummyCube != null) _dummyCube.Combat.ShowHitboxDebug = _hitboxDebug;
        ConsoleWrite(_hitboxDebug ? "Hitbox debug: ON [F6]" : "Hitbox debug: OFF [F6]", "#FF00FF88");
        Logger.Log($"Hitbox debug: {_hitboxDebug}", LogLevel.Info);
    }

    private void QuickSave()
    {
        var sm = SaveManager.Instance;
        if (sm.Save(1, _blueCube, _redCube, _greenCube, "Quick Save"))
        {
            ConsoleWrite("Quick Saved [F5] -> Slot 1", "#FF00FF88");
            StatusText.Text = "Game saved";
        }
        else
        {
            ConsoleWrite("Save failed!", "#FFFF4444");
        }
    }

    private void QuickLoad()
    {
        var sm = SaveManager.Instance;
        var data = sm.Load(1);
        if (data != null && sm.ApplyLoadedState(data, _blueCube, _redCube, _greenCube))
        {
            ConsoleWrite("Quick Loaded [F9] <- Slot 1", "#FF00FF88");
            StatusText.Text = "Game loaded";
        }
        else
        {
            ConsoleWrite("Load failed or no save in slot 1!", "#FFFF4444");
        }
    }

    private void Initialize3D()
    {
        try
        {
            var settings = GameSettings.Instance;
            _scene3D = new Scene3D(settings.GameScreenWidth, settings.GameScreenHeight);

            Viewport3D.Children.Clear();
            Viewport3D.Camera = _scene3D.Camera;
            Viewport3D.Children.Add(_scene3D.Visual);

            Update3DCamera();
            GameWorld3D.Instance.Initialize(_scene3D, settings.GameScreenWidth, settings.GameScreenHeight);

            RegisterCubesIn3D();

            if (settings.Show3DView)
                Toggle3DView();

            Logger.Log("3D world initialized with arena", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to initialize 3D", ex);
        }
    }

    private void RegisterCubesIn3D()
    {
        var world = GameWorld3D.Instance;
        if (_blueCube != null)
            world.RegisterCube(_blueCube, _blueCube.GetHashCode(), Colors.DodgerBlue, "Player");
        if (_redCube != null)
            world.RegisterCube(_redCube, _redCube.GetHashCode(), Colors.Red, "Enemy");
        if (_greenCube != null)
            world.RegisterCube(_greenCube, _greenCube.GetHashCode(), Colors.LimeGreen, "Ally");
        if (_dummyCube != null)
            world.RegisterCube(_dummyCube, _dummyCube.GetHashCode(), Colors.Gold, "Dummy");
    }

    private void Toggle3DView()
    {
        var settings = GameSettings.Instance;
        settings.Show3DView = !settings.Show3DView;

        if (settings.Show3DView)
        {
            Viewport3D.Visibility = Visibility.Visible;
            ConsoleWrite("3D View: ON [F7]", "#FF00FF88");
        }
        else
        {
            Viewport3D.Visibility = Visibility.Collapsed;
            ConsoleWrite("3D View: OFF [F7]", "#FF00FF88");
        }
        Logger.Log($"3D view: {settings.Show3DView}", LogLevel.Info);
    }

    private void Update3DCamera()
    {
        if (_scene3D == null) return;
        var settings = GameSettings.Instance;
        double radX = settings.CameraAngleX * Math.PI / 180;
        double radY = settings.CameraAngleY * Math.PI / 180;
        double dist = settings.CameraDistance;

        double x = dist * Math.Cos(radX) * Math.Sin(radY);
        double y = dist * Math.Sin(radX);
        double z = dist * Math.Cos(radX) * Math.Cos(radY);

        _scene3D.SetCameraPosition(x, y, z);
        _scene3D.SetCameraLookAt(0, 0, 0);
    }

    private void Btn3DToggle_Click(object sender, RoutedEventArgs e) => Toggle3DView();

    private void Btn3DResetCamera_Click(object sender, RoutedEventArgs e)
    {
        var settings = GameSettings.Instance;
        settings.CameraAngleX = 30;
        settings.CameraAngleY = 0;
        settings.CameraDistance = 10;
        Update3DCamera();
        Text3DCameraDist.Text = settings.CameraDistance.ToString("F1");
        Text3DCameraAngleX.Text = settings.CameraAngleX.ToString("F1");
        Text3DCameraAngleY.Text = settings.CameraAngleY.ToString("F1");
        ConsoleWrite("3D camera reset", "#FF00BFFF");
    }

    private void Viewport3D_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _3DMouseDragging = true;
        _3DLastMousePos = e.GetPosition(Viewport3D);
        Viewport3D.CaptureMouse();
    }

    private void Viewport3D_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _3DMouseDragging = false;
        Viewport3D.ReleaseMouseCapture();
    }

    private void Viewport3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_3DMouseDragging) return;
        var pos = e.GetPosition(Viewport3D);
        double dx = pos.X - _3DLastMousePos.X;
        double dy = pos.Y - _3DLastMousePos.Y;
        _3DLastMousePos = pos;

        var settings = GameSettings.Instance;
        settings.CameraAngleY += dx * 0.5;
        settings.CameraAngleX = Math.Clamp(settings.CameraAngleX + dy * 0.5, -89, 89);
        Update3DCamera();
    }

    private void Viewport3D_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        var settings = GameSettings.Instance;
        settings.CameraDistance = Math.Clamp(settings.CameraDistance - e.Delta * 0.01, 2, 50);
        Update3DCamera();
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

        switch (cmd)
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
                ConsoleWrite("  window <w> <h> - Set window size", "#FF888888");
                ConsoleWrite("  save [slot]    - Save game (F5)", "#FF888888");
                ConsoleWrite("  load [slot]    - Load game (F9)", "#FF888888");
                ConsoleWrite("  saves          - List save slots", "#FF888888");
                ConsoleWrite("  delsave <slot> - Delete save", "#FF888888");
                ConsoleWrite("  vfx fire       - Spawn fire", "#FF888888");
                ConsoleWrite("  vfx lightning  - Spawn lightning", "#FF888888");
                ConsoleWrite("  vfx laser      - Spawn laser", "#FF888888");
                ConsoleWrite("  vfx smoke      - Spawn smoke", "#FF888888");
                ConsoleWrite("  vfx water      - Spawn water", "#FF888888");
                ConsoleWrite("  vfx clear      - Remove all VFX", "#FF888888");
                ConsoleWrite("  vfx info       - Show VFX stats", "#FF888888");
                ConsoleWrite("  attack         - Player attack", "#FF888888");
                ConsoleWrite("  block          - Player block", "#FF888888");
                ConsoleWrite("  parry          - Player parry", "#FF888888");
                ConsoleWrite("  dodge          - Player dodge", "#FF888888");
                ConsoleWrite("  combo          - Test 3-hit combo", "#FF888888");
                ConsoleWrite("  dummy          - Reset dummy HP", "#FF888888");
                ConsoleWrite("  dmg <amt>      - Damage dummy", "#FF888888");
                ConsoleWrite("  combatinfo     - Show combat stats", "#FF888888");
                ConsoleWrite("  3d             - Toggle 3D view (F7)", "#FF00BFFF");
                ConsoleWrite("  3d hitboxes    - Toggle 3D hitboxes", "#FF00BFFF");
                ConsoleWrite("  3d bounds      - Toggle 3D bounds", "#FF00BFFF");
                ConsoleWrite("  3d grid        - Toggle spatial hash", "#FF00BFFF");
                ConsoleWrite("  3d reset       - Reset 3D camera", "#FF00BFFF");
                ConsoleWrite("  3d info        - Show 3D stats", "#FF00BFFF");
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

            case "window":
                if (args.Length >= 2 && int.TryParse(args[0], out var ww) && int.TryParse(args[1], out var hh))
                { ApplyWindowSize(ww, hh); }
                else ConsoleWrite("Usage: window <width> <height>", "#FFFF4444");
                break;

            case "log":
                Logger.Log($"Console: {string.Join(' ', args)}", LogLevel.Info);
                ConsoleWrite("Logged", "#FF00FF88");
                break;

            case "settings":
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

            case "save":
                if (args.Length > 0 && int.TryParse(args[0], out var saveSlot))
                {
                    if (SaveManager.Instance.Save(saveSlot, _blueCube, _redCube, _greenCube))
                        ConsoleWrite($"Saved to slot {saveSlot}", "#FF00FF88");
                    else
                        ConsoleWrite($"Failed to save to slot {saveSlot}", "#FFFF4444");
                }
                else
                {
                    if (SaveManager.Instance.Save(1, _blueCube, _redCube, _greenCube))
                        ConsoleWrite("Saved to slot 1 (quick save)", "#FF00FF88");
                    else
                        ConsoleWrite("Failed to save", "#FFFF4444");
                }
                break;

            case "load":
                if (args.Length > 0 && int.TryParse(args[0], out var loadSlot))
                {
                    var loadData = SaveManager.Instance.Load(loadSlot);
                    if (loadData != null && SaveManager.Instance.ApplyLoadedState(loadData, _blueCube, _redCube, _greenCube))
                        ConsoleWrite($"Loaded from slot {loadSlot}", "#FF00FF88");
                    else
                        ConsoleWrite($"Failed to load from slot {loadSlot}", "#FFFF4444");
                }
                else
                {
                    var loadData = SaveManager.Instance.Load(1);
                    if (loadData != null && SaveManager.Instance.ApplyLoadedState(loadData, _blueCube, _redCube, _greenCube))
                        ConsoleWrite("Loaded from slot 1 (quick load)", "#FF00FF88");
                    else
                        ConsoleWrite("Failed to load", "#FFFF4444");
                }
                break;

            case "saves":
                var slots = SaveManager.Instance.GetAllSlots();
                ConsoleWrite("Save Slots:", "#FF00D4FF");
                foreach (var slot in slots)
                {
                    if (slot.Exists)
                        ConsoleWrite($"  [{slot.SlotIndex}] {slot.SlotName} - {slot.Timestamp} ({slot.GameTime:F1}s)", "#FF888888");
                    else
                        ConsoleWrite($"  [{slot.SlotIndex}] <empty>", "#FF555555");
                }
                break;

            case "delsave":
                if (args.Length > 0 && int.TryParse(args[0], out var delSlot))
                {
                    if (SaveManager.Instance.DeleteSave(delSlot))
                        ConsoleWrite($"Deleted slot {delSlot}", "#FF00FF88");
                    else
                        ConsoleWrite($"Slot {delSlot} not found", "#FFFF4444");
                }
                else ConsoleWrite("Usage: delsave <slot>", "#FFFF4444");
                break;

            case "vfx fire":
                {
                    var vfx = VFXSystem.Instance;
                    var gs = GameSettings.Instance;
                    double gw = gs.GameScreenWidth;
                    double gh = gs.GameScreenHeight;
                    if (args.Length >= 2 && double.TryParse(args[0], out double fx) && double.TryParse(args[1], out double fy))
                        vfx.CreateFire(new System.Windows.Vector(fx, fy), FirePreset.Standard);
                    else
                        vfx.CreateFire(new System.Windows.Vector(gw / 2, gh * 0.8), FirePreset.Standard);
                    ConsoleWrite("Fire created", "#FFFF8800");
                }
                break;

            case "vfx lightning":
                {
                    var vfx = VFXSystem.Instance;
                    var gs = GameSettings.Instance;
                    double gw = gs.GameScreenWidth;
                    double gh = gs.GameScreenHeight;
                    vfx.CreateLightningBolt(
                        new System.Windows.Vector(gw * 0.3, gh * 0.05),
                        new System.Windows.Vector(gw * 0.4, gh * 0.4));
                    ConsoleWrite("Lightning created", "#FF88AAFF");
                }
                break;

            case "vfx laser":
                {
                    var vfx = VFXSystem.Instance;
                    var gs = GameSettings.Instance;
                    double gw = gs.GameScreenWidth;
                    double gh = gs.GameScreenHeight;
                    vfx.CreateLaser(
                        new System.Windows.Vector(gw * 0.3, gh * 0.5),
                        new System.Windows.Vector(gw * 0.7, gh * 0.5),
                        LaserPreset.Standard);
                    ConsoleWrite("Laser created", "#FFFF4444");
                }
                break;

            case "vfx smoke":
                {
                    var vfx = VFXSystem.Instance;
                    var gs = GameSettings.Instance;
                    double gw = gs.GameScreenWidth;
                    double gh = gs.GameScreenHeight;
                    vfx.CreateSmoke(new System.Windows.Vector(gw * 0.5, gh * 0.7), SmokePreset.Cloud);
                    ConsoleWrite("Smoke created", "#FF888888");
                }
                break;

            case "vfx water":
                {
                    var vfx = VFXSystem.Instance;
                    var gs = GameSettings.Instance;
                    double gw = gs.GameScreenWidth;
                    double gh = gs.GameScreenHeight;
                    vfx.CreateWater(new System.Windows.Vector(gw * 0.5, gh * 0.15), WaterPreset.Fountain);
                    ConsoleWrite("Water created", "#FF4488FF");
                }
                break;

            case "vfx clear":
                VFXSystem.Instance.RemoveAll();
                ConsoleWrite("All VFX cleared", "#FF00FF88");
                break;

            case "vfx info":
                var vi = VFXSystem.Instance;
                ConsoleWrite($"VFX: {vi.ActiveEffectCount} effects, {vi.TotalParticleCount} particles", "#FF00FF88");
                break;

            case "attack":
                if (_blueCube != null && _blueCube.Combat.CanAct)
                {
                    _blueCube.Combat.TryAttack(_blueCube.Position, _blueCube.FacingAngle);
                    ConsoleWrite("Attack!", "#FFFF4444");
                }
                else ConsoleWrite("Cannot attack now", "#FFFF4444");
                break;

            case "block":
                if (_blueCube != null && _blueCube.Combat.CanAct)
                {
                    _blueCube.Combat.TryBlock();
                    ConsoleWrite("Blocking", "#FF4488FF");
                }
                break;

            case "parry":
                if (_blueCube != null && _blueCube.Combat.CanAct)
                {
                    _blueCube.Combat.TryParry();
                    ConsoleWrite("Parry!", "#FFFFFF00");
                }
                break;

            case "dodge":
                if (_blueCube != null && _blueCube.Combat.CanAct)
                {
                    _blueCube.Combat.TryDodge();
                    ConsoleWrite("Dodge!", "#FF00FF88");
                }
                break;

            case "dummy":
                if (_dummyCube != null)
                {
                    var dd = CharSettings.GetCharacter("DummyCube");
                    if (dd != null)
                    {
                        _dummyCube.Stats.HP = dd.Stats.HP;
                        ConsoleWrite($"Dummy HP reset to {dd.Stats.HP * 100:F0}%", "#FFFFD700");
                    }
                }
                else ConsoleWrite("No dummy found", "#FFFF4444");
                break;

            case "dmg":
                if (_dummyCube != null && args.Length > 0 && double.TryParse(args[0], out double dmg))
                {
                    _dummyCube.Stats.HP = Math.Max(0, _dummyCube.Stats.HP - (float)(dmg / 100.0));
                    var pos = new System.Windows.Vector(
                        _dummyCube.Position.X + _dummyCube.Width / 2,
                        _dummyCube.Position.Y);
                    DamageNumberSystem.Instance.Add(pos, dmg, dmg > 20);
                    ConsoleWrite($"Dummy took {dmg:F0} damage. HP: {_dummyCube.Stats.HP * 100:F1}%", "#FFFF4444");
                }
                else ConsoleWrite("Usage: dmg <amount>", "#FFFF4444");
                break;

            case "combatinfo":
                if (_blueCube != null)
                {
                    var cs = _blueCube.Stats;
                    ConsoleWrite($"=== Player Combat Stats ===", "#FF00D4FF");
                    ConsoleWrite($"  ATK: {cs.Attack * 100:F1}% | DEF: {cs.Defence * 100:F1}% | SPD: {cs.AttackSpeed:F2}x", "#FF888888");
                    ConsoleWrite($"  Crit: {cs.CriticalChance * 100:F1}% | CritDmg: {cs.CriticalDamage * 100:F0}%", "#FF888888");
                    ConsoleWrite($"  Dodge: {cs.DodgeChance * 100:F1}% | Parry: {cs.ParryChance * 100:F1}% | Block: {cs.BlockChance * 100:F1}%", "#FF888888");
                    ConsoleWrite($"  Pen: {cs.Penetration * 100:F1}% | ArmorPen: {cs.ArmorPen * 100:F1}% | LS: {cs.LifeSteal * 100:F1}%", "#FF888888");
                    ConsoleWrite($"  Weapon: {_blueCube.Combat.EquippedWeapon?.Name ?? "None"}", "#FF888888");
                    ConsoleWrite($"  State: {_blueCube.Combat.State} | Frame: {_blueCube.Combat.CurrentFrame}", "#FF888888");
                    if (_dummyCube != null)
                        ConsoleWrite($"  Dummy HP: {_dummyCube.Stats.HP * 100:F1}%", "#FFFFD700");
                }
                break;

            case "clear":
                ConsoleOutput.Text = "";
                break;

            case "3d":
                Toggle3DView();
                break;

            case "3d hitboxes":
                Rendering3D.DebugRenderer3D.Instance.ToggleHitboxes();
                ConsoleWrite("3D hitbox debug toggled", "#FF00BFFF");
                break;

            case "3d bounds":
                Rendering3D.DebugRenderer3D.Instance.ToggleBounds();
                ConsoleWrite("3D bounds debug toggled", "#FF00BFFF");
                break;

            case "3d grid":
                Rendering3D.DebugRenderer3D.Instance.ToggleSpatialHash();
                ConsoleWrite("3D spatial hash toggled", "#FF00BFFF");
                break;

            case "3d reset":
                Btn3DResetCamera_Click(this, new RoutedEventArgs());
                break;

            case "3d info":
                ConsoleWrite($"3D Objects: {Rendering3D.MeshRenderer.Instance.ObjectCount}", "#FF00BFFF");
                ConsoleWrite($"3D Hitboxes active: {Rendering3D.Hitbox3DManager.Instance.ActiveHitboxCount}", "#FF00BFFF");
                ConsoleWrite($"3D Frame: {Rendering3D.FramePerfectTimer.Instance.FixedFrame}", "#FF00BFFF");
                ConsoleWrite($"Camera: dist={GameSettings.Instance.CameraDistance:F1} angX={GameSettings.Instance.CameraAngleX:F0} angY={GameSettings.Instance.CameraAngleY:F0}", "#FF00BFFF");
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
        // Force exact window size, centered on screen
        try
        {
            var settings = GameSettings.Instance;
            _targetWidth = settings.WindowWidth;
            _targetHeight = settings.WindowHeight;

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            int style = GetWindowLong(helper.Handle, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME);
            SetWindowLong(helper.Handle, GWL_STYLE, style);

            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;
            int x = (int)((screenW - _targetWidth) / 2);
            int y = (int)((screenH - _targetHeight) / 2);
            SetWindowPos(helper.Handle, IntPtr.Zero, x, y, _targetWidth, _targetHeight, SWP_FRAMECHANGED);

            Logger.Log($"Window: {_targetWidth}x{_targetHeight} centered on screen", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to set exact window size", ex);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        TooltipManager.Instance.UpdatePosition();
    }

    // --- Tooltip Handlers ---

    private void StatHover_Enter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element && element.Tag is string tooltip)
            TooltipManager.Instance.Show(tooltip);
    }

    private void ControlMove_Enter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element && element.Tag is string tooltip)
            TooltipManager.Instance.Show(tooltip);
    }

    private void DashPlayerHover_Enter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        string tooltip = _blueCube?.Actions.DashEnabled == true
            ? TooltipDescriptions.DashEnabled
            : TooltipDescriptions.DashDisabled;
        TooltipManager.Instance.Show(tooltip);
    }

    private void DashEnemyHover_Enter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        string tooltip = _redCube?.Actions.DashEnabled == true
            ? TooltipDescriptions.DashEnabled
            : TooltipDescriptions.DashDisabled;
        TooltipManager.Instance.Show(tooltip);
    }

    private void DashNPCHover_Enter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        string tooltip = _greenCube?.Actions.DashEnabled == true
            ? TooltipDescriptions.DashEnabled
            : TooltipDescriptions.DashDisabled;
        TooltipManager.Instance.Show(tooltip);
    }

    private void Tooltip_Leave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        TooltipManager.Instance.Hide();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // --- Tab Navigation ---

    private void InitializeTabIcons()
    {
        var tabIcons = new (Button btn, string symbol, string label, string c1, string c2, string glow)[]
        {
            (TabStats, GameIcons.Stats.Symbol, "Stats", GameIcons.Stats.Color1, GameIcons.Stats.Color2, GameIcons.Stats.Glow),
            (TabNewGame, GameIcons.NewGame.Symbol, "New", GameIcons.NewGame.Color1, GameIcons.NewGame.Color2, GameIcons.NewGame.Glow),
            (TabSave, GameIcons.Save.Symbol, "Save", GameIcons.Save.Color1, GameIcons.Save.Color2, GameIcons.Save.Glow),
            (TabLoad, GameIcons.Load.Symbol, "Load", GameIcons.Load.Color1, GameIcons.Load.Color2, GameIcons.Load.Glow),
            (TabOptions, GameIcons.Options.Symbol, "Options", GameIcons.Options.Color1, GameIcons.Options.Color2, GameIcons.Options.Glow),
            (Tab3D, GameIcons.View3D.Symbol, "3D", GameIcons.View3D.Color1, GameIcons.View3D.Color2, GameIcons.View3D.Glow),
            (TabExit, GameIcons.Exit.Symbol, "Exit", GameIcons.Exit.Color1, GameIcons.Exit.Color2, GameIcons.Exit.Glow),
        };

        foreach (var (btn, symbol, label, c1, c2, glow) in tabIcons)
        {
            var icon = GameIcons.CreateIconButton(symbol, label, c1, c2, glow, 60);
            btn.Content = icon.Content;
            btn.Template = icon.Template;
            btn.Width = icon.Width;
            btn.Height = icon.Height;
            btn.Background = icon.Background;
            btn.BorderBrush = icon.BorderBrush;
            btn.BorderThickness = icon.BorderThickness;
            btn.Margin = icon.Margin;
        }

        // Set default active tab
        SwitchTab("StatsPanel");
    }

    private void TabBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string panelName)
        {
            if (panelName == "ExitPanel")
            {
                SwitchTab(panelName);
                return;
            }
            SwitchTab(panelName);
        }
    }

    private string _activeTab = "";

    private void SwitchTab(string panelName)
    {
        _activeTab = panelName;

        StatsPanel.Visibility = panelName == "StatsPanel" ? Visibility.Visible : Visibility.Collapsed;
        NewGamePanel.Visibility = panelName == "NewGamePanel" ? Visibility.Visible : Visibility.Collapsed;
        SavePanel.Visibility = panelName == "SavePanel" ? Visibility.Visible : Visibility.Collapsed;
        LoadPanel.Visibility = panelName == "LoadPanel" ? Visibility.Visible : Visibility.Collapsed;
        OptionsPanel.Visibility = panelName == "OptionsPanel" ? Visibility.Visible : Visibility.Collapsed;
        View3DPanel.Visibility = panelName == "View3DPanel" ? Visibility.Visible : Visibility.Collapsed;
        ExitPanel.Visibility = panelName == "ExitPanel" ? Visibility.Visible : Visibility.Collapsed;

        if (panelName == "SavePanel") PopulateSaveSlots();
        if (panelName == "LoadPanel") PopulateLoadSlots();
        if (panelName == "OptionsPanel") SyncOptionsUI();
        if (panelName == "View3DPanel")
        {
            GameScreenBorder.Visibility = Visibility.Visible;
            Text3DObjectCount.Text = MeshRenderer.Instance.ObjectCount.ToString();
            Text3DCameraDist.Text = GameSettings.Instance.CameraDistance.ToString("F1");
            Text3DCameraAngleX.Text = GameSettings.Instance.CameraAngleX.ToString("F1");
            Text3DCameraAngleY.Text = GameSettings.Instance.CameraAngleY.ToString("F1");
        }
        else if (panelName != "StatsPanel") GameScreenBorder.Visibility = Visibility.Collapsed;
        else GameScreenBorder.Visibility = Visibility.Visible;

        StatusText.Text = panelName switch
        {
            "StatsPanel" => "Viewing character stats",
            "NewGamePanel" => "Start a new game",
            "SavePanel" => "Select a save slot",
            "LoadPanel" => "Select a save to load",
            "OptionsPanel" => "Adjust game settings",
            "View3DPanel" => "3D View Settings [F7 to toggle]",
            "ExitPanel" => "Exit confirmation",
            _ => "Game running"
        };
    }

    private void SyncOptionsUI()
    {
        var s = GameSettings.Instance;
        QualityLowBtn.IsEnabled = s.GraphicsQuality != GraphicsQuality.Low;
        QualityMedBtn.IsEnabled = s.GraphicsQuality != GraphicsQuality.Medium;
        QualityHighBtn.IsEnabled = s.GraphicsQuality != GraphicsQuality.High;
        VSyncToggleBtn.Content = s.VSync ? "ON" : "OFF";
        ShadowsToggleBtn.Content = s.Shadows ? "ON" : "OFF";
        MasterVolumeSlider.Value = s.MasterVolume * 100;
        MusicVolumeSlider.Value = s.MusicVolume * 100;
        SfxVolumeSlider.Value = s.SfxVolume * 100;
        VfxVolumeSlider.Value = s.VfxVolume * 100;
        Size720Btn.IsEnabled = s.WindowWidth != 1280 || s.WindowHeight != 720;
        Size900Btn.IsEnabled = s.WindowWidth != 1600 || s.WindowHeight != 900;
        Size1080Btn.IsEnabled = s.WindowWidth != 1920 || s.WindowHeight != 1080;
    }

    // --- Save/Load Panels ---

    private void PopulateSaveSlots()
    {
        SaveSlotsContainer.Children.Clear();
        var sm = SaveManager.Instance;
        for (int i = 1; i <= 5; i++)
        {
            bool exists = sm.SaveExists(i);
            string info = exists ? $"Slot {i} - Has data" : $"Slot {i} - Empty";
            string color = exists ? "#FF888888" : "#FF555555";

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            var label = new TextBlock { Text = info, Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)), FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Width = 200 };

            var saveBtn = new Button
            {
                Content = "Save Here",
                Width = 100, Height = 30,
                Style = (System.Windows.Style)FindResource("GameButton"),
                Tag = i,
                Margin = new Thickness(10, 0, 0, 0)
            };
            saveBtn.Click += SaveSlot_Click;

            var delBtn = new Button
            {
                Content = "Delete",
                Width = 70, Height = 30,
                Style = (System.Windows.Style)FindResource("GameButton"),
                Tag = i,
                Margin = new Thickness(6, 0, 0, 0)
            };
            delBtn.Click += DeleteSlot_Click;

            row.Children.Add(label);
            row.Children.Add(saveBtn);
            if (exists) row.Children.Add(delBtn);
            SaveSlotsContainer.Children.Add(row);
        }
    }

    private void PopulateLoadSlots()
    {
        LoadSlotsContainer.Children.Clear();
        var sm = SaveManager.Instance;
        for (int i = 1; i <= 5; i++)
        {
            bool exists = sm.SaveExists(i);
            string info = exists ? $"Slot {i} - Has data" : $"Slot {i} - Empty";
            string color = exists ? "#FF888888" : "#FF555555";

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            var label = new TextBlock { Text = info, Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)), FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Width = 200 };

            var loadBtn = new Button
            {
                Content = "Load",
                Width = 100, Height = 30,
                Style = (System.Windows.Style)FindResource("GameButton"),
                Tag = i,
                IsEnabled = exists,
                Margin = new Thickness(10, 0, 0, 0)
            };
            loadBtn.Click += LoadSlot_Click;

            row.Children.Add(label);
            row.Children.Add(loadBtn);
            LoadSlotsContainer.Children.Add(row);
        }
    }

    private void SaveSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int slot)
        {
            if (SaveManager.Instance.Save(slot, _blueCube, _redCube, _greenCube))
            {
                SaveStatus.Text = $"Saved to slot {slot}!";
                SaveStatus.Foreground = new SolidColorBrush(System.Windows.Media.Colors.LimeGreen);
                PopulateSaveSlots();
            }
            else
            {
                SaveStatus.Text = $"Failed to save to slot {slot}";
                SaveStatus.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
    }

    private void LoadSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int slot)
        {
            var data = SaveManager.Instance.Load(slot);
            if (data != null && SaveManager.Instance.ApplyLoadedState(data, _blueCube, _redCube, _greenCube))
            {
                LoadStatus.Text = $"Loaded from slot {slot}!";
                LoadStatus.Foreground = new SolidColorBrush(System.Windows.Media.Colors.LimeGreen);
                SwitchTab("StatsPanel");
            }
            else
            {
                LoadStatus.Text = $"Failed to load from slot {slot}";
                LoadStatus.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
    }

    private void DeleteSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int slot)
        {
            SaveManager.Instance.DeleteSave(slot);
            SaveStatus.Text = $"Slot {slot} deleted";
            SaveStatus.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Orange);
            PopulateSaveSlots();
        }
    }

    // --- New Game / Exit ---

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        // Reset cubes to defaults
        var pData = CharSettings.GetCharacter("PlayerCube");
        var eData = CharSettings.GetCharacter("EnemyCube");
        var gData = CharSettings.GetCharacter("GreenCube");

        if (pData != null && _blueCube != null)
        {
            _blueCube.Position = new System.Windows.Vector(pData.StartX, pData.StartY);
            _blueCube.Velocity = new System.Windows.Vector(0, 0);
            _blueCube.Stats = new Config.CharacterStats { HP = pData.Stats.HP, Defence = pData.Stats.Defence, Attack = pData.Stats.Attack };
        }
        if (eData != null && _redCube != null)
        {
            _redCube.Position = new System.Windows.Vector(eData.StartX, eData.StartY);
            _redCube.Velocity = new System.Windows.Vector(0, 0);
        }
        if (gData != null && _greenCube != null)
        {
            _greenCube.Position = new System.Windows.Vector(gData.StartX, gData.StartY);
            _greenCube.Velocity = new System.Windows.Vector(0, 0);
        }

        SwitchTab("StatsPanel");
        ConsoleWrite("New game started", "#FF00FF88");
    }

    private void SaveExit_Click(object sender, RoutedEventArgs e)
    {
        SaveManager.Instance.Save(1, _blueCube, _redCube, _greenCube, "Auto Save");
        Close();
    }

    private void ForceExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CancelExit_Click(object sender, RoutedEventArgs e)
    {
        SwitchTab("StatsPanel");
    }
}
