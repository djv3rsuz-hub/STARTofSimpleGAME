using System.Windows.Media;

namespace SimpleWPFGame.AI;

public class AIDebugInfo
{
    public AIAction CurrentAction { get; set; }
    public double ActionTimer { get; set; }
    public double HPPercent { get; set; }
    public double TargetHPPercent { get; set; }
    public double DistanceToTarget { get; set; }
    public AIPersonality Personality { get; set; }
    public AIDifficulty Difficulty { get; set; }
    public double Aggression { get; set; }
    public double Caution { get; set; }
    public double PredictionConfidence { get; set; }
    public AIAction PredictedPlayerAction { get; set; }
    public double AdaptationLevel { get; set; }
    public double PlayerSkillEstimate { get; set; }
    public Dictionary<AIAction, double> ActionScores { get; set; } = new();
}

public class AIDebugRenderer
{
    private static AIDebugRenderer? _instance;
    public static AIDebugRenderer Instance => _instance ??= new AIDebugRenderer();

    private AIDebugInfo? _lastInfo;
    private readonly Queue<string> _decisionLog = new(20);

    public AIDebugInfo? LastInfo => _lastInfo;
    public IEnumerable<string> DecisionLog => _decisionLog;

    private AIDebugRenderer() { }

    public void Update(AIController controller)
    {
        if (controller.Brain == null) return;

        var brain = controller.Brain;
        _lastInfo = new AIDebugInfo
        {
            CurrentAction = controller.CurrentAction,
            ActionTimer = controller.IsThinking ? 1.0 : 0,
            Personality = brain.Personality,
            Difficulty = brain.Difficulty,
            Aggression = brain.Aggression,
            Caution = brain.Caution,
            PredictionConfidence = brain.Predictor.Confidence,
            PredictedPlayerAction = brain.Predictor.PredictedAction,
            AdaptationLevel = brain.Learner.AdaptationLevel,
            PlayerSkillEstimate = brain.Learner.PlayerSkillEstimate
        };

        string logEntry = $"[{DateTime.Now:HH:mm:ss}] Action: {controller.CurrentAction} | " +
            $"Predict: {brain.Predictor.PredictedAction} ({brain.Predictor.Confidence:P0}) | " +
            $"Adapt: {brain.Learner.AdaptationLevel:F2}";

        _decisionLog.Enqueue(logEntry);
        if (_decisionLog.Count > 20) _decisionLog.Dequeue();
    }

    public string GetStatusText()
    {
        if (_lastInfo == null) return "AI: No data";
        var info = _lastInfo;
        return $"AI [{info.Personality}] [{info.Difficulty}]\n" +
            $"Action: {info.CurrentAction}\n" +
            $"Aggr: {info.Aggression:F2} | Caution: {info.Caution:F2}\n" +
            $"Predict: {info.PredictedPlayerAction} ({info.PredictionConfidence:P0})\n" +
            $"Adapt: {info.AdaptationLevel:F2} | Skill: {info.PlayerSkillEstimate:F2}";
    }

    public string GetDetailedStatus()
    {
        if (_lastInfo == null) return "No AI data";
        var info = _lastInfo;
        var lines = new List<string>
        {
            $"=== AI Brain Status ===",
            $"Personality: {info.Personality}",
            $"Difficulty: {info.Difficulty}",
            $"Current Action: {info.CurrentAction}",
            $"Aggression: {info.Aggression:F2}",
            $"Caution: {info.Caution:F2}",
            $"Prediction Confidence: {info.PredictionConfidence:P0}",
            $"Predicted Player: {info.PredictedPlayerAction}",
            $"Adaptation Level: {info.AdaptationLevel:F2}",
            $"Player Skill Est: {info.PlayerSkillEstimate:F2}",
            "",
            "--- Action Scores ---"
        };

        foreach (var kv in info.ActionScores.OrderByDescending(x => x.Value))
        {
            string bar = new string('#', (int)(kv.Value * 20));
            lines.Add($"  {kv.Key,-15} {kv.Value:F3} {bar}");
        }

        return string.Join("\n", lines);
    }

    public void Clear()
    {
        _lastInfo = null;
        _decisionLog.Clear();
    }
}
