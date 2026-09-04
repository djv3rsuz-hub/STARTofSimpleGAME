namespace SimpleWPFGame.AI;

public class AILearner
{
    private double _adaptationLevel;
    private double _playerSkillEstimate;
    private double _winRate;
    private int _wins;
    private int _losses;
    private int _draws;
    private readonly Queue<double> _recentPerformance = new(20);
    private readonly Dictionary<AIAction, double> _learnedActionWeights = new();
    private double _learningMomentum;

    public double AdaptationLevel => _adaptationLevel;
    public double PlayerSkillEstimate => _playerSkillEstimate;
    public double WinRate => _wins + _losses > 0 ? _wins / (double)(_wins + _losses) : 0.5;

    public void Update(AIContext context, AIMemory memory, double predictionAccuracy)
    {
        double hpRatio = context.SelfHP / Math.Max(1, context.SelfMaxHP);
        double targetHpRatio = context.TargetHP / Math.Max(1, context.TargetMaxHP);

        double performance = 0;
        if (hpRatio > targetHpRatio) performance += 0.3;
        if (memory.GetAverageDamageDealt() > memory.GetAverageDamageTaken()) performance += 0.3;
        performance += predictionAccuracy * 0.2;
        performance += hpRatio * 0.2;

        _recentPerformance.Enqueue(performance);
        if (_recentPerformance.Count > 20) _recentPerformance.Dequeue();

        double avgPerf = _recentPerformance.Average();
        _adaptationLevel = Math.Clamp(_adaptationLevel + (avgPerf - 0.5) * 0.02, 0, 1);

        double playerAggression = memory.GetPlayerAggression();
        double playerSkill = 0.5;
        if (memory.TotalPlayerPatterns > 10)
        {
            double dodgeRate = memory.GetActionSuccessRate(AIAction.Dodge);
            double parryRate = memory.GetActionSuccessRate(AIAction.Parry);
            playerSkill = (dodgeRate + parryRate) * 0.5 + 0.3;
        }
        _playerSkillEstimate = _playerSkillEstimate * 0.95 + playerSkill * 0.05;

        UpdateLearnedWeights(memory);
    }

    private void UpdateLearnedWeights(AIMemory memory)
    {
        foreach (AIAction action in Enum.GetValues(typeof(AIAction)))
        {
            if (action == AIAction.None) continue;

            double successRate = memory.GetActionSuccessRate(action);
            double effectiveness = memory.GetAverageDamageDealt() / Math.Max(0.1, memory.GetAverageDamageTaken());

            double weight = successRate * 0.4 + effectiveness * 0.3 + _adaptationLevel * 0.3;
            _learnedActionWeights.TryGetValue(action, out double oldWeight);
            _learnedActionWeights[action] = oldWeight * 0.9 + weight * 0.1;
        }
    }

    public void AdjustScores(Dictionary<AIAction, double> scores, AIContext context)
    {
        double hpRatio = context.SelfHP / Math.Max(1, context.SelfMaxHP);
        double targetHpRatio = context.TargetHP / Math.Max(1, context.TargetMaxHP);

        foreach (var action in scores.Keys.ToList())
        {
            double adjustment = 0;

            if (_learnedActionWeights.TryGetValue(action, out double weight))
            {
                adjustment += (weight - 0.5) * _adaptationLevel * 0.3;
            }

            if (hpRatio < 0.3)
            {
                if (action == AIAction.Retreat || action == AIAction.Defensive)
                    adjustment += 0.2;
                if (action == AIAction.Aggressive || action == AIAction.Attack)
                    adjustment -= 0.15;
            }

            if (targetHpRatio < 0.2)
            {
                if (action == AIAction.Attack || action == AIAction.Aggressive)
                    adjustment += 0.25;
                if (action == AIAction.Retreat || action == AIAction.Defensive)
                    adjustment -= 0.1;
            }

            if (_adaptationLevel > 0.5)
            {
                if (action == AIAction.CounterAttack)
                    adjustment += _adaptationLevel * 0.15;
                if (action == AIAction.Feint)
                    adjustment += _adaptationLevel * 0.1;
            }

            scores[action] += adjustment;
        }
    }

    public void RecordWin() { _wins++; _recentPerformance.Enqueue(1.0); }
    public void RecordLoss() { _losses++; _recentPerformance.Enqueue(0.0); }
    public void RecordDraw() { _draws++; _recentPerformance.Enqueue(0.5); }

    public double GetDifficultyModifier()
    {
        return 1.0 + _adaptationLevel * 0.5 + _playerSkillEstimate * 0.3;
    }

    public AIAction GetLearnedCounter(AIAction playerAction)
    {
        return playerAction switch
        {
            AIAction.Attack => _adaptationLevel > 0.5 ? AIAction.Parry : AIAction.Block,
            AIAction.HeavyAttack => AIAction.Dodge,
            AIAction.Dodge => AIAction.Wait,
            AIAction.Block => _adaptationLevel > 0.7 ? AIAction.Feint : AIAction.HeavyAttack,
            AIAction.Parry => AIAction.Wait,
            AIAction.MoveToward => AIAction.Attack,
            AIAction.MoveAway => AIAction.MoveToward,
            _ => AIAction.None
        };
    }

    public void Clear()
    {
        _adaptationLevel = 0;
        _playerSkillEstimate = 0.5;
        _wins = 0;
        _losses = 0;
        _draws = 0;
        _recentPerformance.Clear();
        _learnedActionWeights.Clear();
        _learningMomentum = 0;
    }
}
