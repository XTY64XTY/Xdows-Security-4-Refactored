namespace Xdows.Security.Server.Web;

/// <summary>请求参数，对应原代码里按 <c>&amp;</c> 与 <c>=</c> 手工切出来的参数表。</summary>
public readonly record struct QueryParameter(string Name, string Value);

/// <summary>一次 HTTP 请求的解析结果。</summary>
public sealed class HttpRequestInfo
{
    public HttpRequestInfo(string method, string rawTarget, string path, IReadOnlyList<QueryParameter> query, IReadOnlyDictionary<string, string> headers)
    {
        Method = method;
        RawTarget = rawTarget;
        Path = path;
        Query = query;
        Headers = headers;
    }

    public string Method { get; }

    /// <summary>原始请求目标，如 <c>/Function/DelFiles/?Files=a&amp;Vis=b</c>。</summary>
    public string RawTarget { get; }

    /// <summary>去掉前导 <c>/</c> 与查询串后的路径，如 <c>Function/DelFiles/</c>。</summary>
    public string Path { get; }

    public IReadOnlyList<QueryParameter> Query { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>按位置取参数值（0 基），越界返回空串。</summary>
    public string GetValue(int index) => index >= 0 && index < Query.Count ? Query[index].Value : string.Empty;

    /// <summary>按名称取参数值，缺失返回空串。</summary>
    public string GetValue(string name)
    {
        foreach (QueryParameter parameter in Query)
        {
            if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
            {
                return parameter.Value;
            }
        }

        return string.Empty;
    }

    /// <summary>把所有参数值按出现顺序收集为数组。</summary>
    public string[] GetValues()
    {
        var values = new string[Query.Count];
        for (int i = 0; i < Query.Count; i++)
        {
            values[i] = Query[i].Value;
        }

        return values;
    }

    /// <summary>解析请求目标：路径 + 参数表，语义等同原 <c>(.*?) /(.*?) HTTP</c> 加手工切分。</summary>
    public static HttpRequestInfo Parse(string method, string rawTarget, IReadOnlyDictionary<string, string> headers)
    {
        string target = rawTarget;
        int fragmentIndex = target.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            target = target[..fragmentIndex];
        }

        string path;
        string queryString = string.Empty;
        int queryIndex = target.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = target[..queryIndex];
            queryString = target[(queryIndex + 1)..];
        }
        else
        {
            path = target;
        }

        // 绝对形式（http://host/path）只取路径部分
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            int schemeEnd = path.IndexOf("//", StringComparison.Ordinal) + 2;
            int slash = path.IndexOf('/', schemeEnd);
            path = slash >= 0 ? path[slash..] : "/";
        }

        path = path.TrimStart('/');

        var query = new List<QueryParameter>();
        if (queryString.Length > 0)
        {
            foreach (string pair in queryString.Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                int equals = pair.IndexOf('=');
                if (equals < 0)
                {
                    query.Add(new QueryParameter(pair, string.Empty));
                }
                else
                {
                    query.Add(new QueryParameter(pair[..equals], pair[(equals + 1)..]));
                }
            }
        }

        return new HttpRequestInfo(method, rawTarget, path, query, headers);
    }
}
