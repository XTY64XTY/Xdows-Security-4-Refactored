using Microsoft.UI.Xaml;

namespace Xdows.Security.Client;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>默认连接本机 20000 端口，可用 <c>--url</c> 覆盖。</summary>
    public static string ResolveStartUrl(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--url", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return "http://127.0.0.1:20000/";
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        string url = ResolveStartUrl(Environment.GetCommandLineArgs());
        _window = new MainWindow(url);
        _window.Activate();
    }
}
