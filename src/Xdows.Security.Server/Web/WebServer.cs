using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Xdows.Security.Server.Web;

/// <summary>路由处理结果。</summary>
public sealed record RouteResult(byte[] Body, string ContentTypeExtension);

/// <summary>
/// 内置 Web 服务。原项目用易语言「服务器」组件监听 20000 端口，
/// 这里用 <see cref="TcpListener"/> 复刻同样的行为：一次请求一个连接、响应后断开。
/// </summary>
public sealed class WebServer : IDisposable
{
    private const int MaxHeaderBytes = 64 * 1024;

    private readonly IPEndPoint _endPoint;
    private readonly Func<HttpRequestInfo, RouteResult> _handler;
    private readonly Action<string> _log;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public WebServer(int port, Func<HttpRequestInfo, RouteResult> handler, Action<string>? log = null)
    {
        _endPoint = new IPEndPoint(IPAddress.Loopback, port);
        _handler = handler;
        _log = log ?? (_ => { });
    }

    /// <summary>实际监听端口（<paramref name="port"/> 传 0 时由系统分配）。</summary>
    public int Port => _listener?.LocalEndpoint is IPEndPoint endPoint ? endPoint.Port : _endPoint.Port;

    public void Start()
    {
        if (_listener != null)
        {
            throw new InvalidOperationException("服务已启动。");
        }

        _listener = new TcpListener(_endPoint);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoopAsync(_listener, _cts.Token));
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _log($"接受连接失败：{ex.Message}");
                continue;
            }

            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                client.NoDelay = true;
                using NetworkStream stream = client.GetStream();
                string? requestText = await ReadHeadersAsync(stream, cancellationToken).ConfigureAwait(false);
                if (requestText == null)
                {
                    return;
                }

                HttpRequestInfo request = ParseRequest(requestText);

                if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, [], "html", cancellationToken).ConfigureAwait(false);
                    return;
                }

                RouteResult result;
                try
                {
                    result = _handler(request);
                }
                catch (Exception ex)
                {
                    _log($"处理请求 {request.RawTarget} 时出错：{ex}");
                    result = new RouteResult(Encoding.UTF8.GetBytes(string.Empty), "html");
                }

                await WriteResponseAsync(stream, result.Body, result.ContentTypeExtension, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // 客户端提前断开，忽略。
            }
            catch (SocketException)
            {
                // 同上。
            }
            catch (ObjectDisposedException)
            {
                // 服务已停止。
            }
        }
    }

    private static async Task<string?> ReadHeadersAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var builder = new MemoryStream();
        while (builder.Length < MaxHeaderBytes)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            builder.Write(buffer, 0, read);
            if (EndsWithHeaderTerminator(builder.GetBuffer(), (int)builder.Length))
            {
                break;
            }
        }

        if (builder.Length == 0)
        {
            return null;
        }

        return Encoding.Latin1.GetString(builder.GetBuffer(), 0, (int)builder.Length);
    }

    private static bool EndsWithHeaderTerminator(byte[] data, int length)
    {
        if (length >= 4 && data[length - 4] == '\r' && data[length - 3] == '\n' && data[length - 2] == '\r' && data[length - 1] == '\n')
        {
            return true;
        }

        return length >= 2 && data[length - 2] == '\n' && data[length - 1] == '\n';
    }

    private static HttpRequestInfo ParseRequest(string requestText)
    {
        string[] lines = requestText.Split(["\r\n", "\n"], StringSplitOptions.None);
        string requestLine = lines.Length > 0 ? lines[0] : string.Empty;
        string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string method = parts.Length > 0 ? parts[0] : "GET";
        string target = parts.Length > 1 ? parts[1] : "/";

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0)
            {
                break;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        return HttpRequestInfo.Parse(method, target, headers);
    }

    private static async Task WriteResponseAsync(NetworkStream stream, byte[] body, string extension, CancellationToken cancellationToken)
    {
        byte[] header = HttpHeader.Build(extension, body.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        if (body.Length > 0)
        {
            await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        }

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }
}
