using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;

namespace AssignSticker_X.windows;

public partial class authers_window : Window
{
    public authers_window()
    {
        InitializeComponent();
        LoadList("grouplist.json", "group_list", GroupContainer);
        LoadList("auther_list.json", "auther_list", IndividualContainer);
    }

    private void LoadList(string fileName, string key, StackPanel container)
    {
        var uri = new Uri($"avares://AssignSticker_X/Assets/config_dir/{fileName}");
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = JsonNode.Parse(json);
        var list = doc?[key]?.AsArray();
        if (list == null) return;

        foreach (var item in list)
        {
            if (item is JsonObject obj && obj.ContainsKey("name"))
            {
                var expander = new FASettingsExpander
                {
                    Header = obj["name"]?.ToString(),
                    Description = obj["description"]?.ToString(),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var avatarUri = obj["avatar"]?.ToString();
                if (!string.IsNullOrEmpty(avatarUri))
                {
                    try
                    {
                        var bitmap = new Bitmap(AssetLoader.Open(new Uri(avatarUri)));
                        expander.IconSource = new FAImageIconSource { Source = bitmap };
                    }
                    catch { }
                }

                container.Children.Add(expander);
            }
        }
    }
}
