namespace SimpleWPFGame.UI;

public static class TooltipDescriptions
{
    // --- Options Menu ---
    public const string GraphicsQuality = "Controls visual fidelity.\n\nLow: Minimal effects, best performance.\nMed: Balanced quality and performance.\nHigh: Full effects, requires stronger GPU.";
    public const string VSync = "Synchronizes frame rate to your monitor's\nrefresh rate to prevent screen tearing.\n\nON: Smoother, may add input lag.\nOFF: Lower latency, possible tearing.";
    public const string Shadows = "Toggles dynamic shadow rendering.\n\nON: Objects cast shadows (uses GPU).\nOFF: No shadows, better performance.";
    public const string MasterVolume = "Controls overall audio output volume.\n\n0%: Muted\n100%: Full volume";
    public const string MusicVolume = "Controls background music volume.\n\n0%: Music off\n100%: Full music volume";
    public const string SfxVolume = "Controls sound effects volume.\n\n0%: SFX off\n100%: Full SFX volume";
    public const string VfxVolume = "Controls visual effects volume/intensity.\n\n0%: Minimal VFX\n100%: Full VFX";
    public const string WindowSize = "Changes the game window resolution.\n\n720p:  1280 x 720\n900p:  1600 x 900\n1080p: 1920 x 1080\n\nGame screen adjusts automatically.";
    public const string SaveClose = "Save all current settings to\ngamesettings.cfg and close menu.";
    public const string CloseMenu = "Close options menu without saving.\nChanges since last save will be lost.";

    // --- Stats ---
    public const string HP = "Health Points - Current health ratio.\n\n1.00 = 100% (full health)\n0.00 = Dead\n\nHigher max HP means more damage\nneeded to defeat this character.";
    public const string Defence = "Damage reduction from incoming attacks.\n\n0.00 = No reduction\n1.00 = Immune to all damage\n\nExample: 0.15 = 15% damage blocked.";
    public const string Attack = "Base attack power dealt to enemies.\n\n0.00 = No attack\nHigher = more damage per hit\n\nThis is the base value before\ncrit/skill modifiers apply.";
    public const string AttackSpeed = "Attack speed multiplier.\n\n1.00 = Normal speed\n2.00 = Double speed (attacks 2x faster)\n0.50 = Half speed\n\nHigher = more attacks per second.";
    public const string Resistance = "Resistance to status effects.\n\n0.00 = No resistance\n1.00 = Immune to all effects\n\nAffects stun, slow, poison, etc.";
    public const string DodgeChance = "Chance to completely dodge an attack.\n\n0.00 = Never dodges\n1.00 = Always dodges (invincible)\n\nExample: 0.10 = 10% dodge chance.";
    public const string CriticalChance = "Chance to land a critical hit.\n\n0.00 = Never criticals\n1.00 = Always criticals\n\nExample: 0.15 = 15% crit chance.";
    public const string CriticalDamage = "Critical hit damage multiplier.\n\n1.00 = 100% (no bonus)\n1.50 = 150% (50% extra damage)\n2.00 = 200% (double damage)\n\nApplied on top of base Attack.";
    public const string MainDamage = "Main/physical damage output.\n\n0.00 = No physical damage\nHigher = stronger physical attacks\n\nUsed for basic melee/ranged attacks.";
    public const string SkillDamage = "Skill/magical damage output.\n\n0.00 = No skill damage\nHigher = stronger special abilities\n\nUsed for powered-up moves.";
    public const string ParryDamage = "Damage dealt when parrying an attack.\n\n0.00 = No parry damage\nHigher = more counter-damage on parry\n\nRequires timing a block correctly.";
    public const string CounterDamage = "Damage dealt when countering.\n\n0.00 = No counter damage\nHigher = stronger counter-attacks\n\nTriggered after a successful parry.";

    // --- AI Actions ---
    public const string DashPlayer = "Player dash action toggle.\n\nON: Player can dash (B button/Space)\nOFF: Dash disabled\n\nDash is a quick burst of movement\nwith invincibility frames.";
    public const string DashEnemy = "Enemy AI dash action toggle.\n\nON: Enemy can dash toward player\nOFF: Enemy cannot dash\n\nAffects enemy behavior in combat.";
    public const string DashNPC = "NPC ally dash action toggle.\n\nON: NPC can dash to reposition\nOFF: NPC cannot dash\n\nGreen cube is a friendly NPC.";

    // --- Controls ---
    public const string MoveControl = "W/A/S/D or Arrow Keys\n\nMove the player cube around\nthe game screen. Supports\ncontroller analog stick.";
    public const string DashControl = "Space (keyboard) or B (controller)\n\nPerforms a quick dash in the\ncurrent movement direction.\nHas cooldown between uses.";
    public const string CollisionDebug = "F3 key\n\nToggles collision box visualization.\nShows green dashed outlines and\ncorner ticks on all objects.";
    public const string OptionsKey = "ESC or O key\n\nOpens the options menu.\nStart button on controller.";

    // --- AI States ---
    public const string DashEnabled = "Whether this character can use dash.\n\nDefined in charsettings.ini\nunder [CharacterName] section.\n\nSet DashEnabled = false to disable.";
    public const string DashDisabled = "This character's dash is disabled.\n\nEnable in charsettings.ini:\nDashEnabled = true";
}
