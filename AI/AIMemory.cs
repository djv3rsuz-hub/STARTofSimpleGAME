using System.Windows;

namespace SimpleWPFGame.AI;

public class AIContext
{
    public double DeltaTime { get; set; }
    public double GameTime { get; set; }
    public Vector SelfPosition { get; set; }
    public Vector SelfVelocity { get; set; }
    public double SelfHP { get; set; }
    public double SelfMaxHP { get; set; }
    public double SelfStamina { get; set; }
    public double SelfMana { get; set; }
    public Combat.CombatState SelfCombatState { get; set; }
    public double SelfFacingAngle { get; set; }
    public bool SelfIsControllable { get; set; }

    public Vector TargetPosition { get; set; }
    public Vector TargetVelocity { get; set; }
    public double TargetHP { get; set; }
    public double TargetMaxHP { get; set; }
    public Combat.CombatState TargetCombatState { get; set; }
    public double TargetFacingAngle { get; set; }
    public bool TargetIsControllable { get; set; }

    public double DistanceToTarget { get; set; }
    public double DistanceToCenter { get; set; }
    public double DistanceToWall { get; set; }
    public bool IsTargetInRange { get; set; }
    public bool IsTargetAttacking { get; set; }
    public bool IsTargetBlocking { get; set; }
    public bool IsTargetDodging { get; set; }
    public bool IsTargetParrying { get; set; }
    public bool IsCornered { get; set; }
    public bool HasHealthAdvantage { get; set; }
    public bool HasPositionAdvantage { get; set; }
}

public class AIMemory
{
    public struct ActionRecord
    {
        public AIAction Action;
        public double Timestamp;
        public double DamageDealt;
        public double DamageTaken;
        public bool WasSuccessful;
        public Vector Position;
        public Combat.CombatState State;
    }

    public struct PlayerPattern
    {
        public AIAction PlayerAction;
        public double Timestamp;
        public Vector Position;
        public Combat.CombatState State;
    }

    private readonly Queue<ActionRecord> _actionHistory = new(64);
    private readonly Queue<PlayerPattern> _playerPatterns = new(128);
    private readonly Dictionary<AIAction, int> _actionCounts = new();
    private readonly Dictionary<AIAction, double> _actionSuccessRates = new();
    private readonly Dictionary<AIAction, double> _actionDamageDealt = new();
    private readonly Dictionary<AIAction, double> _actionDamageTaken = new();
    private readonly Dictionary<Combat.CombatState, int> _playerStateCounts = new();
    private readonly Queue<double> _recentDamageTaken = new(16);
    private readonly Queue<double> _recentDamageDealt = new(16);

    public int TotalActions => _actionHistory.Count;
    public int TotalPlayerPatterns => _playerPatterns.Count;

    public void Update(AIContext context) { }

    public void RecordAction(AIAction action, double damageDealt, double damageTaken, bool success, Vector position, Combat.CombatState state)
    {
        var record = new ActionRecord
        {
            Action = action,
            Timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0,
            DamageDealt = damageDealt,
            DamageTaken = damageTaken,
            WasSuccessful = success,
            Position = position,
            State = state
        };

        _actionHistory.Enqueue(record);
        if (_actionHistory.Count > 64) _actionHistory.Dequeue();

        _actionCounts.TryGetValue(action, out int count);
        _actionCounts[action] = count + 1;

        _actionDamageDealt.TryGetValue(action, out double dmg);
        _actionDamageDealt[action] = dmg + damageDealt;

        _actionDamageTaken.TryGetValue(action, out double taken);
        _actionDamageTaken[action] = taken + damageTaken;

        _recentDamageDealt.Enqueue(damageDealt);
        if (_recentDamageDealt.Count > 16) _recentDamageDealt.Dequeue();
        _recentDamageTaken.Enqueue(damageTaken);
        if (_recentDamageTaken.Count > 16) _recentDamageTaken.Dequeue();
    }

    public void RecordPlayerAction(AIAction playerAction, Vector position, Combat.CombatState state)
    {
        var pattern = new PlayerPattern
        {
            PlayerAction = playerAction,
            Timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0,
            Position = position,
            State = state
        };

        _playerPatterns.Enqueue(pattern);
        if (_playerPatterns.Count > 128) _playerPatterns.Dequeue();

        _playerStateCounts.TryGetValue(state, out int count);
        _playerStateCounts[state] = count + 1;
    }

    public AIAction GetMostFrequentAction(int lastN = 20)
    {
        var recent = _actionHistory.TakeLast(Math.Min(lastN, _actionHistory.Count));
        return recent.GroupBy(r => r.Action).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? AIAction.None;
    }

