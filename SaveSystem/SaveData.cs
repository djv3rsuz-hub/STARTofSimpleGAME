namespace SimpleWPFGame.SaveSystem;

public class SaveData
{
    public string Version { get; set; } = "1.0";
    public string SaveTimestamp { get; set; } = "";
    public double GameTime { get; set; }
    public bool IsPaused { get; set; }
    public int SlotIndex { get; set; } = 0;
    public string SlotName { get; set; } = "Quick Save";
    public CharacterSaveData Player { get; set; } = new();
    public CharacterSaveData Enemy { get; set; } = new();
    public CharacterSaveData GreenCube { get; set; } = new();
    public SettingsSnapshot Settings { get; set; } = new();
}

public class CharacterSaveData
{
    public string SectionName { get; set; } = "";
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double VelX { get; set; }
    public double VelY { get; set; }
    public double Rotation { get; set; }
    public bool IsActive { get; set; } = true;

    // Movement
    public double MoveSpeed { get; set; }
    public double AccelerationSpeed { get; set; }
    public double DecelerationSpeed { get; set; }
    public bool ClampToScreen { get; set; } = true;

    // Dash state
    public double DashDistance { get; set; }
    public double DashRotationSpeed { get; set; }
    public double DashCooldown { get; set; }
    public double DashDuration { get; set; }
    public bool IsDashing { get; set; }
    public double DashCooldownRemaining { get; set; }

    // Stats
    public StatsSaveData Stats { get; set; } = new();

    // AI Actions
    public bool DashEnabled { get; set; } = true;
}

public class StatsSaveData
{
    public float HP { get; set; } = 1.00f;
    public float Defence { get; set; }
    public float Attack { get; set; }
    public float AttackSpeed { get; set; } = 1.00f;
    public float Resistance { get; set; }
    public float DodgeChance { get; set; }
    public float CriticalChance { get; set; }
    public float CriticalDamage { get; set; } = 1.50f;
    public float MainDamage { get; set; }
    public float SkillDamage { get; set; }
    public float ParryDamage { get; set; }
    public float CounterDamage { get; set; }
}

public class SettingsSnapshot
{
    // Display
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public int GameScreenWidth { get; set; } = 1870;
    public int GameScreenHeight { get; set; } = 1030;
    public bool VSync { get; set; } = true;
    public int TargetFps { get; set; } = 60;

    // Graphics
    public string GraphicsQuality { get; set; } = "High";
    public bool Shadows { get; set; } = true;
    public bool ShowGameBorder { get; set; } = true;

    // Audio
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public float VfxVolume { get; set; } = 1.0f;

    // Gameplay
    public bool ShowFps { get; set; } = true;
    public bool ShowCollision { get; set; } = false;
    public bool ShowDebugInfo { get; set; } = true;
    public float StickDeadzone { get; set; } = 0.15f;
    public float ControllerSensitivity { get; set; } = 1.0f;
}
