using System.Windows;
using System.Windows.Input;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame.Input;

public sealed class InputManager
{
    private static readonly Lazy<InputManager> _instance = new(() => new InputManager());
    public static InputManager Instance => _instance.Value;

    private readonly HashSet<Key> _pressedKeys = new();
    private readonly HashSet<Key> _justPressedKeys = new();
    private readonly HashSet<Key> _justReleasedKeys = new();
    private Point _mousePosition;
    private Point _mouseDelta;
    private bool _initialized;

    private InputManager() { }

    public bool IsInitialized => _initialized;

    public void Initialize(UIElement element)
    {
        if (_initialized) return;

        element.PreviewKeyDown += OnKeyDown;
        element.PreviewKeyUp += OnKeyUp;
        element.MouseMove += OnMouseMove;
        element.LostFocus += OnLostFocus;

        _initialized = true;
        Logger.Log("InputManager initialized", LogLevel.Info);
    }

    public void BeginFrame()
    {
        _justPressedKeys.Clear();
        _justReleasedKeys.Clear();
    }

    public void EndFrame()
    {
        _mouseDelta = new Point(0, 0);
    }

    public bool IsKeyDown(Key key) => _pressedKeys.Contains(key);
    public bool IsKeyJustPressed(Key key) => _justPressedKeys.Contains(key);
    public bool IsKeyJustReleased(Key key) => _justReleasedKeys.Contains(key);
    public Point MousePosition => _mousePosition;
    public Point MouseDelta => _mouseDelta;

    public bool IsAnyMovementKey =>
        IsKeyDown(Key.W) || IsKeyDown(Key.A) || IsKeyDown(Key.S) || IsKeyDown(Key.D) ||
        IsKeyDown(Key.Up) || IsKeyDown(Key.Down) || IsKeyDown(Key.Left) || IsKeyDown(Key.Right);

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_pressedKeys.Contains(e.Key))
            _justPressedKeys.Add(e.Key);
        _pressedKeys.Add(e.Key);
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
        _justReleasedKeys.Add(e.Key);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition((IInputElement)sender);
        _mouseDelta = new Point(pos.X - _mousePosition.X, pos.Y - _mousePosition.Y);
        _mousePosition = pos;
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _pressedKeys.Clear();
        _justPressedKeys.Clear();
        _justReleasedKeys.Clear();
    }
}
