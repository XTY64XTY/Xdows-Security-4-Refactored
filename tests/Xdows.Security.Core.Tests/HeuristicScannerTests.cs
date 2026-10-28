using Xdows.Security.Core.Scanning;

namespace Xdows.Security.Core.Tests;

public sealed class HeuristicScannerTests
{
    private readonly HeuristicScanner _scanner = new();

    [Theory]
    [InlineData("LoadLibraryA")]
    [InlineData("kernel32.dll")]
    [InlineData("C:\\windows\\system32")]
    [InlineData("*.exe")]
    public void 伪装BAT_命中特征串时判定为真(string fragment)
    {
        Assert.True(HeuristicScanner.IsCamouflagedBat(System.Text.Encoding.ASCII.GetBytes("echo " + fragment)));
    }

    [Fact]
    public void 伪装BAT_普通批处理判定为假()
    {
        Assert.False(HeuristicScanner.IsCamouflagedBat(System.Text.Encoding.ASCII.GetBytes("@echo off\r\ndel /q *.tmp\r\n")));
    }

    [Fact]
    public void DOC宏检查_带有Word标记时不判定为宏病毒()
    {
        byte[] content = System.Text.Encoding.GetEncoding(936).GetBytes("邢唷 Microsoft Word 文档");
        Assert.False(HeuristicScanner.DocMacroCheck(content));
    }

    [Fact]
    public void DOC宏检查_命中特征串时判定为真()
    {
        Assert.True(HeuristicScanner.DocMacroCheck(System.Text.Encoding.ASCII.GetBytes("This program cannot be run in DOS mode")));
    }

    [Fact]
    public void 伪装BAT_得分与附加标记与原始引擎一致()
    {
        var result = _scanner.Analyze(@"C:\test\sample.bat", System.Text.Encoding.ASCII.GetBytes("@echo off\r\nLoadLibraryA\r\n"), string.Empty, deepScan: false);

        // 10（伪装 BAT）+ 20（常用 DLL 缺失 → AddPack2）
        Assert.Equal(30, result.Score);
        Assert.Contains("CamouflageBat", result.Extra);
        Assert.Contains("AddPack2", result.Extra);
    }

    [Fact]
    public void 非PE文件在扩展名判断后退回零分()
    {
        var result = _scanner.Analyze(@"C:\test\sample.exe", System.Text.Encoding.ASCII.GetBytes("just plain text"), string.Empty, deepScan: false);

        Assert.Equal(0, result.Score);
        Assert.Equal(string.Empty, result.Extra);
    }

    [Fact]
    public void 缺少常用DLL与API时按加壳处理()
    {
        string content = "PE" + new string('A', 400);
        string peReference = "NTDLL.dll\nKernel32\nSHELL32.dll\nUSER32.dll\nGdi32\nLoadLibrary\nGetProcAddress\n";

        var result = _scanner.Analyze(@"C:\test\sample.exe", System.Text.Encoding.ASCII.GetBytes(content), peReference, deepScan: false);

        Assert.Equal(15, result.Score);
        Assert.Equal(string.Empty, result.Extra);
    }
}
