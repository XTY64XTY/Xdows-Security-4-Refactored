using System.Security.Cryptography;

namespace Xdows.Security.Core.Scanning;

/// <summary>
/// 扫描入口，对应 <c>Scan.e</c> 导出的 <c>Scan (FilePath, DeepMode)</c>。
/// 返回值为病毒名（空字符串代表未发现威胁）。
/// </summary>
public sealed class ScanEngine
{
    /// <summary>本地黑名单 MD5（原引擎中的 “code60” 硬编码列表）。</summary>
    private static readonly HashSet<string> BlockedMd5 = new(StringComparer.Ordinal)
    {
        "1CD9B8C14E373112D055B86F14CE422F",
        "5F3D1F1E17AFC55FD804807831CB837C",
        "3F32A407163BF5963FC1555274FBA419",
        "6A4853CD0584DC90067E15AFB43C4962",
    };

    private readonly HeuristicScanner _heuristic = new();

    /// <summary>判定为威胁的最低得分。</summary>
    public const int ThreatScoreThreshold = 50;

    /// <summary>对应 <c>Scan (FilePath, DeepMode)</c>。</summary>
    public string Scan(string filePath, bool deepMode)
    {
        if (ScanCheck(filePath))
        {
            return string.Empty;
        }

        string md5 = GetFileMd5(filePath);
        if (BlockedMd5.Contains(md5))
        {
            return "Xdows.local.code60";
        }

        byte[] content;
        try
        {
            content = File.ReadAllBytes(filePath);
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return string.Empty;
        }

        if (content.Length == 0)
        {
            return string.Empty;
        }

        if (EndsWith(filePath, ".dll") && DllScan(content))
        {
            return "Xdows.local.DllVirus";
        }

        HeuristicResult result = _heuristic.Analyze(filePath, content, GetPeInformation(content), deepMode);
        if (result.Score >= ThreatScoreThreshold)
        {
            return "Xdows.local.code" + result.Score.ToString() + result.Extra;
        }

        return string.Empty;
    }

    /// <summary>对应 <c>Scan_Check</c>：返回真表示路径无效、无需扫描。</summary>
    public static bool ScanCheck(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return true;
        }

        if (filePath.Contains('{', StringComparison.Ordinal) || filePath.Contains('}', StringComparison.Ordinal))
        {
            return true;
        }

        return !File.Exists(filePath) && !Directory.Exists(filePath);
    }

    /// <summary>对应 <c>GetFileMD5</c>，返回大写十六进制串。</summary>
    public static string GetFileMd5(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 512, FileOptions.SequentialScan);
            return Convert.ToHexString(MD5.HashData(stream));
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    /// <summary>对应 <c>Dll_GetExportFuncInfo</c> + <c>Dll_Scan</c>：导出名含 Hook / Virus / Bypass 即判定。</summary>
    private static bool DllScan(byte[] content)
    {
        if (!PeImage.TryParse(content, out PeImage image))
        {
            return false;
        }

        foreach (string name in image.ReadExportNames())
        {
            if (name.Contains("Hook", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Virus", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Bypass", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 对应 <c>GetPEInformation</c>：把导入的 DLL 名与函数名拼成一段文本供规则匹配，
    /// 格式为 “DLL 名换行 … 空行 函数名换行 …”。
    /// </summary>
    private static string GetPeInformation(byte[] content)
    {
        if (!PeImage.TryParse(content, out PeImage image))
        {
            return string.Empty;
        }

        if (!image.TryReadImports(out List<string> modules, out List<string> functions))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        foreach (string module in modules)
        {
            builder.Append(module).Append('\n');
        }

        builder.Append('\n');
        foreach (string function in functions)
        {
            builder.Append(function).Append('\n');
        }

        return builder.ToString();
    }

    private static bool EndsWith(string value, string suffix) =>
        value.Length >= suffix.Length && value.EndsWith(suffix, StringComparison.Ordinal);
}
