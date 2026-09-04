namespace SimpleWPFGame.Config;

public class CharacterStats
{
    // --- Core Vitals ---
    public float HP { get; set; } = 1.00f;
    public float MaxHP { get; set; } = 1.00f;
    public float Mana { get; set; } = 1.00f;
    public float Stamina { get; set; } = 1.00f;
    public float HRegen { get; set; } = 0.00f;
    public float MRegen { get; set; } = 0.00f;
    public float SRegen { get; set; } = 0.00f;

    // --- Offense ---
    public float Attack { get; set; } = 0.00f;
    public float AttackSpeed { get; set; } = 1.00f;
    public float MainDamage { get; set; } = 0.00f;
    public float SkillDamage { get; set; } = 0.00f;
    public float CriticalChance { get; set; } = 0.00f;
    public float CriticalDamage { get; set; } = 1.50f;
    public float Penetration { get; set; } = 0.00f;
    public float ArmorPen { get; set; } = 0.00f;
    public float LifeSteal { get; set; } = 0.00f;
    public float manaSteal { get; set; } = 0.00f;
    public float ComboDamageBonus { get; set; } = 0.15f;

    // --- Defense ---
    public float Defence { get; set; } = 0.00f;
    public float BlockChance { get; set; } = 0.00f;
    public float BlockStrength { get; set; } = 0.50f;
    public float ParryChance { get; set; } = 0.00f;
    public float ParryDamage { get; set; } = 0.00f;
    public float PerfectParryBonus { get; set; } = 0.50f;
    public float CounterDamage { get; set; } = 0.00f;
    public float DodgeChance { get; set; } = 0.00f;
    public float PerfectDodgeBonus { get; set; } = 0.50f;
    public float DamageReduction { get; set; } = 0.00f;
    public float KnockbackResist { get; set; } = 0.00f;

    // --- 12 Resistances ---
    public float ResistPhysical { get; set; } = 0.00f;
    public float ResistFire { get; set; } = 0.00f;
    public float ResistIce { get; set; } = 0.00f;
    public float ResistLightning { get; set; } = 0.00f;
    public float ResistPoison { get; set; } = 0.00f;
    public float ResistHoly { get; set; } = 0.00f;
    public float ResistDark { get; set; } = 0.00f;
    public float ResistArcane { get; set; } = 0.00f;
    public float ResistEarth { get; set; } = 0.00f;
    public float ResistWind { get; set; } = 0.00f;
    public float ResistWater { get; set; } = 0.00f;
    public float ResistVoid { get; set; } = 0.00f;

    // --- Status ---
    public float Resistance { get; set; } = 0.00f;
    public float StunChance { get; set; } = 0.00f;
    public float StunDuration { get; set; } = 1.00f;
    public float SlowResist { get; set; } = 0.00f;
    public float FreezeResist { get; set; } = 0.00f;
    public float BurnResist { get; set; } = 0.00f;
    public float PoisonResist { get; set; } = 0.00f;
    public float BleedResist { get; set; } = 0.00f;

    // --- Movement ---
    public float MoveSpeed { get; set; } = 1.00f;
    public float DashPower { get; set; } = 1.00f;
    public float CooldownReduction { get; set; } = 0.00f;

    // --- Utility ---
    public float GoldFind { get; set; } = 0.00f;
    public float MagicFind { get; set; } = 0.00f;
    public float ExperienceBonus { get; set; } = 0.00f;

    public float GetResistance(DamageType type)
    {
        return type switch
        {
            DamageType.Physical => ResistPhysical,
            DamageType.Fire => ResistFire,
            DamageType.Ice => ResistIce,
            DamageType.Lightning => ResistLightning,
            DamageType.Poison => ResistPoison,
            DamageType.Holy => ResistHoly,
            DamageType.Dark => ResistDark,
            DamageType.Arcane => ResistArcane,
            DamageType.Earth => ResistEarth,
            DamageType.Wind => ResistWind,
            DamageType.Water => ResistWater,
            DamageType.Void => ResistVoid,
            _ => 0
        };
    }
}

public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison,
    Holy,
    Dark,
    Arcane,
    Earth,
    Wind,
    Water,
    Void,
    True
}

public class CharacterActionToggles
{
    public bool DashEnabled { get; set; } = true;
    public bool AttackEnabled { get; set; } = true;
    public bool BlockEnabled { get; set; } = true;
    public bool ParryEnabled { get; set; } = true;
    public bool DodgeEnabled { get; set; } = true;
    public bool CounterEnabled { get; set; } = true;
    public bool SpecialAttackEnabled { get; set; } = true;
    public bool HealEnabled { get; set; } = true;
    public bool BuffEnabled { get; set; } = true;
    public bool DebuffEnabled { get; set; } = true;
}

