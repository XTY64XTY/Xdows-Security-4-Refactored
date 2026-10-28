using System.Text;
using Xdows.Security.Server.Plugins;
using Xdows.Security.Server.Web;

namespace Xdows.Security.Server;

/// <summary>
/// 请求路由，移植自原 <c>方法_首页</c>：先处理 <c>/Function/*</c>、插件界面、
/// 默认页与 Xdows Tools 页，最后回落到 <c>Bin\Html</c> 下的静态文件。
/// </summary>
public sealed class SiteRouter
{
    private const string PluginCardTemplateName = "FileDescription.html";
    private const string PluginPaneTemplateName = "FileData.html";

    private readonly AppPaths _paths;
    private readonly PluginManager _plugins;
    private readonly FunctionDispatcher _functions;
    private readonly Action<string> _log;
    private readonly object _templateLock = new();

    private string? _pluginCardTemplate;
    private string? _pluginPaneTemplate;

    public SiteRouter(AppPaths paths, PluginManager plugins, FunctionDispatcher functions, Action<string>? log = null)
    {
        _paths = paths;
        _plugins = plugins;
        _functions = functions;
        _log = log ?? (_ => { });
    }

    public RouteResult Route(HttpRequestInfo request)
    {
        string fileName = request.Path;
        string extension = GetExtension(fileName);

        // /Function/<功能名>?参数…
        if (fileName.StartsWith("Function", StringComparison.Ordinal))
        {
            string[] segments = fileName.Split('/');
            string function = segments.Length > 1 ? segments[1] : string.Empty;
            string body = _functions.Execute(function, request);
            return new RouteResult(Encoding.UTF8.GetBytes(body), extension);
        }

        // /Plugins-<插件名>/…：返回插件自带的界面
        if (fileName.StartsWith("Plugins", StringComparison.Ordinal))
        {
            string pluginName = ExtractPluginName(fileName);
            string html = _plugins.GetUiHtml(pluginName);
            return new RouteResult(Encoding.UTF8.GetBytes(html), "html");
        }

        if (fileName.Length == 0)
        {
            return ReadHtmlFile(_paths.IndexFallbackPath, extension);
        }

        if (fileName.Contains("Xdows-Tools", StringComparison.Ordinal))
        {
            return RenderXdowsTools(fileName, extension);
        }

        string htmlPath = Path.Combine(_paths.HtmlDirectory, fileName);
        if (File.Exists(htmlPath))
        {
            return ReadHtmlFile(htmlPath, extension);
        }

        if (File.Exists(htmlPath + ".html"))
        {
            return ReadHtmlFile(htmlPath + ".html", extension);
        }

        return ReadHtmlFile(_paths.IndexFallbackPath, extension);
    }

    /// <summary>把 <c>Plugins-Files/Html/</c> 解析成插件名 <c>Files</c>。</summary>
    private static string ExtractPluginName(string fileName)
    {
        int dash = fileName.IndexOf('-');
        if (dash < 0)
        {
            return string.Empty;
        }

        string rest = fileName[(dash + 1)..];
        int slash = rest.IndexOf('/');
        return slash < 0 ? rest : rest[..slash];
    }

    /// <summary>渲染 Xdows Tools 页：把 <c>PluginsData</c> / <c>PluginsInfo</c> 替换为插件卡片与页签。</summary>
    private RouteResult RenderXdowsTools(string fileName, string extension)
    {
        string path = Path.Combine(_paths.HtmlDirectory, fileName);
        if (!File.Exists(path))
        {
            return ReadHtmlFile(_paths.IndexFallbackPath, extension);
        }

        string page;
        try
        {
            page = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            _log($"读取 {path} 失败：{ex.Message}");
            return ReadHtmlFile(_paths.IndexFallbackPath, extension);
        }

        var paneBuilder = new StringBuilder();
        var cardBuilder = new StringBuilder();

        foreach (PluginEntry entry in _plugins.Entries)
        {
            string title;
            string info;
            try
            {
                title = entry.Plugin.Title;
                info = entry.Plugin.Info;
            }
            catch (Exception ex)
            {
                _log($"插件 {entry.Name} 读取标题/简介失败：{ex.Message}");
                continue;
            }

            string pane = ReplaceKeys(LoadPaneTemplate(), ("FilesPath", "Plugins-" + entry.Name + "/Html/"), ("tabid", entry.Name + "TabID"), ("FilesName", title));
            string card = ReplaceKeys(LoadCardTemplate(), ("FilesInfo", info), ("tabid", entry.Name + "TabID"), ("FilesName", title));

            paneBuilder.Append(pane).Append('\n');
            cardBuilder.Append(card).Append('\n');
        }

        page = ReplaceKeys(page, ("PluginsData", paneBuilder.ToString()), ("PluginsInfo", cardBuilder.ToString()));
        return new RouteResult(Encoding.UTF8.GetBytes(page), extension);
    }

    /// <summary>对应 <c>文本_替换</c>：依次替换占位符。</summary>
    private static string ReplaceKeys(string text, params (string Key, string Value)[] pairs)
    {
        foreach ((string key, string value) in pairs)
        {
            text = text.Replace(key, value, StringComparison.Ordinal);
        }

        return text;
    }

    private string LoadPaneTemplate()
    {
        lock (_templateLock)
        {
            _pluginPaneTemplate ??= ReadPluginTemplate(PluginPaneTemplateName);
            return _pluginPaneTemplate;
        }
    }

    private string LoadCardTemplate()
    {
        lock (_templateLock)
        {
            _pluginCardTemplate ??= ReadPluginTemplate(PluginCardTemplateName);
            return _pluginCardTemplate;
        }
    }

    private string ReadPluginTemplate(string name)
    {
        string path = Path.Combine(_paths.HtmlDirectory, "Xdows-Tools", "Plugins", name);
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            _log($"读取插件模板 {path} 失败：{ex.Message}");
            return string.Empty;
        }
    }

    private static RouteResult ReadHtmlFile(string path, string extension)
    {
        try
        {
            return new RouteResult(File.ReadAllBytes(path), extension);
        }
        catch (IOException)
        {
            return new RouteResult([], extension);
        }
    }

    /// <summary>取扩展名（不含点），对应 <c>文件_取扩展名</c> 后去掉前导点。</summary>
    private static string GetExtension(string path)
    {
        int slash = path.LastIndexOfAny(['/', '\\']);
        int dot = path.LastIndexOf('.');
        if (dot <= slash || dot == path.Length - 1)
        {
            return string.Empty;
        }

        return path[(dot + 1)..];
    }
}
