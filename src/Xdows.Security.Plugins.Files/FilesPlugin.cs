using Xdows.Security.Plugins;

namespace Xdows.Security.Plugins.BuiltIn.Files;

/// <summary>文件粉碎插件，移植自 <c>Plugins\Files\Files\Main.e</c>。</summary>
public sealed class FilesPlugin : IPlugin
{
    public string Title => "文件粉碎";

    public string Info => "尝试删除无法正常删除的文件";

    public string GetUiHtml(IPluginContext context) =>
        File.ReadAllText(Path.Combine(context.PluginDirectory, "Files", "Files.html"));

    public string Invoke(string function, IReadOnlyList<string> arguments, IPluginContext context)
    {
        switch (function)
        {
            case "AddFile":
                return AddFile(context);

            default:
                return string.Empty;
        }
    }

    private static string AddFile(IPluginContext context)
    {
        string? path = context.Interaction.PickFile("选择需要加入的文件", @"C:\", "所有文件 (*.*)|*.*");
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return $"<tr><td>{Path.GetFileName(path)}</td><td>{path}</td></tr>";
    }
}
