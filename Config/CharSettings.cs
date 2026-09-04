using System.Windows.Media;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame.Config;

public class CharacterData
{
    public string Name { get; set; } = "Unknown";
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double Size { get; set; } = 60;
    public double MoveSpeed { get; set; } = 600;
    public double AccelerationSpeed { get; set; } = 6000;
    public double DecelerationSpeed { get; set; } = 3000;
    public bool Controllable { get; set; }
    public Brush Color { get; set; } = Brushes.White;
    public Brush BorderColor { get; set; } = Brushes.White;
    public double BorderThickness { get; set; } = 1;
    public bool ClampToScreen { get; set; } = true;
    public double DashDistance { get; set; } = 50;
    public double DashRotationSpeed { get; set; } = 2160;
    public double DashCooldown { get; set; } = 0.8;
    public double DashDuration { get; set; } = 0.12;

    public CharacterStats Stats { get; set; } = new();
    public CharacterActionToggles Actions { get; set; } = new();
    public AIProfile AI { get; set; } = new();
}

public static class CharSettings
{
    private static readonly Dictionary<string, CharacterData> _characters = new();
    private static IniParser? _ini;

    public static void Load(string filePath)
    {
        _characters.Clear();
        _ini = new IniParser();
        _ini.Load(filePath);

        foreach (var sectionName in _ini.GetSectionNames())
        {
            var stats = new CharacterStats
            {
                HP = _ini.GetFloat(sectionName, "HP", 1.00f),
                MaxHP = _ini.GetFloat(sectionName, "MaxHP", 1.00f),
                Mana = _ini.GetFloat(sectionName, "Mana", 1.00f),
                Stamina = _ini.GetFloat(sectionName, "Stamina", 1.00f),
                HRegen = _ini.GetFloat(sectionName, "HRegen", 0.00f),
                MRegen = _ini.GetFloat(sectionName, "MRegen", 0.00f),
                SRegen = _ini.GetFloat(sectionName, "SRegen", 0.00f),

                Attack = _ini.GetFloat(sectionName, "Attack", 0.00f),
                AttackSpeed = _ini.GetFloat(sectionName, "AttackSpeed", 1.00f),
                MainDamage = _ini.GetFloat(sectionName, "MainDamage", 0.00f),
                SkillDamage = _ini.GetFloat(sectionName, "SkillDamage", 0.00f),
                CriticalChance = _ini.GetFloat(sectionName, "CriticalChance", 0.00f),
                CriticalDamage = _ini.GetFloat(sectionName, "CriticalDamage", 1.50f),
                Penetration = _ini.GetFloat(sectionName, "Penetration", 0.00f),
                ArmorPen = _ini.GetFloat(sectionName, "ArmorPen", 0.00f),
                LifeSteal = _ini.GetFloat(sectionName, "LifeSteal", 0.00f),
                manaSteal = _ini.GetFloat(sectionName, "ManaSteal", 0.00f),
                ComboDamageBonus = _ini.GetFloat(sectionName, "ComboDamageBonus", 0.15f),

                Defence = _ini.GetFloat(sectionName, "Defence", 0.00f),
                BlockChance = _ini.GetFloat(sectionName, "BlockChance", 0.00f),
                BlockStrength = _ini.GetFloat(sectionName, "BlockStrength", 0.50f),
                ParryChance = _ini.GetFloat(sectionName, "ParryChance", 0.00f),
                ParryDamage = _ini.GetFloat(sectionName, "ParryDamage", 0.00f),
                PerfectParryBonus = _ini.GetFloat(sectionName, "PerfectParryBonus", 0.50f),
                CounterDamage = _ini.GetFloat(sectionName, "CounterDamage", 0.00f),
                DodgeChance = _ini.GetFloat(sectionName, "DodgeChance", 0.00f),
                PerfectDodgeBonus = _ini.GetFloat(sectionName, "PerfectDodgeBonus", 0.50f),
                DamageReduction = _ini.GetFloat(sectionName, "DamageReduction", 0.00f),
                KnockbackResist = _ini.GetFloat(sectionName, "KnockbackResist", 0.00f),

                ResistPhysical = _ini.GetFloat(sectionName, "ResistPhysical", 0.00f),
                ResistFire = _ini.GetFloat(sectionName, "ResistFire", 0.00f),
                ResistIce = _ini.GetFloat(sectionName, "ResistIce", 0.00f),
                ResistLightning = _ini.GetFloat(sectionName, "ResistLightning", 0.00f),
                ResistPoison = _ini.GetFloat(sectionName, "ResistPoison", 0.00f),
                ResistHoly = _ini.GetFloat(sectionName, "ResistHoly", 0.00f),
                ResistDark = _ini.GetFloat(sectionName, "ResistDark", 0.00f),
                ResistArcane = _ini.GetFloat(sectionName, "ResistArcane", 0.00f),
                ResistEarth = _ini.GetFloat(sectionName, "ResistEarth", 0.00f),
                ResistWind = _ini.GetFloat(sectionName, "ResistWind", 0.00f),
                ResistWater = _ini.GetFloat(sectionName, "ResistWater", 0.00f),
                ResistVoid = _ini.GetFloat(sectionName, "ResistVoid", 0.00f),

                Resistance = _ini.GetFloat(sectionName, "Resistance", 0.00f),
                StunChance = _ini.GetFloat(sectionName, "StunChance", 0.00f),
                StunDuration = _ini.GetFloat(sectionName, "StunDuration", 1.00f),
                SlowResist = _ini.GetFloat(sectionName, "SlowResist", 0.00f),
                FreezeResist = _ini.GetFloat(sectionName, "FreezeResist", 0.00f),
                BurnResist = _ini.GetFloat(sectionName, "BurnResist", 0.00f),
                PoisonResist = _ini.GetFloat(sectionName, "PoisonResist", 0.00f),
                BleedResist = _ini.GetFloat(sectionName, "BleedResist", 0.00f),

                MoveSpeed = _ini.GetFloat(sectionName, "MoveSpeedMult", 1.00f),
                DashPower = _ini.GetFloat(sectionName, "DashPower", 1.00f),
                CooldownReduction = _ini.GetFloat(sectionName, "CooldownReduction", 0.00f),

                GoldFind = _ini.GetFloat(sectionName, "GoldFind", 0.00f),
                MagicFind = _ini.GetFloat(sectionName, "MagicFind", 0.00f),
                ExperienceBonus = _ini.GetFloat(sectionName, "ExperienceBonus", 0.00f)
            };

            var actions = new CharacterActionToggles
            {
                DashEnabled = _ini.GetBool(sectionName, "DashEnabled", true),
                AttackEnabled = _ini.GetBool(sectionName, "AttackEnabled", true),
                BlockEnabled = _ini.GetBool(sectionName, "BlockEnabled", true),
                ParryEnabled = _ini.GetBool(sectionName, "ParryEnabled", true),
                DodgeEnabled = _ini.GetBool(sectionName, "DodgeEnabled", true),
                CounterEnabled = _ini.GetBool(sectionName, "CounterEnabled", true),
                SpecialAttackEnabled = _ini.GetBool(sectionName, "SpecialAttackEnabled", true),
                HealEnabled = _ini.GetBool(sectionName, "HealEnabled", false),
                BuffEnabled = _ini.GetBool(sectionName, "BuffEnabled", false),
                DebuffEnabled = _ini.GetBool(sectionName, "DebuffEnabled", false)
            };

            var ai = new AIProfile
            {
                Style = Enum.TryParse<AICombatStyle>(_ini.GetString(sectionName, "AIStyle", "Aggressive"), true, out var s) ? s : AICombatStyle.Aggressive,
                AggressionLevel = _ini.GetFloat(sectionName, "AIAggression", 0.7f),
                RetreatThreshold = _ini.GetFloat(sectionName, "AIRetreatThreshold", 0.3f),
                ParryReactionTime = _ini.GetFloat(sectionName, "AIParryReaction", 0.15f),
                DodgeReactionTime = _ini.GetFloat(sectionName, "AIDodgeReaction", 0.12f),
                ComboAggression = _ini.GetFloat(sectionName, "AIComboAggression", 0.8f),
                DefensivePosture = _ini.GetFloat(sectionName, "AIDefensivePosture", 0.3f),
                SpecialAttackChance = _ini.GetFloat(sectionName, "AISpecialChance", 0.2f),
                HealThreshold = _ini.GetFloat(sectionName, "AIHealThreshold", 0.4f),
                WillParry = _ini.GetBool(sectionName, "AIWillParry", true),
                WillDodge = _ini.GetBool(sectionName, "AIWillDodge", true),
                WillBlock = _ini.GetBool(sectionName, "AIWillBlock", true),
                WillCounter = _ini.GetBool(sectionName, "AIWillCounter", true)
            };

            var data = new CharacterData
            {
                Name = _ini.GetString(sectionName, "Name", sectionName),
                StartX = _ini.GetDouble(sectionName, "StartX", 0),
                StartY = _ini.GetDouble(sectionName, "StartY", 0),
                Size = _ini.GetDouble(sectionName, "Size", 60),
                MoveSpeed = _ini.GetFloat(sectionName, "MoveSpeed", 600),
                AccelerationSpeed = _ini.GetDouble(sectionName, "AccelerationSpeed", 6000),
                DecelerationSpeed = _ini.GetDouble(sectionName, "DecelerationSpeed", 3000),
                Controllable = _ini.GetBool(sectionName, "Controllable", false),
                Color = ParseColor(_ini.GetString(sectionName, "Color", "White")),
                BorderColor = ParseColor(_ini.GetString(sectionName, "BorderColor", "White")),
                BorderThickness = _ini.GetDouble(sectionName, "BorderThickness", 1),
                ClampToScreen = _ini.GetBool(sectionName, "ClampToScreen", true),
                DashDistance = _ini.GetDouble(sectionName, "DashDistance", 50),
                DashRotationSpeed = _ini.GetDouble(sectionName, "DashRotationSpeed", 2160),
                DashCooldown = _ini.GetDouble(sectionName, "DashCooldown", 0.8),
                DashDuration = _ini.GetDouble(sectionName, "DashDuration", 0.12),
                Stats = stats,
                Actions = actions,
                AI = ai
            };

            _characters[sectionName] = data;
            Logger.Log($"Loaded character: {sectionName} ({data.Name}) at ({data.StartX},{data.StartY}) HP={stats.HP:F2} ATK={stats.Attack:F2}", LogLevel.Debug);
        }

        Logger.Log($"CharSettings loaded: {_characters.Count} characters from {filePath}", LogLevel.Info);
    }

    public static CharacterData? GetCharacter(string sectionName)
    {
        _characters.TryGetValue(sectionName, out var data);
        return data;
    }

    public static IReadOnlyDictionary<string, CharacterData> GetAllCharacters() => _characters;
    public static int Count => _characters.Count;

    public static Brush ParseColor(string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return Brushes.White;

        // Try named color first
        var prop = typeof(Colors).GetProperty(colorName);
        if (prop != null)
        {
            var color = (Color)prop.GetValue(null)!;
            return new SolidColorBrush(color);
        }

        // Try hex format #AARRGGBB or #RRGGBB
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            return new SolidColorBrush(color);
        }
        catch
        {
            Logger.Log($"Unknown color: {colorName}, falling back to White", LogLevel.Warning);
            return Brushes.White;
        }
    }
}
