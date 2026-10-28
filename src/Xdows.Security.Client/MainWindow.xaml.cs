using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Xdows.Security.Client.Views;

namespace Xdows.Security.Client;

/// <summary>
/// 客户端主窗口：用 WebView2 承载本机 Web 界面，替代原项目的 CEF（ACF）封装。
/// 通过注入的 <c>window.Client</c> 脚本对象与网页通信，保持原有 <c>Client.postMessage</c> 调用不变。
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly string _startUrl;
    private readonly AppWindow _appWindow;
    private ProgressWindow? _progressWindow;
    private bool _webViewInitialized;

    public MainWindow(string startUrl)
    {
        InitializeComponent();
        _startUrl = startUrl;
        Title = "Xdows Security";

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarGrid);

        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnClosed;

        _ = InitializeWebViewAsync();
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        double scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;
        _appWindow.Resize(new Windows.Graphics.SizeInt32((int)(850 * scale), (int)(638 * scale)));
        UpdateDevToolsButtonMargin();
    }

    private void UpdateDevToolsButtonMargin()
    {
        double inset = _appWindow.TitleBar.RightInset;
        DevToolsButton.Margin = new Thickness(0, 0, inset + 8, 0);
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            ShowStartupError(ex.Message);
            return;
        }

        var core = WebView.CoreWebView2;
        core.Settings.UserAgent += " Client";
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreDefaultScriptDialogsEnabled = false;

        await core.AddScriptToExecuteOnDocumentCreatedAsync(ClientShim.Script);
        core.WebMessageReceived += OnWebMessageReceived;

        _webViewInitialized = true;
        WebView.Source = new Uri(_startUrl);
    }

    private void OnWebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
    {
        ClientMessage? message = ClientMessage.Parse(args.WebMessageAsJson);
        if (message == null)
        {
            return;
        }

        switch (message.Kind)
        {
            case ClientMessageKinds.Client:
                HandleClientCall(message.Arguments);
                break;

            case ClientMessageKinds.BeginScan:
                ShowProgress(message.FirstArgument("正在扫描"));
                break;

            case ClientMessageKinds.BeginInstall:
                ShowProgress(message.FirstArgument("正在解压插件包"));
                break;

            case ClientMessageKinds.EndProgress:
                CloseProgress();
                break;
        }
    }

    /// <summary>处理网页侧 <c>Client.xxx()</c> 调用。</summary>
    private void HandleClientCall(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return;
        }

        if (string.Equals(arguments[0], "ChangeTheme", StringComparison.Ordinal) && arguments.Count >= 6)
        {
            ApplyTheme(arguments[1], arguments[2], arguments[4], arguments[5]);
        }
    }

    /// <summary>对应原 <c>客户端功能回调.收到消息</c> 中的 ChangeTheme 处理。</summary>
    private void ApplyTheme(string background, string foreground, string themeBackground, string theme)
    {
        Color background_ = ColorParser.Parse(background, Colors.White);
        Color foreground_ = ColorParser.Parse(foreground, Colors.Black);
        Color hover = ColorParser.Parse(themeBackground, background_);

        RootGrid.Background = new SolidColorBrush(background_);
        TitleBarGrid.Background = new SolidColorBrush(background_);
        TitleText.Foreground = new SolidColorBrush(foreground_);

        _appWindow.TitleBar.ButtonBackgroundColor = background_;
        _appWindow.TitleBar.ButtonForegroundColor = foreground_;
        _appWindow.TitleBar.ButtonHoverBackgroundColor = hover;
        _appWindow.TitleBar.ButtonHoverForegroundColor = foreground_;
        _appWindow.TitleBar.ButtonPressedBackgroundColor = hover;
        _appWindow.TitleBar.ButtonPressedForegroundColor = foreground_;
        _appWindow.TitleBar.ButtonInactiveBackgroundColor = background_;
        _appWindow.TitleBar.ButtonInactiveForegroundColor = foreground_;

        // 原始逻辑：仅浅色模式下提供开发者工具入口。
        DevToolsButton.Visibility = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_progressWindow != null)
        {
            _progressWindow.ApplyTheme(background_, foreground_);
        }
    }

    private void ShowProgress(string caption)
    {
        if (_progressWindow == null)
        {
            _progressWindow = new ProgressWindow(caption);
            _progressWindow.Closed += (_, _) => _progressWindow = null;
        }

        _progressWindow.Activate();
    }

    private void CloseProgress()
    {
        _progressWindow?.Close();
        _progressWindow = null;
    }

    private void ShowStartupError(string message)
    {
        RootGrid.Children.Clear();
        RootGrid.Children.Add(new TextBlock
        {
            Text = $"无法连接到本机服务：{message}{Environment.NewLine}请先启动 Xdows.Security.Host。",
            Margin = new Thickness(24),
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private void OnDevToolsClick(object sender, RoutedEventArgs e)
    {
        if (_webViewInitialized)
        {
            WebView.CoreWebView2.OpenDevToolsWindow();
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_webViewInitialized)
        {
            WebView.Close();
        }
    }
}
