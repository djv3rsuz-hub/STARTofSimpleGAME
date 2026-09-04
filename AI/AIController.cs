using System.Windows;
using SimpleWPFGame.Game;
using SimpleWPFGame.Combat;

namespace SimpleWPFGame.AI;

public class AIController
{
    private AIBrain? _brain;
    private Cube? _owner;
    private Cube? _target;
    private AIAction _currentAction = AIAction.None;
    private double _actionTimer;
    private double _stateTimer;
    private bool _isThinking;
    private readonly Random _rng = new();

    public AIBrain? Brain => _brain;
    public AIAction CurrentAction => _currentAction;
    public bool IsThinking => _isThinking;

    public void Initialize(Cube owner, Cube? target, AIPersonality personality, AIDifficulty difficulty)
    {
        _owner = owner;
        _target = target;
        _brain = AIBrain.Instance;
        _brain.Initialize(personality, difficulty);
    }

    public void Update(double deltaTime)
    {
        if (_owner == null || _target == null || _brain == null) return;
        if (!_owner.IsActive) return;

        var context = BuildContext(deltaTime);
        _brain.Memory.RecordPlayerAction(
            DetectPlayerAction(_target),
            _target.Position,
            _target.Combat.State);

        _actionTimer -= deltaTime;
        _stateTimer -= deltaTime;

        if (_actionTimer <= 0)
        {
            _currentAction = _brain.Decide(context);
            _actionTimer = 0.1 + (1.0 - _brain.ReactionSpeed) * 0.2;
            ExecuteAction(_currentAction, context);
        }

        UpdateMovement(deltaTime);
    }

    private AIContext BuildContext(double deltaTime)
    {
        var settings = Settings.GameSettings.Instance;
        Vector selfPos = new(_owner!.Position.X + _owner.Width / 2, _owner.Position.Y + _owner.Height / 2);
        Vector targetPos = new(_target!.Position.X + _target.Width / 2, _target.Position.Y + _target.Height / 2);
        double dist = (targetPos - selfPos).Length;

        return new AIContext
        {
            DeltaTime = deltaTime,
            GameTime = System.Diagnostics.Stopwatch.GetTimestamp() / 10000000.0,
            SelfPosition = selfPos,
            SelfVelocity = _owner.Velocity,
            SelfHP = _owner.Stats.HP,
            SelfMaxHP = _owner.Stats.MaxHP,
            SelfStamina = _owner.Stats.Stamina,
            SelfMana = _owner.Stats.Mana,
            SelfCombatState = _owner.Combat.State,
            SelfFacingAngle = _owner.FacingAngle,
            SelfIsControllable = _owner.IsControllable,
            TargetPosition = targetPos,
            TargetVelocity = _target.Velocity,
            TargetHP = _target.Stats.HP,
            TargetMaxHP = _target.Stats.MaxHP,
            TargetCombatState = _target.Combat.State,
            TargetFacingAngle = _target.FacingAngle,
            TargetIsControllable = _target.IsControllable,
            DistanceToTarget = dist,
            DistanceToCenter = Math.Sqrt(Math.Pow(selfPos.X - settings.GameScreenWidth / 2, 2) +
                Math.Pow(selfPos.Y - settings.GameScreenHeight / 2, 2)),
            DistanceToWall = Math.Min(selfPos.X, settings.GameScreenWidth - selfPos.X),
            IsTargetInRange = dist < 150,
            IsTargetAttacking = _target.Combat.IsAttacking,
            IsTargetBlocking = _target.Combat.State == CombatState.Blocking,
            IsTargetDodging = _target.Combat.State == CombatState.Dodging,
            IsTargetParrying = _target.Combat.State == CombatState.Parrying,
            IsCornered = selfPos.X < 80 || selfPos.X > settings.GameScreenWidth - 80 ||
                         selfPos.Y < 80 || selfPos.Y > settings.GameScreenHeight - 80,
            HasHealthAdvantage = _owner.Stats.HP / _owner.Stats.MaxHP > _target.Stats.HP / _target.Stats.MaxHP,
            HasPositionAdvantage = dist < 120 && !_target.Combat.IsAttacking
        };
    }

