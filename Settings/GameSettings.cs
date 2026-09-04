using System.IO;
using System.Text;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame.Settings;

public enum GraphicsQuality { Low, Medium, High }

public sealed class GameSettings
{
    private static readonly Lazy<GameSettings> _instance = new(() => new GameSettings());
    public static GameSettings Instance => _instance.Value;

    private static readonly string SettingsFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "gamesettings.cfg");

    // --- Display ---
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public int GameScreenWidth { get; set; } = 1870;
    public int GameScreenHeight { get; set; } = 1030;
    public int GameScreenBorderInset { get; set; } = 25;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
    public int TargetFps { get; set; } = 60;

    // --- Graphics ---
    public GraphicsQuality GraphicsQuality { get; set; } = GraphicsQuality.High;
    public bool Shadows { get; set; } = true;
    public bool ShowGameBorder { get; set; } = true;

    // --- Audio ---
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public float VfxVolume { get; set; } = 1.0f;

    // --- Gameplay ---
    public float DefaultMoveSpeed { get; set; } = 600f;
    public float DefaultCubeSize { get; set; } = 60f;
    public float StickDeadzone { get; set; } = 0.15f;
    public bool ShowFps { get; set; } = true;
    public bool ShowDebugInfo { get; set; } = true;
    public bool ShowLogPanel { get; set; } = true;
    public bool ShowCollision { get; set; } = false;

    // --- Input ---
    public float ControllerSensitivity { get; set; } = 1.0f;
    public bool InvertMouseY { get; set; } = false;

    // --- Colors (stored as hex strings for easy editing) ---
    public string BackgroundColor { get; set; } = "#FF000000";
    public string PlayerColor { get; set; } = "#FF1E90FF";
    public string EnemyColor { get; set; } = "#FFFF0000";
    public string UiAccentColor { get; set; } = "#FF00D4FF";

    // --- Runtime ---
    public bool IsPaused { get; set; }
    public bool OptionsMenuOpen { get; set; }

    private GameSettings() { }

    public void Load()
    {
        if (!File.Exists(SettingsFile))
        {
            Logger.Log("No gamesettings.cfg found, writing defaults", LogLevel.Info);
            Save();
            return;
        }

        try
        {
            var lines = File.ReadAllLines(SettingsFile);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith('['))
                    continue;

                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex < 0) continue;

                var key = trimmed[..eqIndex].Trim();
                var value = trimmed[(eqIndex + 1)..].Trim();

                ApplySetting(key, value);
            }

            Logger.Log("GameSettings loaded from gamesettings.cfg", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to load gamesettings.cfg", ex);
        }
    }

    public void Save()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ============================================");
            sb.AppendLine("# SimpleWPFGame - Game Settings");
            sb.AppendLine("# Edit values below. Restart to apply display changes.");
            sb.AppendLine("# Audio/Graphics changes apply instantly.");
            sb.AppendLine("# ============================================");
            sb.AppendLine();

            sb.AppendLine("[Display]");
            sb.AppendLine($"WindowWidth = {WindowWidth}");
            sb.AppendLine($"WindowHeight = {WindowHeight}");
            sb.AppendLine($"GameScreenWidth = {GameScreenWidth}");
            sb.AppendLine($"GameScreenHeight = {GameScreenHeight}");
            sb.AppendLine($"GameScreenBorderInset = {GameScreenBorderInset}");
            sb.AppendLine($"Fullscreen = {Fullscreen}");
            sb.AppendLine($"VSync = {VSync}");
            sb.AppendLine($"TargetFps = {TargetFps}");
            sb.AppendLine();

            sb.AppendLine("[Graphics]");
            sb.AppendLine($"# Low, Medium, High");
            sb.AppendLine($"GraphicsQuality = {GraphicsQuality}");
            sb.AppendLine($"Shadows = {Shadows}");
            sb.AppendLine($"ShowGameBorder = {ShowGameBorder}");
            sb.AppendLine();

            sb.AppendLine("[Audio]");
            sb.AppendLine($"# Volume 0.0 to 1.0");
            sb.AppendLine($"MasterVolume = {MasterVolume}");
            sb.AppendLine($"MusicVolume = {MusicVolume}");
            sb.AppendLine($"SfxVolume = {SfxVolume}");
            sb.AppendLine($"VfxVolume = {VfxVolume}");
            sb.AppendLine();

            sb.AppendLine("[Gameplay]");
            sb.AppendLine($"DefaultMoveSpeed = {DefaultMoveSpeed}");
            sb.AppendLine($"DefaultCubeSize = {DefaultCubeSize}");
            sb.AppendLine($"StickDeadzone = {StickDeadzone}");
            sb.AppendLine($"ShowFps = {ShowFps}");
            sb.AppendLine($"ShowDebugInfo = {ShowDebugInfo}");
            sb.AppendLine($"ShowLogPanel = {ShowLogPanel}");
            sb.AppendLine($"ShowCollision = {ShowCollision}");
            sb.AppendLine();

            sb.AppendLine("[Input]");
            sb.AppendLine($"ControllerSensitivity = {ControllerSensitivity}");
            sb.AppendLine($"InvertMouseY = {InvertMouseY}");
            sb.AppendLine();

            sb.AppendLine("[Colors]");
            sb.AppendLine($"# Colors in ARGB hex format (e.g., #FF1E90FF)");
            sb.AppendLine($"BackgroundColor = {BackgroundColor}");
            sb.AppendLine($"PlayerColor = {PlayerColor}");
            sb.AppendLine($"EnemyColor = {EnemyColor}");
            sb.AppendLine($"UiAccentColor = {UiAccentColor}");

            File.WriteAllText(SettingsFile, sb.ToString());
            Logger.Log("GameSettings saved to gamesettings.cfg", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to save gamesettings.cfg", ex);
        }
    }

    private void ApplySetting(string key, string value)
    {
        switch (key)
        {
            case nameof(WindowWidth): WindowWidth = ParseInt(value, WindowWidth); break;
            case nameof(WindowHeight): WindowHeight = ParseInt(value, WindowHeight); break;
            case nameof(GameScreenWidth): GameScreenWidth = ParseInt(value, GameScreenWidth); break;
            case nameof(GameScreenHeight): GameScreenHeight = ParseInt(value, GameScreenHeight); break;
            case nameof(GameScreenBorderInset): GameScreenBorderInset = ParseInt(value, GameScreenBorderInset); break;
            case nameof(Fullscreen): Fullscreen = ParseBool(value, Fullscreen); break;
            case nameof(VSync): VSync = ParseBool(value, VSync); break;
            case nameof(TargetFps): TargetFps = ParseInt(value, TargetFps); break;
            case nameof(GraphicsQuality): GraphicsQuality = ParseEnum(value, GraphicsQuality); break;
            case nameof(Shadows): Shadows = ParseBool(value, Shadows); break;
            case nameof(ShowGameBorder): ShowGameBorder = ParseBool(value, ShowGameBorder); break;
            case nameof(MasterVolume): MasterVolume = ParseFloat(value, MasterVolume); break;
            case nameof(MusicVolume): MusicVolume = ParseFloat(value, MusicVolume); break;
            case nameof(SfxVolume): SfxVolume = ParseFloat(value, SfxVolume); break;
            case nameof(VfxVolume): VfxVolume = ParseFloat(value, VfxVolume); break;
            case nameof(DefaultMoveSpeed): DefaultMoveSpeed = ParseFloat(value, DefaultMoveSpeed); break;
            case nameof(DefaultCubeSize): DefaultCubeSize = ParseFloat(value, DefaultCubeSize); break;
            case nameof(StickDeadzone): StickDeadzone = ParseFloat(value, StickDeadzone); break;
            case nameof(ShowFps): ShowFps = ParseBool(value, ShowFps); break;
            case nameof(ShowDebugInfo): ShowDebugInfo = ParseBool(value, ShowDebugInfo); break;
            case nameof(ShowLogPanel): ShowLogPanel = ParseBool(value, ShowLogPanel); break;
            case nameof(ShowCollision): ShowCollision = ParseBool(value, ShowCollision); break;
            case nameof(ControllerSensitivity): ControllerSensitivity = ParseFloat(value, ControllerSensitivity); break;
            case nameof(InvertMouseY): InvertMouseY = ParseBool(value, InvertMouseY); break;
            case nameof(BackgroundColor): BackgroundColor = value; break;
            case nameof(PlayerColor): PlayerColor = value; break;
            case nameof(EnemyColor): EnemyColor = value; break;
            case nameof(UiAccentColor): UiAccentColor = value; break;
            default:
                Logger.Log($"Unknown setting: {key} = {value}", LogLevel.Warning);
                break;
        }
    }

    private static int ParseInt(string value, int fallback)
        => int.TryParse(value, out var result) ? result : fallback;

    private static float ParseFloat(string value, float fallback)
        => float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : fallback;

    private static bool ParseBool(string value, bool fallback)
        => bool.TryParse(value, out var result) ? result : fallback;

    private static T ParseEnum<T>(string value, T fallback) where T : struct
        => Enum.TryParse<T>(value, true, out var result) ? result : fallback;
}
