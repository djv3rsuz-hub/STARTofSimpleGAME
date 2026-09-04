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
                DashDuration = _ini.GetDouble(sectionName, "DashDuration", 0.12)
            };

            _characters[sectionName] = data;
            Logger.Log($"Loaded character: {sectionName} ({data.Name}) at ({data.StartX},{data.StartY})", LogLevel.Debug);
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
