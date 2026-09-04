using System.Windows;

namespace SimpleWPFGame.AI;

public class AIPredictor
{
    private AIAction _predictedAction = AIAction.None;
    private double _predictionConfidence;
    private Vector _predictedPosition;
    private double _predictedTime;
    private int _correctPredictions;
    private int _totalPredictions;

    public AIAction PredictedAction => _predictedAction;
    public double Confidence => _predictionConfidence;
    public Vector PredictedPosition => _predictedPosition;
    public double PredictionAccuracy => _totalPredictions > 0 ? _correctPredictions / (double)_totalPredictions : 0;

    public void Update(AIContext context, AIMemory memory)
    {
        _predictedAction = memory.PredictNextPlayerAction();
        _predictionConfidence = CalculateConfidence(memory);
        _predictedPosition = PredictTargetPosition(context);
    }

    public void ValidatePrediction(AIAction actualAction)
    {
        _totalPredictions++;
        if (actualAction == _predictedAction)
            _correctPredictions++;
    }

    private double CalculateConfidence(AIMemory memory)
    {
        int patternCount = memory.TotalPlayerPatterns;
        if (patternCount < 5) return 0.2;
        if (patternCount < 20) return 0.4;
        if (patternCount < 50) return 0.6;
        return Math.Min(0.9, 0.6 + (patternCount - 50) * 0.005);
    }

    private Vector PredictTargetPosition(AIContext context)
    {
        double predictTime = 0.3;
        return context.TargetPosition + context.TargetVelocity * predictTime;
    }

    public bool ShouldCounterAttack(AIContext context, double reactionThreshold)
    {
        if (_predictedAction != AIAction.Attack && _predictedAction != AIAction.HeavyAttack)
            return false;
        if (_predictionConfidence < reactionThreshold)
            return false;

        double dist = context.DistanceToTarget;
        double attackRange = 150;
        return dist < attackRange * 1.5;
    }

    public bool ShouldDodgePrediction(AIContext context, double reactionThreshold)
    {
        if (!ShouldCounterAttack(context, reactionThreshold))
            return false;

        double healthPercent = context.SelfHP / Math.Max(1, context.SelfMaxHP);
        return healthPercent < 0.5 || _predictionConfidence > 0.7;
    }

    public bool ShouldParryPrediction(AIContext context, double reactionThreshold)
    {
        if (_predictedAction != AIAction.Attack)
            return false;
        if (_predictionConfidence < reactionThreshold * 1.2)
            return false;

        double dist = context.DistanceToTarget;
        return dist < 120;
    }

    public AIAction GetCounterAction(AIContext context)
    {
        return _predictedAction switch
        {
            AIAction.Attack => AIAction.Parry,
            AIAction.HeavyAttack => AIAction.Dodge,
            AIAction.Dodge => AIAction.Wait,
            AIAction.Block => AIAction.Feint,
            AIAction.Parry => AIAction.Wait,
            AIAction.MoveToward => AIAction.Attack,
            _ => AIAction.Block
        };
    }

    public Vector GetPredictedDodgeDirection(AIContext context)
    {
        Vector toTarget = context.TargetPosition - context.SelfPosition;
        if (toTarget.Length < 0.1) return new Vector(1, 0);
        toTarget.Normalize();

        double cross = toTarget.X * Math.Sin(context.TargetFacingAngle) - toTarget.Y * Math.Cos(context.TargetFacingAngle);
        Vector perp = new(-toTarget.Y, toTarget.X);

        if (cross > 0)
            return perp;
        else
            return new Vector(-perp.X, -perp.Y);
    }

    public void Clear()
    {
        _predictedAction = AIAction.None;
        _predictionConfidence = 0;
        _predictedPosition = default;
        _correctPredictions = 0;
        _totalPredictions = 0;
    }
}
