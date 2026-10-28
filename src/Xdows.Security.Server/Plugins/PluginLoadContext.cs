using System.Reflection;
using System.Runtime.Loader;

namespace Xdows.Security.Server.Plugins;

/// <summary>
/// 单个插件的加载上下文。插件之间的程序集同名（都叫 Main.dll），
/// 必须各自隔离；对插件契约等共享程序集则回落到默认上下文，
/// 以保证 <see cref="Plugins.IPlugin"/> 的类型标识一致。
/// </summary>
internal sealed class PluginLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true)
{
    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
