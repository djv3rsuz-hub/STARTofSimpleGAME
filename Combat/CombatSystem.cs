using System.Windows;
using System.Windows.Media;
using SimpleWPFGame.Config;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;

namespace SimpleWPFGame.Combat;

public class CombatComponent
{
    public CombatState State { get; private set; } = CombatState.Idle;
    public Weapon? EquippedWeapon { get; private set; }
    public int ComboIndex { get; private set; }
    public int CurrentFrame { get; private set; }
    public double ComboTimer { get; private set; }
    public bool CanCancel { get; private set; }

    public bool IsAttacking => State == CombatState.Attacking || State == CombatState.ComboAttacking;
    public bool IsDefending => State == CombatState.Blocking || State == CombatState.Parrying;
    public bool IsInvincible => State == CombatState.Dodging;
    public bool CanAct => State == CombatState.Idle || CanCancel;

    public Hitbox[] CurrentHitboxes { get; private set; } = Array.Empty<Hitbox>();
    public bool ShowHitboxDebug { get; set; }
    public Vector OwnerPosition { get; set; }
    public double OwnerRotation { get; set; }

    private double _comboWindow = 0.5;
    private double _stunTimer;
    private double _counterTimer;
    private int _parryFrame;
    private int _dodgeFrame;
    private bool _isParrying;
    private bool _isPerfectParrying;
    private bool _isDodging;
    private bool _isPerfectDodging;
    private bool _isBlocking;
    private bool _wasPerfectParried;
    private FrameData _currentFrameData;
    private CharacterStats _stats;
    private HashSet<int> _hitTargets = new();
    private bool _hasHitThisSwing;

    public CombatComponent(CharacterStats stats)
    {
        _stats = stats;
        _currentFrameData = FrameData.SwordSlash;
    }

    public void SetStats(CharacterStats stats) => _stats = stats;

    public void EquipWeapon(Weapon weapon)
    {
        EquippedWeapon = weapon;
        Logger.Log($"Equipped: {weapon.Name}", LogLevel.Info);
    }

    public bool TryAttack(Vector attackerPos, double attackerRotation, bool heavy = false)
    {
        if (!CanAct || EquippedWeapon == null) return false;

        if (State == CombatState.Idle)
        {
            StartAttack(0);
            OwnerPosition = attackerPos;
            OwnerRotation = attackerRotation;
            return true;
        }

        if (CanCancel && ComboIndex < EquippedWeapon.ComboLength - 1)
        {
            int nextCombo = ComboIndex + 1;
            StartAttack(nextCombo);
            OwnerPosition = attackerPos;
            OwnerRotation = attackerRotation;
            return true;
        }

        return false;
    }

    private void StartAttack(int comboIndex)
    {
        ComboIndex = comboIndex;
        CurrentFrame = 0;
        State = comboIndex == 0 ? CombatState.Attacking : CombatState.ComboAttacking;
        _currentFrameData = EquippedWeapon!.GetFrameData(comboIndex);
        CanCancel = false;
        ComboTimer = _comboWindow;
        _hasHitThisSwing = false;
        _hitTargets.Clear();
        UpdateHitboxes();
    }

    public bool TryDodge()
    {
        if (!CanAct) return false;

        double dodgeChance = _stats.DodgeChance;
        _isPerfectDodging = CombatCalculator.RollPerfectDodge(dodgeChance);
        _isDodging = !_isPerfectDodging && CombatCalculator.RollDodge(dodgeChance);

        State = CombatState.Dodging;
        CurrentFrame = 0;
        _currentFrameData = new FrameData
        {
            StartupFrames = 2,
            ActiveFrames = _isPerfectDodging ? 16 : 12,
            RecoveryFrames = 4,
            DodgeIframeFrames = _isPerfectDodging ? 20 : 12,
            PerfectDodgeWindowFrames = 4
        };

        return true;
    }

