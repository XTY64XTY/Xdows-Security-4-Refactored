using System.Runtime.InteropServices;

namespace Xdows.Security.Host.Interaction;

/// <summary>
/// 任务对话框，对应原插件的 <c>comctl32!TaskDialog</c> 调用。
/// 需要 <c>app.manifest</c> 声明 Common-Controls 6。
/// </summary>
internal static class TaskDialogInterop
{
    private const uint TdcbfOkButton = 0x0001;
    private const uint TdcbfYesButton = 0x0002;
    private const uint TdcbfNoButton = 0x0004;

    /// <summary>MAKEINTRESOURCE(-1)：警告图标。</summary>
    private const int TdWarningIcon = 0xFFFF;

    /// <summary>MAKEINTRESOURCE(-4)：错误图标。</summary>
    private const int TdErrorIcon = 0xFFFC;

    private const int IdOk = 1;
    private const int IdYes = 6;

    public static bool Confirm(IntPtr owner, string caption, string instruction, string content)
    {
        int result = Show(owner, caption, instruction, content, TdcbfYesButton | TdcbfNoButton, TdWarningIcon);
        return result == IdYes;
    }

    public static void ShowError(IntPtr owner, string caption, string instruction, string content) =>
        Show(owner, caption, instruction, content, TdcbfOkButton, TdErrorIcon);

    private static int Show(IntPtr owner, string caption, string instruction, string content, uint buttons, int icon)
    {
        int hr = TaskDialog(owner, IntPtr.Zero, caption, instruction, content, buttons, icon, out int pressed);
        return hr == 0 ? pressed : 0;
    }

    [DllImport("comctl32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int TaskDialog(
        IntPtr hwndOwner,
        IntPtr hInstance,
        [MarshalAs(UnmanagedType.LPWStr)] string pszWindowTitle,
        [MarshalAs(UnmanagedType.LPWStr)] string pszMainInstruction,
        [MarshalAs(UnmanagedType.LPWStr)] string pszContent,
        uint dwCommonButtons,
        IntPtr pszIcon,
        out int pnButton);
}
