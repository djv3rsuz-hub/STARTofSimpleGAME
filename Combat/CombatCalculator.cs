using System.Windows;
using SimpleWPFGame.Config;

namespace SimpleWPFGame.Combat;

public static class CombatCalculator
{
    private static readonly Random _rng = new();

    public static AttackResult CalculateAttack(
        CharacterStats attacker,
        CharacterStats defender,
        Weapon weapon,
        int comboIndex,
        bool defenderBlocking,
        bool defenderParrying,
        bool defenderPerfectParrying,
        bool defenderDodging,
        bool defenderPerfectDodging,
        int defenderParryFrame,
        int defenderDodgeFrame,
        FrameData attackFrameData)
    {
        var result = new AttackResult { ComboIndex = comboIndex };

        if (defenderPerfectDodging && defenderDodgeFrame <= attackFrameData.PerfectDodgeWindowFrames)
        {
            result.PerfectDodged = true;
            result.Dodged = true;
            result.DefenseUsed = DefenseType.PerfectDodge;
            result.HitEffect = "perfect_dodge";
            return result;
        }

        if (defenderDodging && defenderDodgeFrame < attackFrameData.DodgeIframeFrames)
        {
            result.Dodged = true;
            result.DefenseUsed = DefenseType.Dodge;
            result.HitEffect = "dodge";
            return result;
        }

        if (defenderPerfectParrying && defenderParryFrame <= attackFrameData.PerfectParryWindowFrames)
        {
            result.PerfectParried = true;
            result.Parried = true;
            result.DefenseUsed = DefenseType.PerfectParry;
            result.HitEffect = "perfect_parry";
            result.DamageBlocked = result.DamageDealt;
            return result;
        }

        if (defenderParrying && defenderParryFrame >= attackFrameData.ParryWindowStart
            && defenderParryFrame < attackFrameData.ParryWindowStart + attackFrameData.ParryWindowFrames)
        {
            result.Parried = true;
            result.DefenseUsed = DefenseType.Parry;
            result.HitEffect = "parry";
            result.DamageBlocked = result.DamageDealt;
            return result;
        }

        if (defenderBlocking)
        {
            double blockReduction = 0.6 + defender.Defence * 0.3;
            result.Blocked = true;
            result.DefenseUsed = DefenseType.Block;
            result.HitEffect = "block";
        }

        double baseDamage = weapon.BaseDamage + attacker.Attack * 50;
        baseDamage += comboIndex * weapon.BaseDamage * 0.2;

        bool isCrit = _rng.NextDouble() < attacker.CriticalChance;
        if (isCrit)
        {
            baseDamage *= weapon.CritMultiplier + attacker.CriticalDamage;
            result.Crit = true;
        }

        double defense = defender.Defence * 40;
        double resistance = defender.Resistance * 30;
        double damage = Math.Max(1, baseDamage - defense - resistance);

        if (result.Blocked)
        {
            double blockAmount = damage * result.DefenseUsed switch
            {
                DefenseType.Block => 0.6 + defender.Defence * 0.3,
                _ => 0
            };
            result.DamageBlocked = blockAmount;
            damage -= blockAmount;
        }

        result.Hit = true;
        result.DamageDealt = Math.Max(1, damage);
        result.HitEffect = result.Crit ? "crit_hit" : "hit";

        double kbAngle = _rng.NextDouble() * Math.PI * 2;
        result.Knockback = new Vector(
            Math.Cos(kbAngle) * weapon.KnockbackForce,
            Math.Sin(kbAngle) * weapon.KnockbackForce);

        return result;
    }

    public static bool RollDodge(double dodgeChance)
    {
        return _rng.NextDouble() < dodgeChance;
    }

    public static bool RollPerfectDodge(double dodgeChance)
    {
        double perfectChance = dodgeChance * 0.3;
        return _rng.NextDouble() < perfectChance;
    }

    public static bool RollParry(double parryChance)
    {
        return _rng.NextDouble() < parryChance;
    }

    public static bool RollPerfectParry(double parryChance)
    {
        double perfectChance = parryChance * 0.25;
        return _rng.NextDouble() < perfectChance;
    }

    public static double CalculateComboDamageMultiplier(int comboIndex, int comboLength)
    {
        if (comboLength <= 1) return 1.0;
        double baseMultiplier = 1.0 + comboIndex * 0.15;
        if (comboIndex == comboLength - 1)
            baseMultiplier += 0.25;
        return baseMultiplier;
    }

    public static Vector CalculateKnockback(Vector attackerPos, Vector defenderPos, double force)
    {
        var dir = defenderPos - attackerPos;
        if (dir.Length < 0.01)
            dir = new Vector(1, 0);
        else
            dir /= dir.Length;
        return dir * force;
    }

    public static double CalculateDodgeFrameWindow(FrameData attackData, double reactionTime)
    {
        double attackDuration = attackData.TotalSeconds;
        double dodgeWindow = attackDuration * 0.4;
        return Math.Max(0.05, dodgeWindow - reactionTime * 0.5);
    }

    public static double CalculateParryFrameWindow(FrameData attackData)
    {
        return attackData.ParryWindowFrames / 60.0;
    }

    public static double CalculatePerfectParryWindow(FrameData attackData)
    {
        return attackData.PerfectParryWindowFrames / 60.0;
    }

    public static bool CheckPixelPerfectParry(
        Vector attackerPos, Vector defenderPos,
        double attackerRotation, double range,
        int currentFrame, FrameData attackData)
    {
        if (currentFrame < attackData.ParryWindowStart ||
            currentFrame >= attackData.ParryWindowStart + attackData.ParryWindowFrames)
            return false;

        double dist = (defenderPos - attackerPos).Length;
        bool inRange = dist <= range * 1.2;

        int frameInWindow = currentFrame - attackData.ParryWindowStart;
        bool isPerfect = frameInWindow < attackData.PerfectParryWindowFrames;

        return inRange && isPerfect;
    }

    public static bool CheckPixelPerfectDodge(
        Vector attackDir, Vector defenderVel,
        int currentFrame, FrameData attackData)
    {
        if (currentFrame >= attackData.PerfectDodgeWindowFrames)
            return false;

        double velMag = defenderVel.Length;
        bool moving = velMag > 10;

        if (!moving) return false;

        double velAngle = Math.Atan2(defenderVel.Y, defenderVel.X);
        double attackAngle = Math.Atan2(attackDir.Y, attackDir.X);
        double angleDiff = Math.Abs(velAngle - attackAngle);
        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

        return angleDiff > Math.PI * 0.6;
    }
}
