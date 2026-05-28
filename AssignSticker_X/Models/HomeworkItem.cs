using System.Collections.Generic;

namespace AssignSticker_X.Models;

public class HomeworkItem
{
    public string Subject { get; set; } = "";
    public string Type { get; set; } = "";
    public string? WorkbookName { get; set; }
    public string? StartPage { get; set; }
    public string? EndPage { get; set; }
    public string? Note { get; set; }
    public string? Content { get; set; }
    public List<string> Tags { get; set; } = new();
}
