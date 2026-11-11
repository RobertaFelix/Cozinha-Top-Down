using UnityEngine;

public struct ItemTooltipContent
{
    public string Title;
    public string Body;
    public Sprite Icon;

    public bool IsEmpty => string.IsNullOrWhiteSpace(Title);

    public ItemTooltipContent(string title, string body, Sprite icon = null)
    {
        Title = title ?? "";
        Body = body ?? "";
        Icon = icon;
    }
}
