using System.IO;

namespace AssignSticker_X.Utils;

public static class AppData
{
    public static string BasePath { get; private set; } = null!;
    public static string DataPath { get; private set; } = null!;
    public static string SavesPath { get; private set; } = null!;
    public static string LogsPath { get; private set; } = null!;
    public static string ConfigPath { get; private set; } = null!;

    public static void Initialize()
    {
        BasePath = Directory.GetCurrentDirectory();
        DataPath = Path.Combine(BasePath, "data");
        SavesPath = Path.Combine(DataPath, "saves");
        LogsPath = Path.Combine(DataPath, "logs");
        ConfigPath = Path.Combine(DataPath, "config.json");

        Directory.CreateDirectory(DataPath);
        Directory.CreateDirectory(SavesPath);
        Directory.CreateDirectory(LogsPath);

        if (!File.Exists(ConfigPath))
        {
            File.WriteAllText(ConfigPath, "{}");
            Logger.Info("已创建默认配置文件 config.json");
        }

        Logger.Info($"运行目录初始化完成: {DataPath}");
    }
}
