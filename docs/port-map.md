# 移植对照表

原始工程为易语言项目 `D:\Code\Xdows-Security-4`，`.e` 源文件通过 e2txt 1.2 转成文本后逐段对照移植。

## 源码对照

| 原始文件与位置 | 重构后的位置 |
| --- | --- |
| `Xdows-Security.e` / `程序集1._启动子程序` | `src/Xdows.Security.Host/Program.cs` |
| `窗口1` 的「服务器1」组件（端口 20000） | `src/Xdows.Security.Server/Web/WebServer.cs` |
| `窗口1.方法_首页`（路由） | `src/Xdows.Security.Server/SiteRouter.cs` |
| `窗口1.执行功能`、`转换DLL调用` | `src/Xdows.Security.Server/FunctionDispatcher.cs` |
| `窗口1` 插件目录遍历与 `PluginsData` / `PluginsInfo` 渲染 | `SiteRouter.RenderXdowsTools` + `Plugins/PluginManager.cs` |
| `窗口1.发送数据`（协议头、扩展名映射） | `src/Xdows.Security.Server/Web/HttpHeader.cs` |
| `窗口1.取系统主题` | 由网页端 `matchMedia` + `Client.postMessage` 承担，客户端 `MainWindow.ApplyTheme` |
| `窗口2`（扫描进度，415×250） | `src/Xdows.Security.Client/Views/ProgressWindow.xaml` |
| `窗口3`（插件解压进度，415×250） | 同上（同一窗口复用） |
| `Bin/Function/Scan.e` / `Scan`、`Scan_Check` | `src/Xdows.Security.Core/Scanning/ScanEngine.cs` |
| `Scan.e` / `启发扫描引擎.Heuristic_Scan` | `src/Xdows.Security.Core/Scanning/HeuristicScanner.cs` |
| `Scan.e` / `启发_检测壳子`、`启发_高级检测壳子`、`启发_高级外壳侦测` | `src/Xdows.Security.Core/Scanning/PackerSignatures.cs` |
| `Scan.e` / `GetPEInformation`（`ImageRvaToVa` + 指针读写） | `src/Xdows.Security.Core/Scanning/PeImage.cs` |
| `Scan.e` / `Dll公开函数名查杀`（内联汇编读导出表） | `PeImage.ReadExportNames` + `ScanEngine.DllScan` |
| `Scan.e` / `其它.GetFileMD5`（CryptoAPI 分块读取） | `ScanEngine.GetFileMd5` |
| `Scan.e` / `是否为伪装BAT`、`doc宏检查` | `HeuristicScanner.IsCamouflagedBat`、`DocMacroCheck` |
| `到字节集 (文本)`（GBK 字节） | `src/Xdows.Security.Core/Text/AnsiText.cs` |
| `Bin/Function/Window_Client.e`（ACF/CEF 外壳） | `src/Xdows.Security.Client`（WebView2） |
| `客户端功能回调.收到消息`（`ChangeTheme`） | `MainWindow.OnWebMessageReceived` / `ApplyTheme` |
| `UI_浏览器回调.创建完毕`（浏览器设置、载入网址） | `MainWindow.InitializeWebViewAsync` |
| `环境事件`（CEF 生命周期回调） | WebView2 的生命周期，无需回调对象 |
| 插件 DLL 导出 `SetUITitle` / `GetInfo` / `SetUIHtml` / 功能函数 | `IPlugin`、`IPluginContext`（`src/Xdows.Security.Plugin.Abstractions/IPlugin.cs`） |
| `Plugins/Files/Files/Main.e` | `src/Xdows.Security.Plugins.Files` |
| `Plugins/Process/Files/Main.e` | `src/Xdows.Security.Plugins.Process` |
| `Bin/Html/**` | `assets/web/**`（内容原样） |
| `Bin/Function/Clean.bat` | `assets/bin-function/Clean.bat`（字节原样） |
| `Build/Build.cmd` + `Build/Setup.iss` | `Build/publish.ps1` |

## 保持一致的契约

- 服务地址 `http://127.0.0.1:20000/`，请求路径与 `/Function/<功能名>?参数` 形式不变。
- 响应头保留原始模板的字段与 `Server: Xdows Security Web Server` 标识。
- `Function` 参数按出现顺序取值（`DelFiles` 依赖第 1、2 个参数），`|||` 分隔与 URL 解码位置不变。
- 插件目录 `Plugins\<插件名>\Files\Main.dll`，界面 HTML 由插件自身返回。
- 扫描结果字符串格式：`Xdows.local.code<分值><附加标记>`、`Xdows.local.DllVirus`、`Xdows.local.code60`。
- 启发式规则的分值、标记串、判定顺序逐条保留（含原始实现里的 `LikeAddHook`、`AddPack2` 等标记）。

## 有意调整的地方

1. **HTTP 响应头** 增加 `Content-Length` 与 `Connection: close`。原实现依赖断开连接让浏览器判定响应结束，在 HTTP/1.1 下不可靠；其余字段与原「协议头」常量一致。
2. **删除失败列表**（`DelFiles`）第三列改为文件路径。原实现此处引用了未初始化的局部数组 `Temp`，运行时会出错。
3. **两个插件页面**由 GBK 转为 UTF-8。原服务端以 `charset=UTF-8` 发送 GBK 文件，插件页中文显示为乱码；转换后文本内容不变。
4. **进度窗口**由独立的易语言窗口改为客户端内的 WinUI3 窗口：网页父框架调用 `BeginProgress` / `EndProgress`（Security 页的扫描、Xdows Tools 的导入插件）触发。插件解压由服务端一次性完成，没有真实百分比，进度条使用不确定样式。
5. **客户端外壳**由 CEF（ACF）换为 WebView2：用户代理追加 ` Client` 以保留网页里的 `getBrowserType()` 判断；`Client.postMessage(...)` 由注入脚本转到 WebView2 消息通道，网页调用代码不变。
6. **PE 解析**用结构化解析替代 `ImageRvaToVa` 与内联汇编，判定语义等价。
7. **文件/文件夹对话框与 TaskDialog** 由控制台宿主在独立 STA 线程上调用系统接口（`IFileOpenDialog`、`comctl32!TaskDialog`），属主窗口取控制台窗口。
8. **未实现的功能**保持未实现：快速扫描、全盘扫描、隔离信任、检查更新、云端与自定义引擎开关。

## 已知限制（沿用原始行为）

- 服务只监听 `127.0.0.1`，但请求路径未做规范化校验，与原始实现一致；如需限制访问请自行加校验。
- 扫描为同步处理，前端使用同步 `XMLHttpRequest`，扫描期间页面本身不响应（进度窗口由客户端进程绘制，不受影响）。
