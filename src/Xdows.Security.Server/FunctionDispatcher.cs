using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Xdows.Security.Core.Scanning;
using Xdows.Security.Plugins;
using Xdows.Security.Server.Plugins;
using Xdows.Security.Server.Web;

namespace Xdows.Security.Server;

/// <summary>
/// <c>/Function/*</c> 请求的处理，移植自原 <c>窗口程序集_窗口1</c> 的 <c>执行功能</c>。
/// </summary>
public sealed class FunctionDispatcher
{
    private readonly AppPaths _paths;
    private readonly ScanEngine _engine;
    private readonly PluginManager _plugins;
    private readonly IUserInteraction _interaction;
    private readonly Action<string> _log;

    public FunctionDispatcher(AppPaths paths, ScanEngine engine, PluginManager plugins, IUserInteraction interaction, Action<string>? log = null)
    {
        _paths = paths;
        _engine = engine;
        _plugins = plugins;
        _interaction = interaction;
        _log = log ?? (_ => { });
    }

    public string Execute(string function, HttpRequestInfo request)
    {
        switch (function)
        {
            case "Clean":
                return RunCleanBatch();

            case "OpenColorCssFiles":
                _interaction.OpenWithDefaultApplication(_paths.ColorCssPath);
                return string.Empty;

            case "ScanFast":
            case "ScanAll":
                // 原实现在服务端弹出「暂未实现」，前端页面本身也会提示；保持返回空内容。
                return string.Empty;

            case "AddPlugins":
                return AddPlugins();

            case "ScanDir":
                return ScanDirectory();

            case "ScanFiles":
                return ScanFiles();

            case "DelFiles":
                return DeleteFiles(request);

            default:
                return InvokePlugin(function, request);
        }
    }

    private string RunCleanBatch()
    {
        if (!File.Exists(_paths.CleanBatchPath))
        {
            _log($"未找到清理脚本：{_paths.CleanBatchPath}");
            return string.Empty;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _paths.CleanBatchPath,
                WorkingDirectory = _paths.FunctionDirectory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _log($"启动清理脚本失败：{ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>对应 <c>AddPlugins</c>：选择插件包并解压到 Plugins 目录。</summary>
    private string AddPlugins()
    {
        string? path = _interaction.PickFile("选择需要加入的插件包", _paths.Root, "插件包 (*.zip)|*.zip|所有文件 (*.*)|*.*");
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        try
        {
            ZipFile.ExtractToDirectory(path, _paths.PluginsDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            _log($"解压插件包失败：{ex.Message}");
            _interaction.ShowMessage("Xdows Tools", "插件导入失败", ex.Message);
            return string.Empty;
        }

        // 刷新插件列表，页面重新加载后即可看到新插件。
        _plugins.Load();
        return string.Empty;
    }

    /// <summary>对应 <c>ScanDir</c>：选择目录并对其中所有文件做扫描。</summary>
    private string ScanDirectory()
    {
        string? path = _interaction.PickFolder("选择需要扫描的目录");
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (string file in EnumerateFiles(path))
        {
            string result = _engine.Scan(file, deepMode: false);
            if (result.Length == 0)
            {
                continue;
            }

            builder.Append('\n').Append(BuildRow(Path.GetFileName(file), result, file));
        }

        return builder.ToString();
    }

    /// <summary>对应 <c>ScanFiles</c>：选择单个文件扫描。</summary>
    private string ScanFiles()
    {
        string? path = _interaction.PickFile("选择需要扫描的文件", @"C:\", "所有文件 (*.*)|*.*");
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        string result = _engine.Scan(path, deepMode: false);
        if (result.Length == 0)
        {
            return string.Empty;
        }

        return BuildRow(Path.GetFileName(path), result, path);
    }

    /// <summary>
    /// 对应 <c>DelFiles</c>：参数依次为「文件列表」与「病毒名列表」，均以 <c>|||</c> 分隔且经过 URL 编码。
    /// 返回删除失败的条目。
    /// </summary>
    private string DeleteFiles(HttpRequestInfo request)
    {
        string filesValue = request.GetValue(0);
        if (filesValue == "Error" || filesValue.Length == 0)
        {
            return string.Empty;
        }

        string[] files = DecodeValue(filesValue).Split("|||");
        string[] threats = DecodeValue(request.GetValue(1)).Split("|||");

        var builder = new StringBuilder();
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            if (file.Length == 0)
            {
                continue;
            }

            if (TryDeleteFile(file))
            {
                continue;
            }

            string threat = i < threats.Length ? threats[i] : string.Empty;
            builder.Append('\n').Append(BuildRow(Path.GetFileName(file), threat, file));
        }

        return builder.ToString();
    }

    private static bool TryDeleteFile(string path)
    {
        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>对应 <c>转换DLL调用</c>：功能名形如 <c>插件名-函数名</c>，最多透传两个参数。</summary>
    private string InvokePlugin(string function, HttpRequestInfo request)
    {
        string[] parts = function.Split('-');
        if (parts.Length < 2)
        {
            return string.Empty;
        }

        return _plugins.Invoke(parts[0], parts[1], request.GetValues());
    }

    /// <summary>对应 <c>子程序_查找所有文件</c>：递归列出目录下所有文件。</summary>
    private static IEnumerable<string> EnumerateFiles(string directory)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (string file in files)
        {
            yield return file;
        }

        IEnumerable<string> subDirectories;
        try
        {
            subDirectories = Directory.EnumerateDirectories(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (string subDirectory in subDirectories)
        {
            foreach (string file in EnumerateFiles(subDirectory))
            {
                yield return file;
            }
        }
    }

    private static string BuildRow(string name, string threat, string path) =>
        $"<tr><td>{name}</td><td>{threat}</td><td>{path}</td></tr>";

    /// <summary>对应 <c>编码_URL解码 (值, 假)</c>。</summary>
    private static string DecodeValue(string value) => Uri.UnescapeDataString(value);
}