    private void ExecuteAction(AIAction action, AIContext context)
    {
        if (_owner == null || _target == null) return;

        switch (action)
        {
            case AIAction.Attack:
                if (_owner.Combat.CanAct && context.IsTargetInRange)
                    _owner.Combat.TryAttack(_owner.Position, _owner.FacingAngle, false);
                break;

            case AIAction.HeavyAttack:
                if (_owner.Combat.CanAct && context.IsTargetInRange)
                    _owner.Combat.TryAttack(_owner.Position, _owner.FacingAngle, true);
                break;

            case AIAction.Dodge:
                if (_owner.Actions.DodgeEnabled && _owner.Combat.CanAct)
                {
                    Vector dodgeDir = _brain!.Predictor.GetPredictedDodgeDirection(context);
                    _owner.Velocity = dodgeDir * _owner.DashDistance / _owner.DashDuration;
                }
                break;

            case AIAction.Block:
                if (_owner.Actions.BlockEnabled && _owner.Combat.CanAct)
                    _owner.Combat.TryBlock();
                break;

            case AIAction.Parry:
                if (_owner.Actions.ParryEnabled && _owner.Combat.CanAct)
                    _owner.Combat.TryParry();
                break;

            case AIAction.CounterAttack:
                if (_owner.Combat.State == CombatState.Countering && context.IsTargetInRange)
                    _owner.Combat.TryAttack(_owner.Position, _owner.FacingAngle, false);
                break;

            case AIAction.Feint:
                if (_owner.Combat.CanAct && context.IsTargetInRange)
                {
                    _owner.Combat.TryAttack(_owner.Position, _owner.FacingAngle, false);
                    _stateTimer = 0.1;
                }
                break;

            case AIAction.Wait:
                break;

            case AIAction.Retreat:
                Vector retreatDir = (_owner.Position - _target.Position);
                if (retreatDir.Length > 0.1)
                {
                    retreatDir.Normalize();
                    _owner.Velocity = retreatDir * _owner.MoveSpeed * 0.8;
                }
                break;
        }
    }

    private void UpdateMovement(double deltaTime)
    {
        if (_owner == null || _target == null || _brain == null) return;
        if (_owner.IsDashing || _owner.Combat.IsAttacking) return;

        Vector toTarget = new(_target.Position.X - _owner.Position.X, _target.Position.Y - _owner.Position.Y);
        double dist = toTarget.Length;
        if (toTarget.Length > 0.1) toTarget.Normalize();

        switch (_currentAction)
        {
            case AIAction.MoveToward:
                if (dist > 80)
                    _owner.Velocity = toTarget * _owner.MoveSpeed;
                else
                    _owner.Velocity *= 0.85;
                break;

            case AIAction.MoveAway:
                if (dist < 150)
                    _owner.Velocity = -toTarget * _owner.MoveSpeed * 0.7;
                else
                    _owner.Velocity *= 0.85;
                break;

            case AIAction.MoveLeft:
                _owner.Velocity = new Vector(-_owner.MoveSpeed * 0.6, _owner.Velocity.Y * 0.8);
                break;

            case AIAction.MoveRight:
                _owner.Velocity = new Vector(_owner.MoveSpeed * 0.6, _owner.Velocity.Y * 0.8);
                break;

            case AIAction.Reposition:
                var settings = Settings.GameSettings.Instance;
                Vector center = new(settings.GameScreenWidth / 2, settings.GameScreenHeight / 2);
                Vector toCenter = center - _owner.Position;
                if (toCenter.Length > 1) toCenter.Normalize();
                _owner.Velocity = toCenter * _owner.MoveSpeed * 0.5;
                break;

            case AIAction.Aggressive:
                if (dist > 60)
                    _owner.Velocity = toTarget * _owner.MoveSpeed * 1.1;
                break;

            case AIAction.Defensive:
                if (dist < 100)
                    _owner.Velocity = -toTarget * _owner.MoveSpeed * 0.4;
                else
                    _owner.Velocity *= 0.9;
                break;

            default:
                if (dist > 120)
                    _owner.Velocity = toTarget * _owner.MoveSpeed * 0.3;
                else if (dist < 60)
                    _owner.Velocity = -toTarget * _owner.MoveSpeed * 0.3;
                else
                    _owner.Velocity *= 0.85;
                break;
        }
    }

    private AIAction DetectPlayerAction(Cube player)
    {
        if (player.Combat.IsAttacking) return AIAction.Attack;
        if (player.Combat.State == CombatState.Dodging) return AIAction.Dodge;
        if (player.Combat.State == CombatState.Blocking) return AIAction.Block;
        if (player.Combat.State == CombatState.Parrying) return AIAction.Parry;

        Vector toMe = new(_owner!.Position.X - player.Position.X, _owner.Position.Y - player.Position.Y);
        if (toMe.Length > 0.1)
        {
            toMe.Normalize();
            double dot = toMe.X * Math.Cos(player.FacingAngle) + toMe.Y * Math.Sin(player.FacingAngle);
            if (dot > 0.5 && player.Velocity.Length > 10) return AIAction.MoveToward;
        }

        return AIAction.None;
    }

    public void SetTarget(Cube? target) => _target = target;

    public void Reset()
    {
        _brain?.Reset();
        _currentAction = AIAction.None;
        _actionTimer = 0;
        _stateTimer = 0;
    }
}
