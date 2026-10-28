using System.Diagnostics;
using Xdows.Security.Core.Scanning;
using Xdows.Security.Host.Interaction;
using Xdows.Security.Server;
using Xdows.Security.Server.Plugins;
using Xdows.Security.Server.Web;

namespace Xdows.Security.Host;

/// <summary>
/// 控制台宿主：承担原 <c>Xdows-Security.e</c> 中「窗口1 + 内置 Web 服务」的职责，
/// 同时提供命令行扫描能力（原 <c>Scan.e</c> 的调试入口）。
/// </summary>
internal static class Program
{
    private const int DefaultPort = 20000;
    private const string ClientFileName = "Xdows.Security.Client.exe";

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return Serve([]);
        }

        return args[0].ToLowerInvariant() switch
        {
            "serve" => Serve(args[1..]),
            "scan" => Scan(args[1..]),
            "version" or "--version" or "-v" => PrintVersion(),
            "help" or "--help" or "-h" or "-?" => PrintUsage(),
            _ => UnknownCommand(args[0]),
        };
    }

    private static int Serve(string[] args)
    {
        int port = DefaultPort;
        string root = AppContext.BaseDirectory;
        bool openBrowser = false;
        string? clientPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out port))
                    {
                        return Fail("--port 需要一个有效的端口号。");
                    }

                    break;

                case "--root":
                    if (i + 1 >= args.Length)
                    {
                        return Fail("--root 需要一个目录路径。");
                    }

                    root = args[++i];
                    break;

                case "--open":
                    openBrowser = true;
                    break;

                case "--client":
                    clientPath = i + 1 < args.Length && !args[i + 1].StartsWith('-')
                        ? args[++i]
                        : Path.Combine(AppContext.BaseDirectory, ClientFileName);
                    break;

                default:
                    return Fail($"未知参数：{args[i]}");
            }
        }

        var paths = new AppPaths(root);
        using var interaction = new StaInteraction();
        var engine = new ScanEngine();
        var plugins = new PluginManager(paths, interaction, Log);
        plugins.Load();

        var dispatcher = new FunctionDispatcher(paths, engine, plugins, interaction, Log);
        var router = new SiteRouter(paths, plugins, dispatcher, Log);

        using var server = new WebServer(port, router.Route, Log);
        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            return Fail($"启动服务失败：{ex.Message}");
        }

        string url = $"http://127.0.0.1:{server.Port}/";
        Console.WriteLine("正在运行 Xdows Security Web 端");
        Console.WriteLine(url);
        Console.WriteLine($"插件数量：{plugins.Entries.Count}");
        Console.WriteLine("按 Ctrl+C 停止服务。");

        if (openBrowser)
        {
            OpenInDefaultBrowser(url);
        }

        if (clientPath != null)
        {
            StartClient(clientPath);
        }

        using var stop = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            stop.Set();
        };

        stop.Wait();
        return 0;
    }

    private static int Scan(string[] args)
    {
        if (args.Length == 0)
        {
            return Fail("用法：scan <文件路径> [--deep]");
        }

        string path = args[0];
        bool deep = args.Contains("--deep", StringComparer.Ordinal);

        var engine = new ScanEngine();
        string result = engine.Scan(path, deep);
        Console.WriteLine($"{path} => [{result}]");
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("Xdows Security 4.00 (C# / WinUI3 重构版)");
        return 0;
    }

    private static int PrintUsage()
    {
        Console.WriteLine("用法：");
        Console.WriteLine("  Xdows.Security.Host                      启动 Web 服务（默认端口 20000）");
        Console.WriteLine("  Xdows.Security.Host serve [选项]         启动 Web 服务");
        Console.WriteLine("      --port <端口>     监听端口，默认 20000");
        Console.WriteLine("      --root <目录>     程序根目录，默认为可执行文件所在目录");
        Console.WriteLine("      --open            启动后用默认浏览器打开界面");
        Console.WriteLine("      --client [路径]   启动 WinUI3 客户端");
        Console.WriteLine("  Xdows.Security.Host scan <文件> [--deep] 扫描单个文件");
        Console.WriteLine("  Xdows.Security.Host version              显示版本");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"未知命令：{command}");
        PrintUsage();
        return 1;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static void OpenInDefaultBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"打开浏览器失败：{ex.Message}");
        }
    }

    private static void StartClient(string path)
    {
        if (!File.Exists(path))
        {
            Log($"未找到客户端：{path}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"启动客户端失败：{ex.Message}");
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
