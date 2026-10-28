using Windows.UI;

namespace Xdows.Security.Client;

/// <summary>把网页里的 CSS 颜色值（<c>#RRGGBB</c> / <c>#AARRGGBB</c> / <c>rgb()</c>）转换为 <see cref="Color"/>。</summary>
public static class ColorParser
{
    public static Color Parse(string value, Color fallback)
    {
        string text = value.Trim();
        if (text.Length == 0)
        {
            return fallback;
        }

        if (text.StartsWith('#'))
        {
            return ParseHex(text[1..], fallback);
        }

        if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRgb(text, fallback);
        }

        return fallback;
    }

    private static Color ParseHex(string hex, Color fallback)
    {
        try
        {
            return hex.Length switch
            {
                6 => Color.FromArgb(255, Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16)),
                8 => Color.FromArgb(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16), Convert.ToByte(hex.Substring(6, 2), 16)),
                _ => fallback,
            };
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private static Color ParseRgb(string text, Color fallback)
    {
        int open = text.IndexOf('(');
        int close = text.IndexOf(')');
        if (open < 0 || close <= open)
        {
            return fallback;
        }

        string[] parts = text[(open + 1)..close].Split(',');
        if (parts.Length < 3)
        {
            return fallback;
        }

        try
        {
            byte red = (byte)double.Parse(parts[0].Trim());
            byte green = (byte)double.Parse(parts[1].Trim());
            byte blue = (byte)double.Parse(parts[2].Trim());
            byte alpha = parts.Length >= 4 ? (byte)(double.Parse(parts[3].Trim()) * 255) : (byte)255;
            return Color.FromArgb(alpha, red, green, blue);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}
