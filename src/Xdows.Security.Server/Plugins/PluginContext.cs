using Xdows.Security.Plugins;

namespace Xdows.Security.Server.Plugins;

internal sealed class PluginContext(string appDirectory, string pluginDirectory, IUserInteraction interaction) : IPluginContext
{
    public string AppDirectory { get; } = appDirectory;

    public string PluginDirectory { get; } = pluginDirectory;

    public IUserInteraction Interaction { get; } = interaction;
}
