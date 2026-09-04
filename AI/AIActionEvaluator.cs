using System.Windows;

namespace SimpleWPFGame.AI;

public class AIActionEvaluator
{
    private readonly Random _rng = new();

    public double Score(AIAction action, AIContext context, AIMemory memory, AIPredictor predictor, AIBrain brain)
    {
        double baseScore = GetBaseScore(action, context, brain);
        double situationalBonus = GetSituationalBonus(action, context, memory);
        double learnedBonus = GetLearnedBonus(action, memory, brain);
        double predictionBonus = GetPredictionBonus(action, context, predictor, brain);
        double survivalBonus = GetSurvivalBonus(action, context);
        double aggressionBonus = GetAggressionBonus(action, context, brain);
        double noise = (_rng.NextDouble() - 0.5) * 0.1 * (1.0 - brain.ReactionSpeed);

        return baseScore + situationalBonus + learnedBonus + predictionBonus + survivalBonus + aggressionBonus + noise;
    }

    private double GetBaseScore(AIAction action, AIContext context, AIBrain brain)
    {
        return action switch
        {
            AIAction.MoveToward => 0.3 + brain.Aggression * 0.2,
            AIAction.MoveAway => 0.2 + brain.Caution * 0.2,
            AIAction.MoveLeft => 0.15,
            AIAction.MoveRight => 0.15,
            AIAction.Attack => 0.4 + brain.Aggression * 0.3,
            AIAction.HeavyAttack => 0.3 + brain.Aggression * 0.2,
            AIAction.Dodge => 0.3 + brain.Caution * 0.3,
            AIAction.Block => 0.25 + brain.Caution * 0.35,
            AIAction.Parry => 0.2 + brain.ReactionSpeed * 0.3,
            AIAction.CounterAttack => 0.15 + brain.ReactionSpeed * 0.2,
            AIAction.Feint => 0.1 + brain.PredictionAccuracy * 0.2,
            AIAction.Reposition => 0.2,
            AIAction.Wait => 0.1,
            AIAction.Aggressive => 0.2 + brain.Aggression * 0.3,
            AIAction.Defensive => 0.2 + brain.Caution * 0.3,
            AIAction.Retreat => 0.15 + brain.Caution * 0.2,
            _ => 0
        };
    }

    private double GetSituationalBonus(AIAction action, AIContext context, AIMemory memory)
    {
        double bonus = 0;
        double dist = context.DistanceToTarget;
        double hpRatio = context.SelfHP / Math.Max(1, context.SelfMaxHP);
        double targetHpRatio = context.TargetHP / Math.Max(1, context.TargetMaxHP);

        switch (action)
        {
            case AIAction.Attack:
                if (dist < 100) bonus += 0.3;
                else if (dist < 150) bonus += 0.15;
                else bonus -= 0.2;
                if (context.IsTargetAttacking) bonus -= 0.1;
                if (context.IsTargetDodging) bonus -= 0.3;
                break;

            case AIAction.HeavyAttack:
                if (dist < 120) bonus += 0.2;
                if (context.IsTargetBlocking) bonus += 0.2;
                if (context.IsTargetAttacking) bonus -= 0.2;
                break;

            case AIAction.Dodge:
                if (context.IsTargetAttacking) bonus += 0.4;
                if (hpRatio < 0.3) bonus += 0.2;
                if (context.IsCornered) bonus -= 0.1;
                break;

            case AIAction.Block:
                if (context.IsTargetAttacking) bonus += 0.35;
                if (hpRatio < 0.5) bonus += 0.1;
                break;

            case AIAction.Parry:
                if (context.IsTargetAttacking) bonus += 0.3;
                if (memory.GetPlayerAggression() > 0.6) bonus += 0.2;
                break;

            case AIAction.CounterAttack:
                if (context.SelfCombatState == Combat.CombatState.Parrying) bonus += 0.5;
                if (context.SelfCombatState == Combat.CombatState.Countering) bonus += 0.3;
                break;

            case AIAction.MoveToward:
                if (dist > 200) bonus += 0.3;
                if (dist < 80) bonus -= 0.1;
                break;

            case AIAction.MoveAway:
                if (dist < 80) bonus += 0.2;
                if (hpRatio < 0.3) bonus += 0.3;
                if (context.IsCornered) bonus -= 0.3;
                break;

            case AIAction.Reposition:
                if (context.IsCornered) bonus += 0.4;
                if (dist < 100) bonus += 0.1;
                break;

            case AIAction.Feint:
                if (context.IsTargetBlocking) bonus += 0.25;
                if (context.IsTargetParrying) bonus += 0.3;
                break;

            case AIAction.Retreat:
                if (hpRatio < 0.25) bonus += 0.4;
                if (targetHpRatio > 0.8) bonus += 0.2;
                break;

            case AIAction.Aggressive:
                if (targetHpRatio < 0.3) bonus += 0.4;
                if (hpRatio > 0.7) bonus += 0.2;
                break;

            case AIAction.Defensive:
                if (hpRatio < 0.4) bonus += 0.3;
                if (memory.GetAverageDamageTaken() > memory.GetAverageDamageDealt() * 2)
                    bonus += 0.2;
                break;
        }

        return bonus;
    }

