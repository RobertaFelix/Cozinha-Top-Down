using System;
using System.Text;
using UnityEngine;

public static class ItemTooltipFormatting
{
    public static string FormatBody(string text, ItemTooltipStyle style)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var result = new StringBuilder();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            if (result.Length > 0) result.Append('\n');
            var line = raw.Replace("</noparse>", "");
            int colon = line.IndexOf(':');
            string label = (colon >= 0 ? line.Substring(0, colon) : "").Trim().ToLowerInvariant();
            Color? color = null;
            if (style != null)
            {
                switch (label)
                {
                    case "value": case "price": case "unit price": case "sale price": color = style.valueColor; break;
                    case "effect": color = style.effectColor; break;
                    case "duration": case "time": case "base cooking time": color = style.durationColor; break;
                    case "quantity": color = style.quantityColor; break;
                    case "ingredients": color = style.ingredientsColor; break;
                }
            }
            if (color.HasValue) result.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color.Value)).Append('>');
            result.Append("<noparse>").Append(line).Append("</noparse>");
            if (color.HasValue) result.Append("</color>");
        }
        return result.ToString();
    }
}
