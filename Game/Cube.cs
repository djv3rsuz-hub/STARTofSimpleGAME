using System.Windows;
using System.Windows.Media;
using SimpleWPFGame.Input;
using SimpleWPFGame.Settings;

namespace SimpleWPFGame.Game;

public class Cube : GameObject
{
    public Brush Color { get; set; }
    public bool IsControllable { get; set; }
    public double MoveSpeed { get; set; } = 300;
    public double AccelerationSpeed { get; set; } = 0;
    public double DecelerationSpeed { get; set; } = 0;
    public bool ClampToScreen { get; set; } = true;

    private Pen _borderPen;

    public Cube(double x, double y, double size, Brush color, bool controllable = false)
        : base(x, y, size, size)
    {
        Color = color;
        IsControllable = controllable;
        _borderPen = new Pen(Brushes.White, 1);
    }

    public void SetBorderColor(Brush brush, double thickness = 1)
    {
        _borderPen = new Pen(brush, thickness);
    }

    public override void Update(double deltaTime)
    {
        if (!IsActive) return;

        if (IsControllable)
            UpdateMovement(deltaTime);

        // Apply velocity to position
        Position += Velocity * deltaTime;

        // Clamp to screen edges (inside the 1px game border)
        if (ClampToScreen)
        {
            var settings = GameSettings.Instance;
            double borderOffset = 2;
            double maxX = settings.GameScreenWidth - Width - borderOffset;
            double maxY = settings.GameScreenHeight - Height - borderOffset;
            Position = new Vector(
                Math.Clamp(Position.X, borderOffset, maxX),
                Math.Clamp(Position.Y, borderOffset, maxY));

            // Stop at walls
            if (Position.X <= borderOffset || Position.X >= maxX) Velocity = new Vector(0, Velocity.Y);
            if (Position.Y <= borderOffset || Position.Y >= maxY) Velocity = new Vector(Velocity.X, 0);
        }
    }

    private void UpdateMovement(double deltaTime)
    {
        var input = InputManager.Instance;
        var controller = ControllerManager.Instance;
        var settings = GameSettings.Instance;

        var desiredDir = new Vector(0, 0);

        // Keyboard input
        if (input.IsKeyDown(System.Windows.Input.Key.W) || input.IsKeyDown(System.Windows.Input.Key.Up))
            desiredDir.Y -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.S) || input.IsKeyDown(System.Windows.Input.Key.Down))
            desiredDir.Y += 1;
        if (input.IsKeyDown(System.Windows.Input.Key.A) || input.IsKeyDown(System.Windows.Input.Key.Left))
            desiredDir.X -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.D) || input.IsKeyDown(System.Windows.Input.Key.Right))
            desiredDir.X += 1;

        // Controller stick input
        if (controller.IsConnected)
        {
            float deadzone = settings.StickDeadzone;
            float sensitivity = settings.ControllerSensitivity;
            float stickX = controller.LeftStickX * sensitivity;
            float stickY = -controller.LeftStickY * sensitivity;

            if (Math.Abs(stickX) > deadzone)
                desiredDir.X += stickX;
            if (Math.Abs(stickY) > deadzone)
                desiredDir.Y += stickY;

            // D-Pad
            if (controller.DPadUp) desiredDir.Y -= 1;
            if (controller.DPadDown) desiredDir.Y += 1;
            if (controller.DPadLeft) desiredDir.X -= 1;
            if (controller.DPadRight) desiredDir.X += 1;
        }

        bool hasInput = desiredDir.LengthSquared > 0.001;

        if (hasInput)
        {
            // Normalize so diagonal isn't faster
            desiredDir.Normalize();

            // Target velocity at max speed in the input direction
            var targetVelocity = desiredDir * MoveSpeed;

            if (AccelerationSpeed <= 0)
            {
                // Instant acceleration (legacy behavior)
                Velocity = targetVelocity;
            }
            else
            {
                // Accelerate toward target velocity
                var diff = targetVelocity - Velocity;
                double maxStep = AccelerationSpeed * deltaTime;

                if (diff.Length <= maxStep)
                    Velocity = targetVelocity;
                else
                    Velocity += Vector.Multiply(diff / diff.Length, maxStep);
            }
        }
        else
        {
            // No input - decelerate to zero
            if (DecelerationSpeed <= 0)
            {
                // Instant stop (legacy behavior)
                Velocity = new Vector(0, 0);
            }
            else
            {
                double currentSpeed = Velocity.Length;
                if (currentSpeed > 0.5)
                {
                    // Decelerate toward zero
                    double maxStep = DecelerationSpeed * deltaTime;
                    if (maxStep >= currentSpeed)
                        Velocity = new Vector(0, 0);
                    else
                        Velocity *= (currentSpeed - maxStep) / currentSpeed;
                }
                else
                {
                    Velocity = new Vector(0, 0);
                }
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        if (!IsActive) return;
        context.DrawRectangle(Color, _borderPen, Bounds);
        RenderCollisionDebug(context);
    }
}
