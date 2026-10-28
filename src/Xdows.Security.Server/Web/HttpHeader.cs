using System.Text;

namespace Xdows.Security.Server.Web;

/// <summary>
/// 响应头模板，来自原项目的常量「协议头」：
/// <code>
/// HTTP/1.1 200 OK
/// Server: Xdows Security Web Server
/// Content-Type:text/%s;charset=UTF-8
/// Access-Control-Allow-Origin:*
/// Access-Control-Allow-Methods:POST,GET,OPTIONS,DELETE
/// Access-Control-Allow-Headers:x-requested-with,content-type
/// </code>
/// 额外补上 Content-Length 与 Connection: close，便于客户端判定响应结束。
/// </summary>
public static class HttpHeader
{
    public static byte[] Build(string extension, int contentLength)
    {
        string contentType = MapExtension(extension);
        var builder = new StringBuilder(256);
        builder.Append("HTTP/1.1 200 OK\r\n");
        builder.Append("Server: Xdows Security Web Server\r\n");
        builder.Append("Content-Type:text/").Append(contentType).Append(";charset=UTF-8\r\n");
        builder.Append("Access-Control-Allow-Origin:*\r\n");
        builder.Append("Access-Control-Allow-Methods:POST,GET,OPTIONS,DELETE\r\n");
        builder.Append("Access-Control-Allow-Headers:x-requested-with,content-type\r\n");
        builder.Append("Content-Length: ").Append(contentLength).Append("\r\n");
        builder.Append("Connection: close\r\n");
        builder.Append("\r\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    /// <summary>对应原 <c>发送数据</c> 中的扩展名映射：<c>js</c> → <c>javascript</c>，空 → <c>html</c>。</summary>
    public static string MapExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return "html";
        }

        return string.Equals(extension, "js", StringComparison.OrdinalIgnoreCase) ? "javascript" : extension;
    }
}
