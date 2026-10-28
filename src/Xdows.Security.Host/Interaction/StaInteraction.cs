using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Xdows.Security.Plugins;

namespace Xdows.Security.Host.Interaction;

/// <summary>
/// 控制台宿主的交互实现：所有对话框都在独立 STA 线程上执行，
/// 避免在 Web 请求线程（MTA）上直接调用 COM 组件。
/// </summary>
public sealed class StaInteraction : IUserInteraction, IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private bool _disposed;

    public StaInteraction()
    {
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "Xdows.Interaction.STA",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    /// <summary>对话框的属主窗口，优先使用控制台窗口句柄。</summary>
    private static IntPtr OwnerWindow => GetConsoleWindow();

    public string? PickFile(string title, string initialDirectory, string filter) =>
        Invoke(() => NativeFileDialog.OpenFile(OwnerWindow, title, initialDirectory, filter));

    public string? PickFolder(string title) =>
        Invoke(() => NativeFileDialog.OpenFolder(OwnerWindow, title));

    public bool Confirm(string caption, string instruction, string content) =>
        Invoke(() => TaskDialogInterop.Confirm(OwnerWindow, caption, instruction, content));

    public void ShowMessage(string caption, string instruction, string content) =>
        Invoke(() =>
        {
            TaskDialogInterop.ShowError(OwnerWindow, caption, instruction, content);
            return true;
        });

    public void OpenWithDefaultApplication(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private T Invoke<T>(Func<T> action)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(StaInteraction));
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    private void MessageLoop()
    {
        foreach (Action action in _queue.GetConsumingEnumerable())
        {
            action();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