    public AIAction GetMostEffectiveAction(int lastN = 20)
    {
        var recent = _actionHistory.TakeLast(Math.Min(lastN, _actionHistory.Count));
        return recent.GroupBy(r => r.Action)
            .Select(g => new { Action = g.Key, AvgDmg = g.Average(r => r.DamageDealt) })
            .OrderByDescending(x => x.AvgDmg).FirstOrDefault()?.Action ?? AIAction.None;
    }

    public AIAction GetSafestAction(int lastN = 20)
    {
        var recent = _actionHistory.TakeLast(Math.Min(lastN, _actionHistory.Count));
        return recent.GroupBy(r => r.Action)
            .Select(g => new { Action = g.Key, AvgTaken = g.Average(r => r.DamageTaken) })
            .OrderBy(x => x.AvgTaken).FirstOrDefault()?.Action ?? AIAction.None;
    }

    public double GetActionSuccessRate(AIAction action, int lastN = 20)
    {
        var recent = _actionHistory.TakeLast(Math.Min(lastN, _actionHistory.Count));
        var actionRecords = recent.Where(r => r.Action == action).ToList();
        if (actionRecords.Count == 0) return 0.5;
        return actionRecords.Count(r => r.WasSuccessful) / (double)actionRecords.Count;
    }

    public AIAction GetMostFrequentPlayerAction(int lastN = 30)
    {
        var recent = _playerPatterns.TakeLast(Math.Min(lastN, _playerPatterns.Count));
        if (!recent.Any()) return AIAction.None;

        var mostCommon = recent.GroupBy(p => p.PlayerAction)
            .OrderByDescending(g => g.Count()).FirstOrDefault();
        return mostCommon?.Key ?? AIAction.None;
    }

    public AIAction PredictNextPlayerAction(int lastN = 30)
    {
        var recent = _playerPatterns.TakeLast(Math.Min(lastN, _playerPatterns.Count)).ToList();
        if (recent.Count < 3) return GetMostFrequentPlayerAction(lastN);

        var last3 = recent.TakeLast(3).Select(p => p.PlayerAction).ToList();
        var transitions = new Dictionary<(AIAction, AIAction), Dictionary<AIAction, int>>();

        for (int i = 0; i < recent.Count - 2; i++)
        {
            var key = (recent[i].PlayerAction, recent[i + 1].PlayerAction);
            var next = recent[i + 2].PlayerAction;

            if (!transitions.ContainsKey(key))
                transitions[key] = new Dictionary<AIAction, int>();

            transitions[key].TryGetValue(next, out int count);
            transitions[key][next] = count + 1;
        }

        var lastKey = (last3[1], last3[2]);
        if (transitions.TryGetValue(lastKey, out var nextActions))
        {
            return nextActions.OrderByDescending(kv => kv.Value).First().Key;
        }

        return GetMostFrequentPlayerAction(lastN);
    }

    public double GetAverageDamageDealt(int lastN = 10)
    {
        if (_recentDamageDealt.Count == 0) return 0;
        return _recentDamageDealt.TakeLast(Math.Min(lastN, _recentDamageDealt.Count)).Average();
    }

    public double GetAverageDamageTaken(int lastN = 10)
    {
        if (_recentDamageTaken.Count == 0) return 0;
        return _recentDamageTaken.TakeLast(Math.Min(lastN, _recentDamageTaken.Count)).Average();
    }

    public Combat.CombatState GetMostFrequentPlayerState(int lastN = 30)
    {
        var recent = _playerPatterns.TakeLast(Math.Min(lastN, _playerPatterns.Count));
        if (!recent.Any()) return Combat.CombatState.Idle;
        return recent.GroupBy(p => p.State).OrderByDescending(g => g.Count()).First().Key;
    }

    public double GetPlayerAggression(int lastN = 20)
    {
        var recentList = _playerPatterns.TakeLast(Math.Min(lastN, _playerPatterns.Count)).ToList();
        if (recentList.Count == 0) return 0.5;
        int attacks = recentList.Count(p => p.PlayerAction == AIAction.Attack || p.PlayerAction == AIAction.HeavyAttack);
        return (double)attacks / recentList.Count;
    }

    public void Clear()
    {
        _actionHistory.Clear();
        _playerPatterns.Clear();
        _actionCounts.Clear();
        _actionSuccessRates.Clear();
        _actionDamageDealt.Clear();
        _actionDamageTaken.Clear();
        _playerStateCounts.Clear();
        _recentDamageTaken.Clear();
        _recentDamageDealt.Clear();
    }
}