    public bool TryBlock()
    {
        if (!CanAct) return false;
        State = CombatState.Blocking;
        _isBlocking = true;
        CurrentFrame = 0;
        return true;
    }

    public bool TryParry()
    {
        if (!CanAct) return false;

        double parryChance = _stats.ParryChance;
        _isPerfectParrying = CombatCalculator.RollPerfectParry(parryChance);
        _isParrying = !_isPerfectParrying && CombatCalculator.RollParry(parryChance);

        State = CombatState.Parrying;
        CurrentFrame = 0;
        _currentFrameData = new FrameData
        {
            StartupFrames = 1,
            ActiveFrames = _isPerfectParrying ? 8 : 6,
            RecoveryFrames = 3,
            ParryWindowStart = 0,
            ParryWindowFrames = _isPerfectParrying ? 8 : 6,
            PerfectParryWindowFrames = _isPerfectParrying ? 4 : 2
        };

        return true;
    }

    public void TriggerCounter()
    {
        if (State != CombatState.Parrying) return;
        State = CombatState.Countering;
        _counterTimer = 0.5;
        _wasPerfectParried = true;
        Logger.Log("Counter triggered!", LogLevel.Info);
    }

    public void ApplyStun(double duration)
    {
        State = CombatState.Stunned;
        _stunTimer = duration;
    }

    public AttackResult ProcessIncomingAttack(
        CharacterStats attacker,
        Vector attackerPos,
        Vector defenderPos,
        int attackComboIndex)
    {
        if (EquippedWeapon == null)
            return new AttackResult { Hit = true, DamageDealt = 5 };

        var attackFrameData = EquippedWeapon.GetFrameData(attackComboIndex);

        return CombatCalculator.CalculateAttack(
            attacker,
            _stats,
            EquippedWeapon,
            attackComboIndex,
            _isBlocking,
            _isParrying,
            _isPerfectParrying,
            _isDodging,
            _isPerfectDodging,
            _parryFrame,
            _dodgeFrame,
            attackFrameData);
    }

    public bool CheckHitTarget(int targetId)
    {
        return _hitTargets.Contains(targetId);
    }

    public void RegisterHit(int targetId)
    {
        _hitTargets.Add(targetId);
        _hasHitThisSwing = true;
    }

    private void UpdateHitboxes()
    {
        if (EquippedWeapon == null) return;
        CurrentHitboxes = EquippedWeapon.GetHitboxes(ComboIndex, OwnerPosition, OwnerRotation);
        foreach (ref var hb in CurrentHitboxes.AsSpan())
            hb.IsActive = true;
    }

    public void Update(double deltaTime)
    {
        try
        {
            switch (State)
            {
                case CombatState.Attacking:
                case CombatState.ComboAttacking:
                    UpdateAttack(deltaTime);
                    break;
                case CombatState.Dodging:
                    UpdateDodge(deltaTime);
                    break;
                case CombatState.Blocking:
                    UpdateBlock(deltaTime);
                    break;
                case CombatState.Parrying:
                    UpdateParry(deltaTime);
                break;
            case CombatState.Countering:
                UpdateCounter(deltaTime);
                break;
            case CombatState.Stunned:
                UpdateStun(deltaTime);
                break;
        }

        if (IsAttacking)
            UpdateHitboxes();
        }
        catch (Exception ex)
        {
            Logging.Logger.LogError("Combat update error", ex);
            ResetState();
        }
    }

    private void UpdateAttack(double deltaTime)
    {
        CurrentFrame++;
        OwnerPosition = OwnerPosition;

        int totalFrames = _currentFrameData.TotalFrames;
        if (CurrentFrame >= _currentFrameData.StartupFrames + _currentFrameData.ActiveFrames)
        {
            CanCancel = true;
            ComboTimer -= deltaTime;
        }

        if (CurrentFrame >= totalFrames)
        {
            if (ComboTimer > 0 && ComboIndex < (EquippedWeapon?.ComboLength ?? 1) - 1)
                return;

            ResetState();
        }
    }

