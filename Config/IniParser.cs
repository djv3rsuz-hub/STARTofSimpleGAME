using System.Globalization;
using System.IO;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame.Config;

/// <summary>
/// Simple INI file reader/writer.
/// Supports [Sections], key = value pairs, # and ; comments.
/// </summary>
public sealed class IniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new();
    private string _filePath = string.Empty;

    public IniParser() { }

    public IniParser(string filePath)
    {
        _filePath = filePath;
    }

    public void Load(string filePath)
    {
        _filePath = filePath;
        _sections.Clear();

        if (!File.Exists(filePath))
        {
            Logger.Log($"INI file not found: {filePath}", LogLevel.Warning);
            return;
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            string currentSection = string.Empty;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';')
                    continue;

                // Section header [SectionName]
                if (line[0] == '[' && line[^1] == ']')
                {
                    currentSection = line[1..^1].Trim();
                    if (!_sections.ContainsKey(currentSection))
                        _sections[currentSection] = new Dictionary<string, string>();
                    continue;
                }

                // Key = Value
                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0) continue;

                var key = line[..eqIndex].Trim();
                var value = line[(eqIndex + 1)..].Trim();

                // Strip inline comments (value # comment or value ; comment)
                var commentIdx = FindCommentStart(value);
                if (commentIdx >= 0)
                    value = value[..commentIdx].Trim();

                if (_sections.ContainsKey(currentSection))
                    _sections[currentSection][key] = value;
            }

            Logger.Log($"INI loaded: {filePath} ({_sections.Count} sections)", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load INI: {filePath}", ex);
        }
    }

    public void Save(string? filePath = null)
    {
        var path = filePath ?? _filePath;
        try
        {
            using var writer = new StreamWriter(path);

            foreach (var section in _sections)
            {
                writer.WriteLine($"[{section.Key}]");

                foreach (var kvp in section.Value)
                    writer.WriteLine($"{kvp.Key} = {kvp.Value}");

                writer.WriteLine();
            }

            Logger.Log($"INI saved: {path}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to save INI: {path}", ex);
        }
    }

    public string GetString(string section, string key, string defaultValue = "")
    {
        if (_sections.TryGetValue(section, out var entries) && entries.TryGetValue(key, out var value))
            return value;
        return defaultValue;
    }

    public int GetInt(string section, string key, int defaultValue = 0)
    {
        var raw = GetString(section, key);
        return int.TryParse(raw, out var result) ? result : defaultValue;
    }

    public float GetFloat(string section, string key, float defaultValue = 0f)
    {
        var raw = GetString(section, key);
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result : defaultValue;
    }

    public double GetDouble(string section, string key, double defaultValue = 0.0)
    {
        var raw = GetString(section, key);
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result : defaultValue;
    }

    public bool GetBool(string section, string key, bool defaultValue = false)
    {
        var raw = GetString(section, key).ToLowerInvariant();
        return raw switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    public void SetString(string section, string key, string value)
    {
        if (!_sections.ContainsKey(section))
            _sections[section] = new Dictionary<string, string>();
        _sections[section][key] = value;
    }

    public void SetInt(string section, string key, int value)
        => SetString(section, key, value.ToString());

    public void SetFloat(string section, string key, float value)
        => SetString(section, key, value.ToString(CultureInfo.InvariantCulture));

    public void SetBool(string section, string key, bool value)
        => SetString(section, key, value.ToString().ToLowerInvariant());

    public bool HasSection(string section) => _sections.ContainsKey(section);
    public bool HasKey(string section, string key)
        => _sections.TryGetValue(section, out var entries) && entries.ContainsKey(key);

    public IReadOnlyDictionary<string, string>? GetSection(string section)
        => _sections.TryGetValue(section, out var entries) ? entries : null;

    public IEnumerable<string> GetSectionNames() => _sections.Keys;

    public string GetFilePath() => _filePath;

    private static int FindCommentStart(string value)
    {
        bool inQuote = false;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '"') inQuote = !inQuote;
            if (!inQuote && (value[i] == '#' || value[i] == ';'))
                return i;
        }
        return -1;
    }
}
