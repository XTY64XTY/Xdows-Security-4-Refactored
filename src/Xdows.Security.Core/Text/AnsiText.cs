using System.Text;

namespace Xdows.Security.Core.Text;

/// <summary>
/// 易语言的 <c>到字节集 (文本)</c> 使用系统 ANSI 代码页（简体中文下为 GBK/936）。
/// 扫描规则中有大量按原始字节比对的汉字特征串，因此这里固定使用 936 代码页，
/// 保证与原始引擎的字节序列一致。
/// </summary>
public static class AnsiText
{
    private static readonly Encoding Gbk = CreateGbk();

    private static Encoding CreateGbk()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(936);
    }

    /// <summary>把文本编码为 GBK 字节序列，等价于易语言的 <c>到字节集</c>。</summary>
    public static byte[] ToBytes(string value) => Gbk.GetBytes(value);

    /// <summary>按 GBK 解码字节序列，等价于易语言的 <c>到文本</c>（遇到 0 截止）。</summary>
    public static string ToText(ReadOnlySpan<byte> value)
    {
        int length = value.IndexOf((byte)0);
        if (length < 0)
        {
            length = value.Length;
        }

        return Gbk.GetString(value[..length]);
    }
}
