using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.settingswindow.view.management;

public partial class subjectmanag_interface : UserControl
{
    public subjectmanag_interface()
    {
        InitializeComponent();
        LoadSubjects();
        AddSubjectBtn.Click += (_, _) => AddSubject();
    }

    private void LoadSubjects()
    {
        SubjectList.Children.Clear();
        var subjects = GetSubjects();
        foreach (var subj in subjects)
        {
            var expander = new FASettingsExpander
            {
                Header = subj,
                Description = "",
                IsClickEnabled = false
            };
            expander.IconSource = new FASymbolIconSource { Symbol = FASymbol.Find, FontSize = 16 };

            var deleteBtn = new Button
            {
                Content = "删除",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                Foreground = new SolidColorBrush(Color.Parse("#D32F2F"))
            };
            var captured = subj;
            deleteBtn.Click += (_, _) => DeleteSubject(captured);
            expander.Footer = deleteBtn;

            SubjectList.Children.Add(expander);
        }
    }

    private static List<string> GetSubjects()
    {
        var saved = ConfigManager.Get<string>("subjects");
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(saved);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        return new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "历史", "地理", "政治" };
    }

    private static void SaveSubjects(List<string> subjects)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(subjects);
        ConfigManager.Set("subjects", json);
        ConfigManager.Save();
    }

    private void AddSubject()
    {
        var name = NewSubjectBox.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var subjects = GetSubjects();
        if (subjects.Contains(name)) return;

        subjects.Add(name);
        SaveSubjects(subjects);
        NewSubjectBox.Text = "";
        LoadSubjects();
    }

    private void DeleteSubject(string name)
    {
        var subjects = GetSubjects();
        subjects.Remove(name);
        SaveSubjects(subjects);
        LoadSubjects();
    }
}