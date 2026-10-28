using Xdows.Security.Core.Scanning;

namespace Xdows.Security.Core.Tests;

public sealed class ScanEngineTests
{
    private readonly ScanEngine _engine = new();

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\a{b}\test.exe")]
    [InlineData(@"C:\不存在的文件.exe")]
    public void 无效路径无需扫描(string path)
    {
        Assert.True(ScanEngine.ScanCheck(path));
    }

    [Fact]
    public void 存在的文件需要扫描()
    {
        string path = Path.GetTempFileName();
        try
        {
            Assert.False(ScanEngine.ScanCheck(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void 空文件返回空结果()
    {
        string path = Path.GetTempFileName();
        try
        {
            Assert.Equal(string.Empty, _engine.Scan(path, deepMode: false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void 文件不存在返回空结果()
    {
        Assert.Equal(string.Empty, _engine.Scan(@"C:\不存在的文件.exe", deepMode: false));
    }

    [Fact]
    public void MD5使用大写十六进制()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "abc");
            Assert.Equal("900150983CD24FB0D6963F7D28E17F72", ScanEngine.GetFileMd5(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
