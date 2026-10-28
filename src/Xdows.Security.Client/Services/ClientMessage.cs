using System.Text.Json;

namespace Xdows.Security.Client;

/// <summary>网页 → 客户端 的消息类型。</summary>
public static class ClientMessageKinds
{
    /// <summary>对应原 CEF 客户端对象 <c>Client.xxx()</c> 的调用。</summary>
    public const string Client = "client";

    /// <summary>开始扫描，显示进度窗口。</summary>
    public const string BeginScan = "scan";

    /// <summary>开始安装插件，显示进度窗口。</summary>
    public const string BeginInstall = "install";

    /// <summary>结束进度窗口。</summary>
    public const string EndProgress = "done";
}

/// <summary>注入脚本 <c>ClientShim</c> 发出的消息。</summary>
public sealed class ClientMessage
{
    public string Kind { get; init; } = string.Empty;

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string FirstArgument(string fallback) => Arguments.Count > 0 && Arguments[0].Length > 0 ? Arguments[0] : fallback;

    public static ClientMessage? Parse(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string kind = root.TryGetProperty("kind", out JsonElement kindElement) ? kindElement.GetString() ?? string.Empty : string.Empty;
            var arguments = new List<string>();

            if (root.TryGetProperty("args", out JsonElement argsElement) && argsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in argsElement.EnumerateArray())
                {
                    arguments.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
                }
            }

            return new ClientMessage { Kind = kind, Arguments = arguments };
        }
        catch (JsonException)
        {
        }

        return null;
    }
}

/// <summary>
/// 注入到每个文档的脚本：把网页侧的 <c>Client.postMessage(...)</c> 映射到 WebView2 消息通道，
/// 这样原有网页代码无需改动。
/// </summary>
public static class ClientShim
{
    public const string Script = """
        (function () {
            if (!window.chrome || !window.chrome.webview) { return; }

            function send(kind, args) {
                window.chrome.webview.postMessage({ kind: kind, args: args });
            }

            window.Client = window.Client || {};
            window.Client.postMessage = function () {
                send('client', Array.prototype.slice.call(arguments));
            };
            window.Client.showScanProgress = function (text) {
                send('scan', [text || '正在扫描']);
            };
            window.Client.showPluginInstall = function (text) {
                send('install', [text || '正在解压插件包']);
            };
            window.Client.endProgress = function () {
                send('done', []);
            };
        })();
        """;
}
