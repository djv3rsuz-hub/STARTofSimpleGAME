namespace SimpleWPFGame.Config;

public class CharacterStats
{
    public float HP { get; set; } = 1.00f;
    public float Defence { get; set; } = 0.00f;
    public float Attack { get; set; } = 0.00f;
    public float AttackSpeed { get; set; } = 1.00f;
    public float Resistance { get; set; } = 0.00f;
    public float DodgeChance { get; set; } = 0.00f;
    public float CriticalChance { get; set; } = 0.00f;
    public float CriticalDamage { get; set; } = 1.50f;
    public float MainDamage { get; set; } = 0.00f;
    public float SkillDamage { get; set; } = 0.00f;
    public float ParryDamage { get; set; } = 0.00f;
    public float CounterDamage { get; set; } = 0.00f;
}

public class CharacterActionToggles
{
    public bool DashEnabled { get; set; } = true;
}
