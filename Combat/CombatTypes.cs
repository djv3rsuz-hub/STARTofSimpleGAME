using System.Windows;
using SimpleWPFGame.Config;

namespace SimpleWPFGame.Combat;

public enum CombatState
{
    Idle,
    Attacking,
    ComboAttacking,
    Dodging,
    Blocking,
    Parrying,
    Countering,
    Stunned,
    Dead
}

public enum AttackType
{
    Slash,
    Thrust,
    Spin,
    Heavy,
    Light
}

public enum DefenseType
{
    None,
    Block,
    Parry,
    PerfectParry,
    Dodge,
    PerfectDodge
}

public struct FrameData
{
    public int StartupFrames;
    public int ActiveFrames;
    public int RecoveryFrames;
    public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;
    public double TotalSeconds => TotalFrames / 60.0;

    public int ParryWindowStart;
    public int ParryWindowFrames;
    public int PerfectParryWindowFrames;

    public int DodgeIframeFrames;
    public int PerfectDodgeWindowFrames;

    public static FrameData SwordSlash => new()
    {
        StartupFrames = 4,
        ActiveFrames = 6,
        RecoveryFrames = 8,
        ParryWindowStart = 2,
        ParryWindowFrames = 8,
        PerfectParryWindowFrames = 3,
        DodgeIframeFrames = 12,
        PerfectDodgeWindowFrames = 4
    };

    public static FrameData SwordHeavy => new()
    {
        StartupFrames = 8,
        ActiveFrames = 8,
        RecoveryFrames = 14,
        ParryWindowStart = 4,
        ParryWindowFrames = 10,
        PerfectParryWindowFrames = 4,
        DodgeIframeFrames = 12,
        PerfectDodgeWindowFrames = 4
    };

    public static FrameData SwordThrust => new()
    {
        StartupFrames = 3,
        ActiveFrames = 4,
        RecoveryFrames = 6,
        ParryWindowStart = 1,
        ParryWindowFrames = 6,
        PerfectParryWindowFrames = 2,
        DodgeIframeFrames = 10,
        PerfectDodgeWindowFrames = 3
    };
}

public struct Hitbox
{
    public Rect Bounds;
    public int ActiveFrameStart;
    public int ActiveFrameEnd;
    public double KnockbackForce;
    public DamageType DamageType;
    public bool IsActive;

    public Hitbox(Rect bounds, int activeStart, int activeEnd, double knockback = 0)
    {
        Bounds = bounds;
        ActiveFrameStart = activeStart;
        ActiveFrameEnd = activeEnd;
        KnockbackForce = knockback;
        DamageType = DamageType.Physical;
        IsActive = false;
    }

    public bool CheckFrame(int currentFrame)
        => currentFrame >= ActiveFrameStart && currentFrame <= ActiveFrameEnd;

    public bool Intersects(Hitbox other)
        => IsActive && other.IsActive && Bounds.IntersectsWith(other.Bounds);
}

public struct AttackResult
{
    public bool Hit;
    public bool Crit;
    public bool Dodged;
    public bool PerfectDodged;
    public bool Parried;
    public bool PerfectParried;
    public bool Blocked;
    public bool Deflected;
    public bool Countered;
    public double DamageDealt;
    public double DamageBlocked;
    public double HealAmount;
    public DefenseType DefenseUsed;
    public Vector Knockback;
    public int ComboIndex;
    public string HitEffect;

    public static AttackResult Miss => new() { Hit = false, HitEffect = "miss" };
    public static AttackResult PerfectParryResult => new()
    {
        Hit = false,
        PerfectParried = true,
        DefenseUsed = DefenseType.PerfectParry,
        HitEffect = "perfect_parry"
    };
}
