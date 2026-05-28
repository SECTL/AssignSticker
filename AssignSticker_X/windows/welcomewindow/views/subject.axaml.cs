using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using AssignSticker_X.Utils;

namespace AssignSticker_X.windows.welcomewindow.views;

public partial class subject : UserControl
{
    public subject()
    {
        InitializeComponent();
        LoadSubjects();
        AddSubjectBtn.Click += (_, _) => AddSubject();
    }

    private readonly Dictionary<string, bool> _expandedState = new();

    private void LoadSubjects()
    {
        SubjectList.Children.Clear();
        var subjects = GetSubjects();
        var workbooks = GetWorkbooks();

        foreach (var subj in subjects)
        {
            var expander = new FASettingsExpander
            {
                Header = subj,
                IsExpanded = _expandedState.GetValueOrDefault(subj, false)
            };
            expander.PropertyChanged += (_, e) =>
            {
                if (e.Property == FASettingsExpander.IsExpandedProperty)
                    _expandedState[subj] = expander.IsExpanded;
            };
            expander.IconSource = new FASymbolIconSource { Symbol = FASymbol.Find, FontSize = 16 };

            var deleteBtn = new Button
            {
                Content = "删除学科",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                Foreground = new SolidColorBrush(Color.Parse("#D32F2F"))
            };
            var capturedSubj = subj;
            deleteBtn.Click += (_, _) => DeleteSubject(capturedSubj);
            expander.Footer = deleteBtn;

            if (workbooks.TryGetValue(subj, out var books))
            {
                foreach (var book in books)
                    expander.Items.Add(BuildWorkbookItem(subj, book));
            }
            expander.Items.Add(BuildAddWorkbookItem(subj));
            SubjectList.Children.Add(expander);
        }
    }

    private FASettingsExpanderItem BuildWorkbookItem(string subject, string workbook)
    {
        var renameBox = new TextBox
        {
            Text = workbook,
            FontSize = 13,
            Width = 160,
            IsVisible = false
        };
        var nameText = new TextBlock
        {
            Text = workbook,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        var renameBtn = new Button
        {
            Content = "重命名",
            FontSize = 11,
            Padding = new Thickness(6, 1)
        };
        var deleteBtn = new Button
        {
            Content = "删除",
            FontSize = 11,
            Padding = new Thickness(6, 1),
            Foreground = new SolidColorBrush(Color.Parse("#D32F2F"))
        };
        renameBtn.Click += (_, _) =>
        {
            if (renameBox.IsVisible)
            {
                var newName = renameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != workbook)
                    RenameWorkbook(subject, workbook, newName);
                renameBox.IsVisible = false;
                nameText.IsVisible = true;
                renameBtn.Content = "重命名";
            }
            else
            {
                renameBox.Text = nameText.Text;
                renameBox.IsVisible = true;
                nameText.IsVisible = false;
                renameBtn.Content = "确认";
            }
        };
        var capturedWorkbook = workbook;
        deleteBtn.Click += (_, _) => DeleteWorkbook(subject, capturedWorkbook);

        return new FASettingsExpanderItem
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { nameText, renameBox }
            },
            Footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { renameBtn, deleteBtn }
            }
        };
    }

    private FASettingsExpanderItem BuildAddWorkbookItem(string subject)
    {
        var addBox = new TextBox
        {
            PlaceholderText = "输入练习册名称",
            Width = 180,
            FontSize = 13
        };
        var addBtn = new Button
        {
            Content = "添加练习册",
            FontSize = 12,
            Padding = new Thickness(8, 2)
        };
        addBtn.Click += (_, _) =>
        {
            var name = addBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;
            AddWorkbook(subject, name);
            addBox.Text = "";
        };

        return new FASettingsExpanderItem
        {
            Footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { addBox, addBtn }
            }
        };
    }

    private static List<string> GetSubjects()
    {
        var saved = ConfigManager.Get<string>("subjects");
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(saved);
                if (parsed != null && parsed.Count > 0) return parsed;
            }
            catch { }
        }
        var defaults = new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "历史", "地理", "政治" };
        SaveSubjects(defaults);
        return defaults;
    }

    private static void SaveSubjects(List<string> subjects)
    {
        ConfigManager.Set("subjects", JsonSerializer.Serialize(subjects));
        ConfigManager.Save();
    }

    private static Dictionary<string, List<string>> GetWorkbooks()
    {
        var saved = ConfigManager.Get<string>("subject_workbooks");
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(saved);
                if (parsed != null) return parsed;
            }
            catch { }
        }
        var defaults = new Dictionary<string, List<string>>
        {
            ["语文"] = new() { "能力培养与测试" },
            ["数学"] = new() { "能力培养与测试" },
            ["英语"] = new() { "全品", "5年中考 3 年模拟", "阳光课堂" }
        };
        SaveWorkbooks(defaults);
        return defaults;
    }

    private static void SaveWorkbooks(Dictionary<string, List<string>> workbooks)
    {
        ConfigManager.Set("subject_workbooks", JsonSerializer.Serialize(workbooks));
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
        var workbooks = GetWorkbooks();
        workbooks.Remove(name);
        SaveWorkbooks(workbooks);
        LoadSubjects();
    }

    private void AddWorkbook(string subject, string workbook)
    {
        var workbooks = GetWorkbooks();
        if (!workbooks.ContainsKey(subject))
            workbooks[subject] = new List<string>();
        if (workbooks[subject].Contains(workbook)) return;
        workbooks[subject].Add(workbook);
        SaveWorkbooks(workbooks);
        LoadSubjects();
    }

    private void RenameWorkbook(string subject, string oldName, string newName)
    {
        var workbooks = GetWorkbooks();
        if (!workbooks.ContainsKey(subject)) return;
        var idx = workbooks[subject].IndexOf(oldName);
        if (idx < 0) return;
        workbooks[subject][idx] = newName;
        SaveWorkbooks(workbooks);
        LoadSubjects();
    }

    private void DeleteWorkbook(string subject, string workbook)
    {
        var workbooks = GetWorkbooks();
        if (!workbooks.ContainsKey(subject)) return;
        workbooks[subject].Remove(workbook);
        SaveWorkbooks(workbooks);
        LoadSubjects();
    }
}
