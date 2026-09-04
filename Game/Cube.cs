using System.Windows;
using System.Windows.Media;
using SimpleWPFGame.Combat;
using SimpleWPFGame.Config;
using SimpleWPFGame.Input;
using SimpleWPFGame.Logging;
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

    // Dash action
    public double DashDistance { get; set; } = 50;
    public double DashRotationSpeed { get; set; } = 2160;
    public double DashCooldown { get; set; } = 0.8;
    public double DashDuration { get; set; } = 0.12;

    public bool IsDashing { get; internal set; }
    public double DashCooldownRemaining { get; internal set; }

    // RPG Stats
    public CharacterStats Stats { get; set; } = new();
    public CharacterActionToggles Actions { get; set; } = new();
    public AIProfile AI { get; set; } = new();

    // Combat
    public CombatComponent Combat { get; private set; }
    public double FacingAngle { get; set; }

    private Vector _dashDirection;
    private double _dashTimer;
    private double _dashSpeed;
    private Vector _positionBeforeDash;

    private Pen _borderPen;

    public Cube(double x, double y, double size, Brush color, bool controllable = false)
        : base(x, y, size, size)
    {
        Color = color;
        IsControllable = controllable;
        _borderPen = new Pen(Brushes.White, 1);
        Combat = new CombatComponent(Stats);
    }

    public void SetStats(CharacterStats stats, CharacterActionToggles actions, AIProfile? ai = null)
    {
        Stats = stats;
        Actions = actions;
        if (ai != null) AI = ai;
        Combat.SetStats(stats);
    }

    public void EquipWeapon(Weapon weapon)
    {
        Combat.EquipWeapon(weapon);
    }

    public double DistanceTo(Cube other)
    {
        return (other.Position - Position).Length;
    }

    public Vector DirectionTo(Cube other)
    {
        var dir = other.Position - Position;
        if (dir.Length < 0.01) return new Vector(1, 0);
        dir.Normalize();
        return dir;
    }

    public void SetBorderColor(Brush brush, double thickness = 1)
    {
        _borderPen = new Pen(brush, thickness);
    }

    public override void Update(double deltaTime)
    {
        if (!IsActive) return;

        Combat.Update(deltaTime);

        if (DashCooldownRemaining > 0)
            DashCooldownRemaining -= deltaTime;

        if (IsDashing)
        {
            UpdateDash(deltaTime);
        }
        else if (IsControllable)
        {
            if (TryStartDash()) { }
            else if (TryCombatInput()) { }
            else
            {
                UpdateMovement(deltaTime);
            }
        }

        if (!IsDashing)
            Position += Velocity * deltaTime;

        if (ClampToScreen)
        {
            var settings = GameSettings.Instance;
            double borderOffset = 2;
            double maxX = settings.GameScreenWidth - Width - borderOffset;
            double maxY = settings.GameScreenHeight - Height - borderOffset;
            Position = new Vector(
                Math.Clamp(Position.X, borderOffset, maxX),
                Math.Clamp(Position.Y, borderOffset, maxY));

            if (Position.X <= borderOffset || Position.X >= maxX) Velocity = new Vector(0, Velocity.Y);
            if (Position.Y <= borderOffset || Position.Y >= maxY) Velocity = new Vector(Velocity.X, 0);
        }
    }

    private bool TryCombatInput()
    {
        if (!IsControllable || !Combat.CanAct) return false;

        var input = InputManager.Instance;
        var controller = ControllerManager.Instance;

        // Attack: J key / X button / Left Click
        bool attackPressed = input.IsKeyJustPressed(System.Windows.Input.Key.J)
            || (controller.IsConnected && controller.IsXDown);

        // Block: K key / Right Bumper
        bool blockPressed = input.IsKeyDown(System.Windows.Input.Key.K)
            || (controller.IsConnected && controller.IsRBDown);

        // Parry: L key / Left Bumper (tap)
        bool parryPressed = input.IsKeyJustPressed(System.Windows.Input.Key.L)
            || (controller.IsConnected && controller.IsLBDown);

        // Dodge: I key / Y button
        bool dodgePressed = input.IsKeyJustPressed(System.Windows.Input.Key.I)
            || (controller.IsConnected && controller.IsYDown);

        if (attackPressed && Actions.AttackEnabled)
        {
            if (Combat.TryAttack(Position, FacingAngle))
            {
                UpdateFacingDirection();
                return true;
            }
        }

        if (blockPressed && Actions.BlockEnabled && Combat.CanAct)
        {
            Combat.TryBlock();
            return true;
        }

        if (parryPressed && Actions.ParryEnabled)
        {
            Combat.TryParry();
            return true;
        }

        if (dodgePressed && Actions.DodgeEnabled)
        {
            Combat.TryDodge();
            return true;
        }

        return false;
    }

    private void UpdateFacingDirection()
    {
        var input = InputManager.Instance;
        var controller = ControllerManager.Instance;

        double dx = 0, dy = 0;

        if (input.IsKeyDown(System.Windows.Input.Key.W) || input.IsKeyDown(System.Windows.Input.Key.Up)) dy -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.S) || input.IsKeyDown(System.Windows.Input.Key.Down)) dy += 1;
        if (input.IsKeyDown(System.Windows.Input.Key.A) || input.IsKeyDown(System.Windows.Input.Key.Left)) dx -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.D) || input.IsKeyDown(System.Windows.Input.Key.Right)) dx += 1;

        if (controller.IsConnected)
        {
            float deadzone = GameSettings.Instance.StickDeadzone;
            if (Math.Abs(controller.LeftStickX) > deadzone) dx += controller.LeftStickX;
            if (Math.Abs(controller.LeftStickY) > deadzone) dy -= controller.LeftStickY;
        }

        if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
            FacingAngle = Math.Atan2(dy, dx);
    }

    public void ApplyKnockback(Vector knockback)
    {
        double resist = Stats.KnockbackResist;
        double finalKnockback = knockback.Length * (1 - resist);
        if (finalKnockback < 1) return;
        var dir = knockback;
        if (dir.Length > 0.01) dir.Normalize();
        Velocity += dir * finalKnockback;
    }

    private bool TryStartDash()
    {
        if (!Actions.DashEnabled) return false;
        if (DashCooldownRemaining > 0) return false;

        var input = InputManager.Instance;
        var controller = ControllerManager.Instance;
        var settings = GameSettings.Instance;

        // Check dash trigger: B button on controller, Space on keyboard
        bool dashPressed = false;
        if (controller.IsConnected && controller.IsBDown)
            dashPressed = true;
        if (input.IsKeyJustPressed(System.Windows.Input.Key.Space))
            dashPressed = true;

        if (!dashPressed) return false;

        // Get dash direction from stick/keyboard
        var dir = new Vector(0, 0);

        // Keyboard direction
        if (input.IsKeyDown(System.Windows.Input.Key.W) || input.IsKeyDown(System.Windows.Input.Key.Up))
            dir.Y -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.S) || input.IsKeyDown(System.Windows.Input.Key.Down))
            dir.Y += 1;
        if (input.IsKeyDown(System.Windows.Input.Key.A) || input.IsKeyDown(System.Windows.Input.Key.Left))
            dir.X -= 1;
        if (input.IsKeyDown(System.Windows.Input.Key.D) || input.IsKeyDown(System.Windows.Input.Key.Right))
            dir.X += 1;

        // Controller stick direction
        if (controller.IsConnected)
        {
            float deadzone = settings.StickDeadzone;
            float stickX = controller.LeftStickX;
            float stickY = -controller.LeftStickY;

            if (Math.Abs(stickX) > deadzone) dir.X += stickX;
            if (Math.Abs(stickY) > deadzone) dir.Y += stickY;

            // D-Pad
            if (controller.DPadUp) dir.Y -= 1;
            if (controller.DPadDown) dir.Y += 1;
            if (controller.DPadLeft) dir.X -= 1;
            if (controller.DPadRight) dir.X += 1;
        }

        // If no direction input, dash forward based on current velocity, or default right
        if (dir.LengthSquared < 0.001)
        {
            if (Velocity.LengthSquared > 1)
                dir = Velocity / Velocity.Length;
            else
                dir = new Vector(1, 0); // Default: dash right
        }
        else
        {
            dir.Normalize();
        }

        // Start dash
        IsDashing = true;
        _dashDirection = dir;
        _dashTimer = DashDuration;
        _positionBeforeDash = Position;
        _dashSpeed = DashDistance / DashDuration;
        Velocity = new Vector(0, 0);

        Logger.Log($"Dash started: dir=({dir.X:F2},{dir.Y:F2}) dist={DashDistance} speed={_dashSpeed:F0}", LogLevel.Debug);
        return true;
    }

    private void UpdateDash(double deltaTime)
    {
        _dashTimer -= deltaTime;

        // Move in dash direction
        Position += _dashDirection * (_dashSpeed * deltaTime);

        // Rotate the cube during dash
        Rotation += DashRotationSpeed * deltaTime;
        if (Rotation >= 360) Rotation -= 360;

        // Clamp during dash
        if (ClampToScreen)
        {
            var settings = GameSettings.Instance;
            double borderOffset = 2;
            double maxX = settings.GameScreenWidth - Width - borderOffset;
            double maxY = settings.GameScreenHeight - Height - borderOffset;
            Position = new Vector(
                Math.Clamp(Position.X, borderOffset, maxX),
                Math.Clamp(Position.Y, borderOffset, maxY));
        }

        // Dash finished
        if (_dashTimer <= 0)
        {
            IsDashing = false;
            Rotation = 0;
            DashCooldownRemaining = DashCooldown;
            Logger.Log($"Dash finished. Cooldown: {DashCooldown}s", LogLevel.Debug);
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
            desiredDir.Normalize();
            var targetVelocity = desiredDir * MoveSpeed;

            if (AccelerationSpeed <= 0)
            {
                Velocity = targetVelocity;
            }
            else
            {
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
            if (DecelerationSpeed <= 0)
            {
                Velocity = new Vector(0, 0);
            }
            else
            {
                double currentSpeed = Velocity.Length;
                if (currentSpeed > 0.5)
                {
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

        Combat.Render(context, new Vector(Position.X + Width / 2, Position.Y + Height / 2), FacingAngle);

        if (Math.Abs(Rotation) > 0.01)
        {
            var center = new Point(Position.X + Width / 2, Position.Y + Height / 2);
            var group = new TransformGroup();
            group.Children.Add(new RotateTransform(Rotation, center.X, center.Y));

            context.PushTransform(group);
            context.DrawRectangle(Color, _borderPen, Bounds);
            context.Pop();
        }
        else
        {
            context.DrawRectangle(Color, _borderPen, Bounds);
        }

        RenderCollisionDebug(context);
    }
}