    private double GetLearnedBonus(AIAction action, AIMemory memory, AIBrain brain)
    {
        double successRate = memory.GetActionSuccessRate(action);
        double effectiveness = memory.GetAverageDamageDealt() / Math.Max(0.1, memory.GetAverageDamageTaken());

        return (successRate - 0.5) * brain.LearningRate * 2 + (effectiveness - 1.0) * brain.LearningRate;
    }

    private double GetPredictionBonus(AIAction action, AIContext context, AIPredictor predictor, AIBrain brain)
    {
        if (predictor.Confidence < 0.3) return 0;

        double bonus = 0;
        var predicted = predictor.PredictedAction;

        if (action == AIAction.Parry && predicted == AIAction.Attack)
            bonus += 0.3 * predictor.Confidence * brain.PredictionAccuracy;
        else if (action == AIAction.Dodge && (predicted == AIAction.Attack || predicted == AIAction.HeavyAttack))
            bonus += 0.25 * predictor.Confidence * brain.PredictionAccuracy;
        else if (action == AIAction.Attack && predicted == AIAction.Dodge)
            bonus -= 0.15 * predictor.Confidence;
        else if (action == AIAction.Block && predicted == AIAction.HeavyAttack)
            bonus += 0.2 * predictor.Confidence;
        else if (action == AIAction.Wait && predicted == AIAction.Parry)
            bonus += 0.2 * predictor.Confidence;

        return bonus;
    }

    private double GetSurvivalBonus(AIAction action, AIContext context)
    {
        double hpRatio = context.SelfHP / Math.Max(1, context.SelfMaxHP);
        double bonus = 0;

        if (hpRatio < 0.2)
        {
            bonus += action switch
            {
                AIAction.Retreat => 0.4,
                AIAction.Dodge => 0.3,
                AIAction.Block => 0.2,
                AIAction.Defensive => 0.25,
                AIAction.Attack => -0.2,
                AIAction.HeavyAttack => -0.3,
                _ => 0
            };
        }
        else if (hpRatio < 0.5)
        {
            bonus += action switch
            {
                AIAction.Dodge => 0.15,
                AIAction.Block => 0.1,
                AIAction.Defensive => 0.15,
                _ => 0
            };
        }

        return bonus;
    }

    private double GetAggressionBonus(AIAction action, AIContext context, AIBrain brain)
    {
        double targetHpRatio = context.TargetHP / Math.Max(1, context.TargetMaxHP);
        double bonus = 0;

        if (targetHpRatio < 0.2 && brain.Aggression > 0.5)
        {
            bonus += action switch
            {
                AIAction.Attack => 0.3,
                AIAction.HeavyAttack => 0.2,
                AIAction.Aggressive => 0.25,
                AIAction.Retreat => -0.2,
                _ => 0
            };
        }

        if (context.HasHealthAdvantage && brain.Aggression > 0.6)
        {
            bonus += action switch
            {
                AIAction.Attack => 0.15,
                AIAction.MoveToward => 0.1,
                _ => 0
            };
        }

        return bonus;
    }
}
