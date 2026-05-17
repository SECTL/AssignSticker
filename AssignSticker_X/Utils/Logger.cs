using System;
using System.Collections.Generic;
using System.IO;

namespace AssignSticker_X.Utils;

public static class Logger
{
    private static readonly List<string> _buffer = new();
    private static readonly object _lock = new();

    public static void Info(string message) => Log("INFO", message, ConsoleColor.Green);
    public static void Warn(string message) => Log("WARN", message, ConsoleColor.DarkYellow);
    public static void Error(string message) => Log("ERROR", message, ConsoleColor.DarkRed);
    public static void Fatal(string message) => Log("FATAL", message, ConsoleColor.DarkRed);

    private static void Log(string level, string message, ConsoleColor color)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"{time} | {level} | {message}";

        lock (_lock)
        {
            _buffer.Add(line);
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"{time} | ");
        Console.ForegroundColor = color;
        Console.Write(level);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($" | {message}");
        Console.ResetColor();
    }

    public static void FlushToFile()
    {
        if (AppData.LogsPath == null) return;

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var existing = Directory.GetFiles(AppData.LogsPath, $"{today}-*.txt");
        var count = existing.Length + 1;
        var fileName = $"{today}-{count}.txt";
        var filePath = Path.Combine(AppData.LogsPath, fileName);

        lock (_lock)
        {
            File.WriteAllLines(filePath, _buffer);
            _buffer.Clear();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"日志已保存: {filePath}");
        Console.ResetColor();
    }
}
