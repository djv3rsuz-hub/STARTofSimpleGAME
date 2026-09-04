using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleWPFGame.Game;
using SimpleWPFGame.Logging;
using SimpleWPFGame.Settings;

namespace SimpleWPFGame.SaveSystem;

public sealed class SaveManager
{
    private static readonly Lazy<SaveManager> _instance = new(() => new SaveManager());
    public static SaveManager Instance => _instance.Value;

    private static readonly string SaveDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Saves");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = false
    };

    private SaveManager()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }

    public string GetSavePath(int slot)
        => Path.Combine(SaveDirectory, $"save_slot_{slot}.json");

    public bool Save(int slot, Cube? player, Cube? enemy, Cube? green, string? name = null)
    {
        try
        {
            var data = CaptureGameState(slot, name ?? $"Slot {slot}", player, enemy, green);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var path = GetSavePath(slot);
            File.WriteAllText(path, json);

            Logger.Log($"Game saved to slot {slot}: {path}", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to save to slot {slot}", ex);
            return false;
        }
    }

    public SaveData? Load(int slot)
    {
        try
        {
            var path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Logger.Log($"No save file found in slot {slot}", LogLevel.Warning);
                return null;
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

            if (data == null)
            {
                Logger.Log($"Save file in slot {slot} is empty or invalid", LogLevel.Warning);
                return null;
            }

            Logger.Log($"Game loaded from slot {slot}: {path}", LogLevel.Info);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load from slot {slot}", ex);
            return null;
        }
    }

    public bool ApplyLoadedState(SaveData data, Cube? player, Cube? enemy, Cube? green)
    {
        try
        {
            if (player != null)
                ApplyCharacterState(player, data.Player);

            if (enemy != null)
                ApplyCharacterState(enemy, data.Enemy);

            if (green != null)
                ApplyCharacterState(green, data.GreenCube);

            ApplySettings(data.Settings);

            Logger.Log($"Game state restored from {data.SaveTimestamp}", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to apply loaded state", ex);
            return false;
        }
    }

    private void ApplyCharacterState(Cube cube, CharacterSaveData save)
    {
        cube.Position = new System.Windows.Vector(save.PosX, save.PosY);
        cube.Velocity = new System.Windows.Vector(save.VelX, save.VelY);
        cube.Rotation = save.Rotation;
        cube.IsActive = save.IsActive;
        cube.MoveSpeed = save.MoveSpeed;
        cube.AccelerationSpeed = save.AccelerationSpeed;
        cube.DecelerationSpeed = save.DecelerationSpeed;
        cube.ClampToScreen = save.ClampToScreen;
        cube.DashDistance = save.DashDistance;
        cube.DashRotationSpeed = save.DashRotationSpeed;
        cube.DashCooldown = save.DashCooldown;
        cube.DashDuration = save.DashDuration;
        cube.IsDashing = save.IsDashing;
        cube.DashCooldownRemaining = save.DashCooldownRemaining;
        cube.Stats.HP = save.Stats.HP;
        cube.Stats.Defence = save.Stats.Defence;
        cube.Stats.Attack = save.Stats.Attack;
        cube.Stats.AttackSpeed = save.Stats.AttackSpeed;
        cube.Stats.Resistance = save.Stats.Resistance;
        cube.Stats.DodgeChance = save.Stats.DodgeChance;
        cube.Stats.CriticalChance = save.Stats.CriticalChance;
        cube.Stats.CriticalDamage = save.Stats.CriticalDamage;
        cube.Stats.MainDamage = save.Stats.MainDamage;
        cube.Stats.SkillDamage = save.Stats.SkillDamage;
        cube.Stats.ParryDamage = save.Stats.ParryDamage;
        cube.Stats.CounterDamage = save.Stats.CounterDamage;
        cube.Actions.DashEnabled = save.DashEnabled;
    }

    private SaveData CaptureGameState(int slot, string name, Cube? player, Cube? enemy, Cube? green)
    {
        var engine = GameEngine.Instance;
        var settings = GameSettings.Instance;

        var data = new SaveData
        {
            Version = "1.0",
            SaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            GameTime = engine.ElapsedTime,
            IsPaused = settings.IsPaused,
            SlotIndex = slot,
            SlotName = name
        };

        if (player != null) data.Player = CaptureCharacter("PlayerCube", player);
        if (enemy != null) data.Enemy = CaptureCharacter("EnemyCube", enemy);
        if (green != null) data.GreenCube = CaptureCharacter("GreenCube", green);

        data.Settings = new SettingsSnapshot
        {
            WindowWidth = settings.WindowWidth,
            WindowHeight = settings.WindowHeight,
            GameScreenWidth = settings.GameScreenWidth,
            GameScreenHeight = settings.GameScreenHeight,
            VSync = settings.VSync,
            TargetFps = settings.TargetFps,
            GraphicsQuality = settings.GraphicsQuality.ToString(),
            Shadows = settings.Shadows,
            ShowGameBorder = settings.ShowGameBorder,
            MasterVolume = settings.MasterVolume,
            MusicVolume = settings.MusicVolume,
            SfxVolume = settings.SfxVolume,
            VfxVolume = settings.VfxVolume,
            ShowFps = settings.ShowFps,
            ShowCollision = settings.ShowCollision,
            ShowDebugInfo = settings.ShowDebugInfo,
            StickDeadzone = settings.StickDeadzone,
            ControllerSensitivity = settings.ControllerSensitivity
        };

        return data;
    }

    private CharacterSaveData CaptureCharacter(string section, Cube cube)
    {
        return new CharacterSaveData
        {
            SectionName = section,
            PosX = cube.Position.X,
            PosY = cube.Position.Y,
            VelX = cube.Velocity.X,
            VelY = cube.Velocity.Y,
            Rotation = cube.Rotation,
            IsActive = cube.IsActive,
            MoveSpeed = cube.MoveSpeed,
            AccelerationSpeed = cube.AccelerationSpeed,
            DecelerationSpeed = cube.DecelerationSpeed,
            ClampToScreen = cube.ClampToScreen,
            DashDistance = cube.DashDistance,
            DashRotationSpeed = cube.DashRotationSpeed,
            DashCooldown = cube.DashCooldown,
            DashDuration = cube.DashDuration,
            IsDashing = cube.IsDashing,
            DashCooldownRemaining = cube.DashCooldownRemaining,
            Stats = new StatsSaveData
            {
                HP = cube.Stats.HP,
                Defence = cube.Stats.Defence,
                Attack = cube.Stats.Attack,
                AttackSpeed = cube.Stats.AttackSpeed,
                Resistance = cube.Stats.Resistance,
                DodgeChance = cube.Stats.DodgeChance,
                CriticalChance = cube.Stats.CriticalChance,
                CriticalDamage = cube.Stats.CriticalDamage,
                MainDamage = cube.Stats.MainDamage,
                SkillDamage = cube.Stats.SkillDamage,
                ParryDamage = cube.Stats.ParryDamage,
                CounterDamage = cube.Stats.CounterDamage
            },
            DashEnabled = cube.Actions.DashEnabled
        };
    }

    private void ApplySettings(SettingsSnapshot snap)
    {
        var s = GameSettings.Instance;
        s.WindowWidth = snap.WindowWidth;
        s.WindowHeight = snap.WindowHeight;
        s.GameScreenWidth = snap.GameScreenWidth;
        s.GameScreenHeight = snap.GameScreenHeight;
        s.VSync = snap.VSync;
        s.TargetFps = snap.TargetFps;
        s.Shadows = snap.Shadows;
        s.ShowGameBorder = snap.ShowGameBorder;
        s.MasterVolume = snap.MasterVolume;
        s.MusicVolume = snap.MusicVolume;
        s.SfxVolume = snap.SfxVolume;
        s.VfxVolume = snap.VfxVolume;
        s.ShowFps = snap.ShowFps;
        s.ShowCollision = snap.ShowCollision;
        s.ShowDebugInfo = snap.ShowDebugInfo;
        s.StickDeadzone = snap.StickDeadzone;
        s.ControllerSensitivity = snap.ControllerSensitivity;

        if (Enum.TryParse<GraphicsQuality>(snap.GraphicsQuality, true, out var q))
            s.GraphicsQuality = q;
    }

    public bool DeleteSave(int slot)
    {
        try
        {
            var path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Logger.Log($"Save slot {slot} deleted", LogLevel.Info);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to delete slot {slot}", ex);
            return false;
        }
    }

    public bool SaveExists(int slot) => File.Exists(GetSavePath(slot));

    public List<SaveSlotInfo> GetAllSlots()
    {
        var slots = new List<SaveSlotInfo>();
        for (int i = 1; i <= 5; i++)
        {
            var info = new SaveSlotInfo { SlotIndex = i, Exists = SaveExists(i) };
            if (info.Exists)
            {
                try
                {
                    var data = Load(i);
                    if (data != null)
                    {
                        info.Timestamp = data.SaveTimestamp;
                        info.GameTime = data.GameTime;
                        info.SlotName = data.SlotName;
                    }
                }
                catch { }
            }
            slots.Add(info);
        }
        return slots;
    }
}

public class SaveSlotInfo
{
    public int SlotIndex { get; set; }
    public bool Exists { get; set; }
    public string Timestamp { get; set; } = "";
    public double GameTime { get; set; }
    public string SlotName { get; set; } = "";
}
