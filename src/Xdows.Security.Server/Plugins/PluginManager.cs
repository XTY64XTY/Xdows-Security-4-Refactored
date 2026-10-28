using System.Reflection;
using System.Runtime.Loader;
using Xdows.Security.Plugins;

namespace Xdows.Security.Server.Plugins;

/// <summary>已加载的插件。</summary>
public sealed record PluginEntry(string Name, string Directory, IPlugin Plugin);

/// <summary>
/// 插件加载与管理。目录约定与原项目一致：<c>Plugins\&lt;插件名&gt;\Files\Main.dll</c>。
/// </summary>
public sealed class PluginManager
{
    private readonly AppPaths _paths;
    private readonly IUserInteraction _interaction;
    private readonly Action<string> _log;
    private readonly List<PluginEntry> _entries = [];
    private readonly List<PluginLoadContext> _loadContexts = [];
    private readonly Dictionary<string, PluginEntry> _byName = new(StringComparer.OrdinalIgnoreCase);

    public PluginManager(AppPaths paths, IUserInteraction interaction, Action<string>? log = null)
    {
        _paths = paths;
        _interaction = interaction;
        _log = log ?? (_ => { });
    }

    /// <summary>已加载插件，顺序与目录扫描顺序一致。</summary>
    public IReadOnlyList<PluginEntry> Entries => _entries;

    /// <summary>扫描 <c>Plugins</c> 目录并加载插件，对应原 <c>方法_首页</c> 里遍历插件目录的逻辑。</summary>
    public void Load()
    {
        _entries.Clear();
        _byName.Clear();
        foreach (PluginLoadContext context in _loadContexts)
        {
            context.Unload();
        }

        _loadContexts.Clear();

        if (!System.IO.Directory.Exists(_paths.PluginsDirectory))
        {
            return;
        }

        foreach (string directory in System.IO.Directory.EnumerateDirectories(_paths.PluginsDirectory).OrderBy(static p => p, StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(directory);
            string assemblyPath = Path.Combine(directory, "Files", "Main.dll");
            if (!File.Exists(assemblyPath))
            {
                continue;
            }

            try
            {
                // 每个插件使用独立加载上下文：插件程序集同名（Main.dll）也必须能同时存在。
                var context = new PluginLoadContext(name);
                _loadContexts.Add(context);

                Assembly assembly = context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
                Type? type = assembly.GetTypes().FirstOrDefault(static t =>
                    typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

                if (type == null)
                {
                    throw new InvalidOperationException("未找到 IPlugin 实现。");
                }

                if (Activator.CreateInstance(type) is not IPlugin plugin)
                {
                    throw new InvalidOperationException("插件实例创建失败。");
                }

                var entry = new PluginEntry(name, directory, plugin);
                _entries.Add(entry);
                _byName[name] = entry;
            }
            catch (Exception ex)
            {
                _log(FormatLoadFailure(name, assemblyPath, ex));
            }
        }
    }

    /// <summary>取插件界面 HTML，对应原 <c>DLL调用 (…, "SetUIHtml")</c>。</summary>
    public string GetUiHtml(string name)
    {
        if (!_byName.TryGetValue(name, out PluginEntry? entry))
        {
            return string.Empty;
        }

        try
        {
            return entry.Plugin.GetUiHtml(CreateContext(entry));
        }
        catch (Exception ex)
        {
            _log($"插件 {name} 读取界面失败：{ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>调用插件功能，对应原 <c>转换DLL调用</c>。</summary>
    public string Invoke(string name, string function, IReadOnlyList<string> arguments)
    {
        if (!_byName.TryGetValue(name, out PluginEntry? entry))
        {
            return string.Empty;
        }

        try
        {
            return entry.Plugin.Invoke(function, arguments, CreateContext(entry));
        }
        catch (Exception ex)
        {
            _log($"插件 {name} 执行 {function} 失败：{ex.Message}");
            return string.Empty;
        }
    }

    private PluginContext CreateContext(PluginEntry entry) => new(_paths.Root, entry.Directory, _interaction);

    /// <summary>对应常量「无法加载插件」的提示文本。</summary>
    private static string FormatLoadFailure(string name, string assemblyPath, Exception ex) =>
        $"无法加载插件{Environment.NewLine}" +
        $"插件名称：{name}{Environment.NewLine}" +
        $"加载原因：{assemblyPath} 加载失败{Environment.NewLine}{ex.Message}";
}
