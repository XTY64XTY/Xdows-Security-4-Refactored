namespace Xdows.Security.Core.Scanning;

/// <summary>启发式扫描结果：得分 + 附加标记串，对应原引擎的 <c>返回值 + 返回附加数据</c>。</summary>
public sealed record HeuristicResult(int Score, string Extra);

/// <summary>
/// 启发扫描引擎，逐条移植自 <c>Scan.e</c> 的 <c>启发扫描引擎</c> 程序集。
/// 分值、标记串、判定顺序均与原始实现保持一致。
/// </summary>
public sealed class HeuristicScanner
{
    /// <summary>等价于 <c>启发_函数检测 (PE引用, 被测函数) ＞ 0</c>。</summary>
    private static bool Match(string peReference, string name) => peReference.IndexOf(name, StringComparison.OrdinalIgnoreCase) > 0;

    /// <summary>等价于 <c>启发_文件检测 (文件内容, 寻找内容)</c>。</summary>
    private static bool ContainsText(ReadOnlySpan<byte> content, string value) => ByteSearch.ContainsText(content, value);

    public HeuristicResult Analyze(string filePath, byte[] content, string peReference, bool deepScan)
    {
        int score = 0;
        string extra = string.Empty;

        // --- 可疑格式分析 ---
        if (HasSuffix(filePath, ".fne"))
        {
            score -= 20;
        }

        if (HasSuffix(filePath, ".exe") && filePath.Contains(@"\Start Menu\Programs\Startup\", StringComparison.Ordinal))
        {
            int attributes = ReadAttributes(filePath);
            if (attributes is 2 or 3 or 6 or 18 or 34)
            {
                score += 20;
            }
        }

        if (HasSuffix(filePath, ".exe"))
        {
            string name = GetFileName(filePath).Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
            string surplusSuffix = LastChars(name, 4);
            if (surplusSuffix is ".jpg" or ".bmp" or ".gif" or ".avi" or ".wmv" or ".rar" or ".zip")
            {
                score += 10;
            }
        }

        if (HasSuffix(filePath, ".doc"))
        {
            if (DocMacroCheck(content))
            {
                score += 10;
                extra += " DocVirus";
            }
            else
            {
                return new HeuristicResult(0, string.Empty);
            }
        }

        if (HasSuffix(filePath, ".bat") || HasSuffix(filePath, ".cmd"))
        {
            // 伪装 BAT：内容里出现 PE/系统 API 特征
            if (IsCamouflagedBat(content))
            {
                score += 10;
                extra += " CamouflageBat";
            }
            else
            {
                return new HeuristicResult(0, string.Empty);
            }
        }
        else if (!ContainsText(content, "PE"))
        {
            return new HeuristicResult(0, string.Empty);
        }

        // --- 可疑行为分析 ---
        if (Match(peReference, "GetOpenFileNameA") || Match(peReference, "GetOpenFileNameW") ||
            Match(peReference, "GetSaveFileNameA") || Match(peReference, "GetSaveFileNameW"))
        {
            score -= 20;
        }

        if (Match(peReference, "LoadLibrary"))
        {
            score += Match(peReference, "GetProcAddress") ? 15 : 10;
        }

        if (Match(peReference, "SetFileAttributes") && Match(peReference, "FILE_ATTRIBUTE_HIDDEN"))
        {
            score += 20;
        }

        if (Match(peReference, "SHFormatDrive"))
        {
            score += 20;
        }

        if (Match(peReference, "RtlAdjustPrivilege"))
        {
            score += 20;
        }

        if (Match(peReference, "HideCurrentProcess"))
        {
            score += 20;
        }

        if (Match(peReference, "SetFilePointer"))
        {
            score += 15;
            if (deepScan && Match(peReference, "WriteFile") && ContainsText(content, "physicaldrive0"))
            {
                score += 5;
                extra += " ChangeMBR";
            }
        }

        if (Match(peReference, "CreateService") && Match(peReference, "StartService"))
        {
            extra += " UseService";
            score += 15;
        }

        if (Match(peReference, "CopyFile") && Match(peReference, "CreateDirectory") &&
            Match(peReference, "DeleteFile") && Match(peReference, "GetFullPathName"))
        {
            score += 5;
        }

        if (Match(peReference, "CreateObject") &&
            (Match(peReference, "Scriptlet.TypeLib") || Match(peReference, "Shell.Application") ||
             Match(peReference, "Scripting.FileSystemObject")))
        {
            score += 15;
        }

        if (Match(peReference, "GetDlgItemInt") || Match(peReference, "GetDlgItemText") || Match(peReference, "GetDlgItemTextA"))
        {
            score += 15;
        }

        if (Match(peReference, "InternetReadFile") || Match(peReference, "FtpGetFile") || Match(peReference, "URLDownloadToFile"))
        {
            if (Match(peReference, "WinExec") && Match(peReference, "RegCreateKey"))
            {
                score += 20;
                if (Match(peReference, "MoveFile") || Match(peReference, "CopyFile"))
                {
                    score += 10;
                }
            }
        }

        if (Match(peReference, "CallNextHookEx") || Match(peReference, "SetWindowsHook"))
        {
            extra += " AddHook";
            score += 20;
        }
        else if (Match(peReference, "Hook"))
        {
            extra += " LikeAddHook";
            score += 15;
        }

        if (Match(peReference, "WriteConsole"))
        {
            score += 5;
        }

        if (Match(peReference, "GetModuleFileName") || Match(peReference, "GetModuleHandle"))
        {
            score += 20;
        }

        if (Match(peReference, "WNetAddConnection"))
        {
            score += 15;
        }

        if (Match(peReference, "CopyScreen"))
        {
            score += 15;
        }

        if (Match(peReference, "ExitWindows"))
        {
            score += 5;
        }

        if (Match(peReference, "URLDownloadToFile"))
        {
            score += 15;
        }

        if (Match(peReference, "URLDownloadToCacheFile"))
        {
            score -= 15;
        }

        if (Match(peReference, "mouse_event") || Match(peReference, "keydb_event"))
        {
            score += 15;
        }

        if (Match(peReference, "SetPriorityClass"))
        {
            score += 15;
        }

        if (Match(peReference, "EnumAudioEndpoints"))
        {
            extra += " LikeSandboxBypass";
            score += 15;
        }

        // --- 壳检测：命名壳优先，其次高级外壳侦测，最后高级签名 ---
        string packer = PackerSignatures.DetectNamedPacker(content);
        if (packer != "-1")
        {
            extra += " AddPackWith" + packer;
            score += 15;
        }
        else if (DetectAdvancedShell(content, peReference))
        {
            extra += " AddPack2";
            score += 20;
        }
        else if (PackerSignatures.DetectAdvancedPacker(content))
        {
            extra += " AddPack3";
            score += 25;
        }

        // --- 深度扫描附加检测 ---
        if (deepScan)
        {
            if (ContainsText(content, ".sys"))
            {
                extra += " UseDriver";
                score += 10;
            }

            if (ContainsText(content, "Virtual"))
            {
                score += 20;
            }

            if (ContainsText(content, "BlackMoon"))
            {
                extra += " BlackMoon";
                score += 15;
            }

            if (ContainsText(content, "PYAS") || ContainsText(content, "wsctrlsvc") || ContainsText(content, "ESET") ||
                ContainsText(content, "zhudongfangyu") || ContainsText(content, "avp") || ContainsText(content, "avconsol") ||
                ContainsText(content, "ASWSCAN") || ContainsText(content, "KWatch") || ContainsText(content, "QQPCTray"))
            {
                extra += " AVKiller";
                score += 20;
            }

            if (ContainsText(content, "DelegateExecute") || ContainsText(content, "fodhelper.exe"))
            {
                extra += " UACBypass";
                score += 20;
            }

            if (ContainsText(content, "sandboxie") || ContainsText(content, "vmware - tray") || ContainsText(content, "Detonate") ||
                ContainsText(content, "Vmware") || ContainsText(content, "VMWARE") || ContainsText(content, "Sandbox") ||
                ContainsText(content, "SANDBOX"))
            {
                extra += " SandboxBypass";
                score += 20;
            }
        }

        return new HeuristicResult(score, extra);
    }

    /// <summary>对应 <c>是否为伪装BAT</c>。</summary>
    public static bool IsCamouflagedBat(ReadOnlySpan<byte> content)
    {
        if (ByteSearch.ContainsText(content, "program cannot be run in"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "LoadLibraryA"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "Win32"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "kernel32.dll"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "ntdll.dll"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "GetProcAddress"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, @"C:\windows\"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "*.exe"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "Shutdown"))
        {
            return true;
        }

        return false;
    }

    /// <summary>对应 <c>doc宏检查</c>。</summary>
    public static bool DocMacroCheck(ReadOnlySpan<byte> content)
    {
        if (ByteSearch.ContainsText(content, "This program cannot be run"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "LoadLibraryA"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "RichN"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "kernel32.dll"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "Win32"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "GetProcAddress"))
        {
            return true;
        }

        if (ByteSearch.ContainsText(content, "邢") && ByteSearch.ContainsText(content, "唷"))
        {
            if (ByteSearch.ContainsText(content, "Microsoft Office Word"))
            {
                return false;
            }

            if (ByteSearch.ContainsText(content, "Microsoft Word"))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>对应 <c>启发_高级外壳侦测</c>：常用 DLL / API 都缺失时认为被加壳。</summary>
    private static bool DetectAdvancedShell(byte[] content, string peReference)
    {
        _ = content;
        int dllHits = 6;
        int apiHits = 4;

        if (!Match(peReference, "NTDLL.dll"))
        {
            dllHits--;
        }

        if (!Match(peReference, "Kernel32"))
        {
            dllHits--;
        }

        if (!Match(peReference, "SHELL32.dll"))
        {
            dllHits--;
        }

        if (!Match(peReference, "USER32.dll"))
        {
            dllHits--;
        }

        if (!Match(peReference, "Gdi32"))
        {
            dllHits--;
        }

        if (dllHits < 2)
        {
            return true;
        }

        if (!Match(peReference, "LoadLibrary"))
        {
            apiHits--;
        }

        if (!Match(peReference, "Message"))
        {
            apiHits--;
        }

        if (!Match(peReference, "ReadFile"))
        {
            apiHits--;
        }

        if (!Match(peReference, "Virtual"))
        {
            apiHits--;
        }

        return apiHits < 1;
    }

    private static bool HasSuffix(string value, string suffix) =>
        value.Length >= suffix.Length && value.EndsWith(suffix, StringComparison.Ordinal);

    private static string LastChars(string value, int count) =>
        value.Length <= count ? value : value[^count..];

    /// <summary>对应 <c>FilePathToName</c>：取路径最后一段。</summary>
    private static string GetFileName(string path)
    {
        int index = path.LastIndexOf('\\');
        return index < 0 ? path : path[(index + 1)..];
    }

    /// <summary>对应 <c>取文件属性</c>，取值为 #只读=1 / #隐藏=2 / #系统=4 / #子目录=16 / #存档=32。</summary>
    private static int ReadAttributes(string path)
    {
        try
        {
            return (int)System.IO.File.GetAttributes(path);
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
