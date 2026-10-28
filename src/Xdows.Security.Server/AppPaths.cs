namespace Xdows.Security.Server;

/// <summary>程序目录约定，与原项目 <c>取运行目录 ()</c> 下的结构保持一致。</summary>
public sealed class AppPaths
{
    public AppPaths(string root)
    {
        Root = Path.GetFullPath(root);
    }

    /// <summary>程序根目录。</summary>
    public string Root { get; }

    /// <summary>网页资源目录 <c>Bin\Html</c>。</summary>
    public string HtmlDirectory => Path.Combine(Root, "Bin", "Html");

    /// <summary>批处理等附属文件目录 <c>Bin\Function</c>。</summary>
    public string FunctionDirectory => Path.Combine(Root, "Bin", "Function");

    /// <summary>插件目录 <c>Plugins</c>。</summary>
    public string PluginsDirectory => Path.Combine(Root, "Plugins");

    public string IndexFallbackPath => Path.Combine(HtmlDirectory, "GoToIndex.html");

    public string ColorCssPath => Path.Combine(HtmlDirectory, "Color.css");

    public string CleanBatchPath => Path.Combine(FunctionDirectory, "Clean.bat");
}
