using System.Diagnostics;
using System.Text;
using Xdows.Security.Plugins;

namespace Xdows.Security.Plugins.BuiltIn.Process;

/// <summary>进程管理插件，移植自 <c>Plugins\Process\Files\Main.e</c>。</summary>
public sealed class ProcessPlugin : IPlugin
{
    public string Title => "进程管理";

    public string Info => "管理系统进程";

    public string GetUiHtml(IPluginContext context) =>
        File.ReadAllText(Path.Combine(context.PluginDirectory, "Files", "Process.html"));

    public string Invoke(string function, IReadOnlyList<string> arguments, IPluginContext context)
    {
        switch (function)
        {
            case "GetProcessList":
                return GetProcessList();

            case "KillProcess":
                return KillProcess(arguments, context);

            default:
                return string.Empty;
        }
    }

    /// <summary>对应 <c>GetProcessList</c>：列出进程名称、PID 与路径。</summary>
    private static string GetProcessList()
    {
        var builder = new StringBuilder();

        foreach ((string name, uint processId, string path) in NativeProcessList.Enumerate())
        {
            if (processId == 0)
            {
                continue;
            }

            string pathText = path.Length == 0 ? "受保护，无法获取" : path;
            builder.Append('\n').Append($"<tr><td>{name}</td><td>{processId}</td><td>{pathText}</td></tr>");
        }

        return builder.ToString();
    }

    /// <summary>对应 <c>KillProcess</c>：先做参数校验与二次确认（原实现不真正结束进程）。</summary>
    private static string KillProcess(IReadOnlyList<string> arguments, IPluginContext context)
    {
        string pid = arguments.Count > 0 ? arguments[0] : string.Empty;

        if (pid.Length == 0)
        {
            context.Interaction.ShowMessage(
                "Xdows Tools",
                "请先选择进程再终止",
                "错误代码：NotChooseProcess" + Environment.NewLine +
                "错误信息：未选择需要终止的进程导致无法确定需要终止的进程。");
            return "Error:NotChooseProcess";
        }

        string name = arguments.Count > 1 ? Uri.UnescapeDataString(arguments[1]) : string.Empty;
        context.Interaction.Confirm(
            "Xdows Tools",
            "你希望结束这个进程吗？",
            "名称：" + name + Environment.NewLine +
            "PID：" + pid + Environment.NewLine +
            "当你选择强力模式时进程会被强行结束，任何相关数据都可能会丢失，甚至还可能会导致系统不稳定或崩溃，请谨慎使用。");

        return "OK";
    }
}