    private void UpdateDodge(double deltaTime)
    {
        CurrentFrame++;
        if (CurrentFrame >= _currentFrameData.TotalFrames)
        {
            _isDodging = false;
            _isPerfectDodging = false;
            ResetState();
        }
    }

    private void UpdateBlock(double deltaTime)
    {
        CurrentFrame++;
        if (CurrentFrame > 30)
        {
            _isBlocking = false;
            ResetState();
        }
    }

    private void UpdateParry(double deltaTime)
    {
        CurrentFrame++;
        _parryFrame = CurrentFrame;

        if (CurrentFrame >= _currentFrameData.TotalFrames)
        {
            _isParrying = false;
            _isPerfectParrying = false;
            ResetState();
        }
    }

    private void UpdateCounter(double deltaTime)
    {
        _counterTimer -= deltaTime;
        if (_counterTimer <= 0)
        {
            _wasPerfectParried = false;
            StartAttack(0);
        }
    }

    private void UpdateStun(double deltaTime)
    {
        _stunTimer -= deltaTime;
        if (_stunTimer <= 0)
            ResetState();
    }

    private void ResetState()
    {
        State = CombatState.Idle;
        CurrentFrame = 0;
        ComboIndex = 0;
        CanCancel = false;
        _parryFrame = 0;
        _dodgeFrame = 0;
        CurrentHitboxes = Array.Empty<Hitbox>();
    }

    public void Render(DrawingContext context, Vector position, double rotation)
    {
        if (EquippedWeapon == null) return;

        if (IsAttacking)
        {
            EquippedWeapon.RenderSlash(context, position, rotation, CurrentFrame, ComboIndex);
        }

        if (ShowHitboxDebug && CurrentHitboxes.Length > 0)
        {
            foreach (var hb in CurrentHitboxes)
            {
                bool active = hb.CheckFrame(CurrentFrame);
                var color = active
                    ? Color.FromArgb(120, 255, 0, 0)
                    : Color.FromArgb(60, 255, 255, 0);
                var brush = new SolidColorBrush(color);
                var pen = new Pen(new SolidColorBrush(
                    active ? Color.FromRgb(255, 100, 100) : Color.FromRgb(200, 200, 0)), 1);
                context.DrawRectangle(brush, pen, hb.Bounds);
            }
        }

        if (State == CombatState.Dodging && _isPerfectDodging)
        {
            double alpha = 0.6 * (1 - (double)CurrentFrame / _currentFrameData.ActiveFrames);
            var brush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), 100, 200, 255));
            context.DrawEllipse(brush, null, new Point(position.X, position.Y), 35, 35);
        }

        if (State == CombatState.Parrying && _isPerfectParrying)
        {
            double progress = (double)CurrentFrame / _currentFrameData.ActiveFrames;
            double radius = 30 + progress * 20;
            double alpha = 1 - progress;
            var brush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), 255, 255, 100));
            context.DrawEllipse(brush, null, new Point(position.X, position.Y), radius, radius);
        }

        if (State == CombatState.Countering)
        {
            double flash = Math.Sin(_counterTimer * 20) * 0.5 + 0.5;
            var brush = new SolidColorBrush(Color.FromArgb((byte)(flash * 255), 255, 50, 50));
            context.DrawEllipse(brush, null, new Point(position.X, position.Y), 25, 25);
        }

        if (State == CombatState.Stunned)
        {
            for (int i = 0; i < 3; i++)
            {
                double angle = _stunTimer * 5 + i * Math.PI * 2 / 3;
                double x = position.X + Math.Cos(angle) * 20;
                double y = position.Y - 25 + Math.Sin(angle * 2) * 5;
                var starBrush = new SolidColorBrush(Colors.Yellow);
                context.DrawEllipse(starBrush, null, new Point(x, y), 3, 3);
            }
        }
    }
}
