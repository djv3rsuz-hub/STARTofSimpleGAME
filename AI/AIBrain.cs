namespace SimpleWPFGame.AI;

public enum AIAction
{
    None,
    MoveToward,
    MoveAway,
    MoveLeft,
    MoveRight,
    Attack,
    HeavyAttack,
    Dodge,
    Block,
    Parry,
    CounterAttack,
    Feint,
    Reposition,
    Wait,
    Aggressive,
    Defensive,
    Retreat
}

public enum AIPersonality
{
    Aggressive,
    Defensive,
    Balanced,
    Berserker,
    Assassin,
    Tank,
    ParryMaster,
    DodgeMaster,
    CounterMaster,
    Adaptive
}

public enum AIDifficulty
{
    Easy,
    Normal,
    Hard,
    Expert,
    Nightmare,
    Adaptive
}

public class AIBrain
{
    private static AIBrain? _instance;
    public static AIBrain Instance => _instance ??= new AIBrain();

    public AIPersonality Personality { get; set; } = AIPersonality.Balanced;
    public AIDifficulty Difficulty { get; set; } = AIDifficulty.Normal;
    public double Aggression { get; set; } = 0.5;
    public double Caution { get; set; } = 0.5;
    public double ReactionSpeed { get; set; } = 0.5;
    public double PredictionAccuracy { get; set; } = 0.3;
    public double LearningRate { get; set; } = 0.1;

    public AIMemory Memory { get; } = new();
    public AIPredictor Predictor { get; } = new();
    public AILearner Learner { get; } = new();
    public AIActionEvaluator Evaluator { get; } = new();

    private readonly Dictionary<AIAction, double> _actionScores = new();
    private AIAction _currentDecision = AIAction.None;
    private double _decisionTimer;
    private double _decisionCooldown = 0.15;
    private int _consecutiveSameAction;
    private AIAction _lastAction = AIAction.None;
    private readonly Random _rng = new();

    private AIBrain() { }

    public void Initialize(AIPersonality personality, AIDifficulty difficulty)
    {
        Personality = personality;
        Difficulty = difficulty;
        ApplyPersonalityDefaults();
        ApplyDifficultyScaling();
    }

    private void ApplyPersonalityDefaults()
    {
        switch (Personality)
        {
            case AIPersonality.Aggressive:
                Aggression = 0.85; Caution = 0.2; ReactionSpeed = 0.6; break;
            case AIPersonality.Defensive:
                Aggression = 0.25; Caution = 0.85; ReactionSpeed = 0.7; break;
            case AIPersonality.Balanced:
                Aggression = 0.5; Caution = 0.5; ReactionSpeed = 0.5; break;
            case AIPersonality.Berserker:
                Aggression = 0.95; Caution = 0.05; ReactionSpeed = 0.4; break;
            case AIPersonality.Assassin:
                Aggression = 0.7; Caution = 0.6; ReactionSpeed = 0.85; break;
            case AIPersonality.Tank:
                Aggression = 0.4; Caution = 0.7; ReactionSpeed = 0.3; break;
            case AIPersonality.ParryMaster:
                Aggression = 0.5; Caution = 0.6; ReactionSpeed = 0.9; break;
            case AIPersonality.DodgeMaster:
                Aggression = 0.5; Caution = 0.7; ReactionSpeed = 0.85; break;
            case AIPersonality.CounterMaster:
                Aggression = 0.4; Caution = 0.65; ReactionSpeed = 0.8; break;
            case AIPersonality.Adaptive:
                Aggression = 0.5; Caution = 0.5; ReactionSpeed = 0.6; break;
        }
    }

    private void ApplyDifficultyScaling()
    {
        switch (Difficulty)
        {
            case AIDifficulty.Easy:
                ReactionSpeed *= 0.5; PredictionAccuracy *= 0.2; LearningRate *= 0.1; break;
            case AIDifficulty.Normal:
                ReactionSpeed *= 0.75; PredictionAccuracy *= 0.4; LearningRate *= 0.3; break;
            case AIDifficulty.Hard:
                ReactionSpeed *= 1.0; PredictionAccuracy *= 0.6; LearningRate *= 0.5; break;
            case AIDifficulty.Expert:
                ReactionSpeed *= 1.2; PredictionAccuracy *= 0.8; LearningRate *= 0.7; break;
            case AIDifficulty.Nightmare:
                ReactionSpeed *= 1.5; PredictionAccuracy *= 0.95; LearningRate *= 1.0; break;
            case AIDifficulty.Adaptive:
                break;
        }
    }

    public AIAction Decide(AIContext context)
    {
        _decisionTimer -= context.DeltaTime;
        if (_decisionTimer > 0) return _currentDecision;
        _decisionTimer = _decisionCooldown * (1.0 - ReactionSpeed * 0.5);

        Memory.Update(context);
        Predictor.Update(context, Memory);
        Learner.Update(context, Memory, PredictionAccuracy);

        _actionScores.Clear();
        foreach (AIAction action in Enum.GetValues(typeof(AIAction)))
        {
            if (action == AIAction.None) continue;
            _actionScores[action] = Evaluator.Score(action, context, Memory, Predictor, this);
        }

        if (Difficulty == AIDifficulty.Adaptive)
        {
            Learner.AdjustScores(_actionScores, context);
        }

        _actionScores[AIAction.None] = 0.1;

        AIAction best = AIAction.None;
        double bestScore = double.MinValue;
        foreach (var kv in _actionScores)
        {
            if (kv.Value > bestScore)
            {
                bestScore = kv.Value;
                best = kv.Key;
            }
        }

        if (best == _lastAction)
            _consecutiveSameAction++;
        else
            _consecutiveSameAction = 0;

        if (_consecutiveSameAction > 3 && _rng.NextDouble() < 0.3)
        {
            var alternatives = _actionScores.Where(kv => kv.Key != best && kv.Value > bestScore * 0.5)
                .OrderByDescending(kv => kv.Value).ToList();
            if (alternatives.Count > 0)
                best = alternatives[_rng.Next(alternatives.Count)].Key;
            _consecutiveSameAction = 0;
        }

        _lastAction = best;
        _currentDecision = best;
        return best;
    }

    public void Reset()
    {
        Memory.Clear();
        Predictor.Clear();
        Learner.Clear();
        _actionScores.Clear();
        _currentDecision = AIAction.None;
        _lastAction = AIAction.None;
        _consecutiveSameAction = 0;
        _decisionTimer = 0;
    }

    public Dictionary<AIAction, double> GetDebugScores(AIContext context)
    {
        var scores = new Dictionary<AIAction, double>();
        foreach (AIAction action in Enum.GetValues(typeof(AIAction)))
        {
            if (action == AIAction.None) continue;
            scores[action] = Evaluator.Score(action, context, Memory, Predictor, this);
        }
        return scores;
    }
}