public enum AICombatStyle
{
    Aggressive,
    Defensive,
    Balanced,
    Berserker,
    Tank,
    Assassin,
    Mage,
    Support,
    BerserkerFrenzy,
    ParryMaster,
    DodgeMaster,
    CounterMaster
}

public class AIProfile
{
    public AICombatStyle Style { get; set; } = AICombatStyle.Aggressive;
    public float AggressionLevel { get; set; } = 0.7f;
    public float RetreatThreshold { get; set; } = 0.3f;
    public float ParryReactionTime { get; set; } = 0.15f;
    public float DodgeReactionTime { get; set; } = 0.12f;
    public float ComboAggression { get; set; } = 0.8f;
    public float DefensivePosture { get; set; } = 0.3f;
    public float SpecialAttackChance { get; set; } = 0.2f;
    public float HealThreshold { get; set; } = 0.4f;
    public float TargetSwitchChance { get; set; } = 0.1f;
    public float StaggerWindow { get; set; } = 0.3f;
    public bool WillParry { get; set; } = true;
    public bool WillDodge { get; set; } = true;
    public bool WillBlock { get; set; } = true;
    public bool WillCounter { get; set; } = true;
    public bool WillHeal { get; set; } = false;
    public bool WillBuff { get; set; } = false;
    public bool WillDebuff { get; set; } = false;

    public static AIProfile Aggressive => new()
    {
        Style = AICombatStyle.Aggressive,
        AggressionLevel = 0.9f,
        RetreatThreshold = 0.15f,
        ParryReactionTime = 0.2f,
        DodgeReactionTime = 0.18f,
        ComboAggression = 0.9f,
        DefensivePosture = 0.1f,
        SpecialAttackChance = 0.3f,
        WillParry = false,
        WillDodge = true,
        WillBlock = false,
        WillCounter = false
    };

    public static AIProfile Defensive => new()
    {
        Style = AICombatStyle.Defensive,
        AggressionLevel = 0.3f,
        RetreatThreshold = 0.5f,
        ParryReactionTime = 0.1f,
        DodgeReactionTime = 0.08f,
        ComboAggression = 0.4f,
        DefensivePosture = 0.8f,
        SpecialAttackChance = 0.15f,
        WillParry = true,
        WillDodge = true,
        WillBlock = true,
        WillCounter = true
    };

    public static AIProfile Berserker => new()
    {
        Style = AICombatStyle.Berserker,
        AggressionLevel = 1.0f,
        RetreatThreshold = 0.05f,
        ParryReactionTime = 0.25f,
        DodgeReactionTime = 0.2f,
        ComboAggression = 1.0f,
        DefensivePosture = 0.0f,
        SpecialAttackChance = 0.4f,
        WillParry = false,
        WillDodge = false,
        WillBlock = false,
        WillCounter = false
    };

    public static AIProfile Tank => new()
    {
        Style = AICombatStyle.Tank,
        AggressionLevel = 0.4f,
        RetreatThreshold = 0.2f,
        ParryReactionTime = 0.12f,
        DodgeReactionTime = 0.15f,
        ComboAggression = 0.5f,
        DefensivePosture = 0.9f,
        SpecialAttackChance = 0.1f,
        WillParry = true,
        WillDodge = false,
        WillBlock = true,
        WillCounter = true,
        WillHeal = true,
        HealThreshold = 0.5f
    };

    public static AIProfile Assassin => new()
    {
        Style = AICombatStyle.Assassin,
        AggressionLevel = 0.8f,
        RetreatThreshold = 0.25f,
        ParryReactionTime = 0.08f,
        DodgeReactionTime = 0.06f,
        ComboAggression = 0.95f,
        DefensivePosture = 0.2f,
        SpecialAttackChance = 0.35f,
        WillParry = false,
        WillDodge = true,
        WillBlock = false,
        WillCounter = true
    };

    public static AIProfile ParryMaster => new()
    {
        Style = AICombatStyle.ParryMaster,
        AggressionLevel = 0.5f,
        RetreatThreshold = 0.3f,
        ParryReactionTime = 0.04f,
        DodgeReactionTime = 0.1f,
        ComboAggression = 0.6f,
        DefensivePosture = 0.7f,
        SpecialAttackChance = 0.25f,
        WillParry = true,
        WillDodge = true,
        WillBlock = true,
        WillCounter = true
    };

    public static AIProfile DodgeMaster => new()
    {
        Style = AICombatStyle.DodgeMaster,
        AggressionLevel = 0.6f,
        RetreatThreshold = 0.35f,
        ParryReactionTime = 0.12f,
        DodgeReactionTime = 0.03f,
        ComboAggression = 0.7f,
        DefensivePosture = 0.5f,
        SpecialAttackChance = 0.2f,
        WillParry = true,
        WillDodge = true,
        WillBlock = false,
        WillCounter = true
    };
}
