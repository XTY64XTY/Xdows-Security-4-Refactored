using Xdows.Security.Core.Text;

namespace Xdows.Security.Core.Scanning;

/// <summary>字节序列查找，对应易语言的 <c>寻找字节集</c> / <c>寻找文本</c>。</summary>
internal static class ByteSearch
{
    /// <summary>等价于 <c>寻找字节集 (数据, 内容) ≠ -1</c>。</summary>
    public static bool Contains(ReadOnlySpan<byte> data, ReadOnlySpan<byte> value) => data.IndexOf(value) >= 0;

    /// <summary>等价于 <c>寻找字节集 (数据, 到字节集 (内容)) ≠ -1</c>，内容按 GBK 编码。</summary>
    public static bool ContainsText(ReadOnlySpan<byte> data, string value) => Contains(data, AnsiText.ToBytes(value));

    /// <summary>等价于 <c>寻找文本 (文本, 内容, , 假) ＞ -1</c>（不区分大小写）。</summary>
    public static bool ContainsTextIgnoreCase(string text, string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);
}
