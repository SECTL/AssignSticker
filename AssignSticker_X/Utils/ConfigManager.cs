using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssignSticker_X.Utils;

public static class ConfigManager
{
    private static JsonObject? _config;

    public static void Load()
    {
        if (!File.Exists(AppData.ConfigPath))
        {
            _config = new JsonObject();
            Save();
            return;
        }

        var json = File.ReadAllText(AppData.ConfigPath);
        _config = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        Logger.Info("配置文件已加载");
    }

    public static void Save()
    {
        if (_config == null) return;
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppData.ConfigPath, json);
    }

    public static T? Get<T>(string key, T? defaultValue = default)
    {
        if (_config == null) return defaultValue;
        if (_config.TryGetPropertyValue(key, out var node) && node != null)
            return node.GetValue<T>();
        return defaultValue;
    }

    public static void Set<T>(string key, T value)
    {
        _config ??= new JsonObject();
        _config[key] = JsonValue.Create(value);
    }

    public static bool ContainsKey(string key)
    {
        return _config?.ContainsKey(key) == true;
    }
}
