namespace Xdows.Security.Plugins;

/// <summary>
/// Xdows Tools 插件契约。对应原插件 DLL 导出的
/// <c>SetUITitle</c> / <c>GetInfo</c> / <c>SetUIHtml</c> 以及各功能函数。
/// </summary>
public interface IPlugin
{
    /// <summary>页签标题，对应 <c>SetUITitle</c>。</summary>
    string Title { get; }

    /// <summary>插件简介，对应 <c>GetInfo</c>。</summary>
    string Info { get; }

    /// <summary>插件界面 HTML，对应 <c>SetUIHtml</c>。</summary>
    string GetUiHtml(IPluginContext context);

    /// <summary>
    /// 调用插件功能，对应 <c>/Function/&lt;插件名&gt;-&lt;函数名&gt;?参数…</c>。
    /// 返回内容会原样回传给页面。
    /// </summary>
    string Invoke(string function, IReadOnlyList<string> arguments, IPluginContext context);
}

/// <summary>插件运行上下文。</summary>
public interface IPluginContext
{
    /// <summary>程序根目录（含 Bin、Plugins 的目录）。</summary>
    string AppDirectory { get; }

    /// <summary>插件自身目录，即 <c>Plugins\&lt;插件名&gt;</c>。</summary>
    string PluginDirectory { get; }

    /// <summary>宿主提供的交互能力（文件对话框、确认框等）。</summary>
    IUserInteraction Interaction { get; }
}

/// <summary>宿主交互能力，屏蔽控制台宿主与窗口宿主之间的差异。</summary>
public interface IUserInteraction
{
    /// <summary>选择单个文件；取消返回 <c>null</c>。</summary>
    string? PickFile(string title, string initialDirectory, string filter);

    /// <summary>选择目录；取消返回 <c>null</c>。</summary>
    string? PickFolder(string title);

    /// <summary>确认对话框，对应原代码里的 TaskDialog 二次确认。</summary>
    bool Confirm(string caption, string instruction, string content);

    /// <summary>提示对话框。</summary>
    void ShowMessage(string caption, string instruction, string content);

    /// <summary>用系统默认方式打开文件，对应易语言的 <c>执行 (4, 路径, …)</c>。</summary>
    void OpenWithDefaultApplication(string path);
}
