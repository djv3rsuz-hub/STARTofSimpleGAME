using System.Windows;
using System.Windows.Threading;
using SharpDX.XInput;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame.Game;

public sealed class ControllerManager
{
    private static readonly Lazy<ControllerManager> _instance = new(() => new ControllerManager());
    public static ControllerManager Instance => _instance.Value;

    private Controller? _controller;
    private ControllerType _activeControllerType = ControllerType.None;
    private DispatcherTimer _pollTimer;
    private State _currentState;
    private State _previousState;
    private bool _initialized;

    public bool IsConnected => _controller?.IsConnected ?? false;
    public ControllerType ActiveControllerType => _activeControllerType;
    public bool IsInitialized => _initialized;

    public float LeftStickX => _currentState.Gamepad.LeftThumbX / 32767f;
    public float LeftStickY => _currentState.Gamepad.LeftThumbY / 32767f;
    public float RightStickX => _currentState.Gamepad.RightThumbX / 32767f;
    public float RightStickY => _currentState.Gamepad.RightThumbY / 32767f;
    public float LeftTrigger => _currentState.Gamepad.LeftTrigger / 255f;
    public float RightTrigger => _currentState.Gamepad.RightTrigger / 255f;

    public bool IsAPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.A) != 0;
    public bool IsBPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.B) != 0;
    public bool IsXPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.X) != 0;
    public bool IsYPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.Y) != 0;
    public bool IsStartPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.Start) != 0;
    public bool IsBackPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.Back) != 0;
    public bool IsLeftShoulder => (_currentState.Gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
    public bool IsRightShoulder => (_currentState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
    public bool IsLeftThumbPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0;
    public bool IsRightThumbPressed => (_currentState.Gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0;

    public bool IsADown => IsAPressed && (_previousState.Gamepad.Buttons & GamepadButtonFlags.A) == 0;
    public bool IsBDown => IsBPressed && (_previousState.Gamepad.Buttons & GamepadButtonFlags.B) == 0;

    public bool DPadUp => (_currentState.Gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0;
    public bool DPadDown => (_currentState.Gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0;
    public bool DPadLeft => (_currentState.Gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0;
    public bool DPadRight => (_currentState.Gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0;

    private ControllerManager()
    {
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(8) // ~120Hz polling
        };
        _pollTimer.Tick += PollController;
    }

    public void Initialize()
    {
        if (_initialized) return;

        // Try PlayerIndex.One first
        _controller = new Controller(UserIndex.One);
        _activeControllerType = DetectControllerType();
        _pollTimer.Start();
        _initialized = true;

        if (_controller.IsConnected)
            Logger.Log($"Controller connected: {_activeControllerType}", LogLevel.Info);
        else
            Logger.Log("No controller detected. Keyboard/mouse active.", LogLevel.Info);
    }

    public void Shutdown()
    {
        _pollTimer.Stop();
        Logger.Log("ControllerManager shut down", LogLevel.Info);
    }

    private void PollController(object? sender, EventArgs e)
    {
        if (_controller == null) return;

        _previousState = _currentState;

        try
        {
            if (_controller.IsConnected)
            {
                _currentState = _controller.GetState();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Controller poll error", ex);
            _activeControllerType = ControllerType.None;
        }
    }

    private ControllerType DetectControllerType()
    {
        if (_controller == null || !_controller.IsConnected)
            return ControllerType.None;

        return ControllerType.XboxGeneric; // SharpDX XInput handles Xbox controllers natively
    }
}

public enum ControllerType
{
    None,
    XboxGeneric,
    XboxOne,
    XboxSeries,
    PS4DualShock,
    PS5DualSense,
    Unknown
}
