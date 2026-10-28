using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Xdows.Security.Client.Views;

/// <summary>
/// 进度窗口，对应原项目的「窗口2（扫描进度）」与「窗口3（插件解压进度）」：
/// 固定 415×250、不可缩放、执行期间禁止关闭、可选「总在最前」。
/// </summary>
public sealed partial class ProgressWindow : Window
{
    private readonly AppWindow _appWindow;
    private bool _allowClose;

    public ProgressWindow(string caption)
    {
        InitializeComponent();

        Title = caption;
        CaptionText.Text = caption;
        StatusText.Text = caption + "，请稍候…";

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        _appWindow.Closing += OnClosing;

        RootGrid.Loaded += OnLoaded;
    }

    /// <summary>状态文本，对应原窗口的「标签2」。</summary>
    public void SetStatus(string status) => StatusText.Text = status;

    /// <summary>执行完成：允许关闭并给出完成提示。</summary>
    public void Complete(string caption, string status)
    {
        Title = caption;
        CaptionText.Text = caption;
        StatusText.Text = status;
        Progress.IsIndeterminate = false;
        Progress.Value = 100;
        _allowClose = true;
        _appWindow.Closing -= OnClosing;
    }

    public void ApplyTheme(Color background, Color foreground)
    {
        RootGrid.Background = new SolidColorBrush(background);
        CaptionText.Foreground = new SolidColorBrush(foreground);
        StatusText.Foreground = new SolidColorBrush(foreground);
        TopmostCheckBox.Foreground = new SolidColorBrush(foreground);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        double scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;
        _appWindow.Resize(new Windows.Graphics.SizeInt32((int)(415 * scale), (int)(250 * scale)));
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }
    }

    private void OnTopmostChanged(object sender, RoutedEventArgs e)
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = TopmostCheckBox.IsChecked == true;
        }
    }

    /// <summary>执行期间禁止关闭，对应原窗口「可否被关闭 → 假」。</summary>
    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_allowClose)
        {
            args.Cancel = true;
        }
    }
}
