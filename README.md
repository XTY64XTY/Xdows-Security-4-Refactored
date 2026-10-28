# Xdows Security 4（C# / WinUI3 重构版）

本仓库是易语言项目 [Xdows-Security-4](../Xdows-Security-4) 的 C# 重构版。原项目由三部分组成：
主程序内置 Web 服务并通过网页提供界面、CEF（ACF）客户端承载网页、独立的 `Scan.dll` 启发式扫描引擎。
重构后用 **C# 控制台宿主** 承担服务与引擎职责，用 **WinUI3 + WebView2 客户端** 承担界面外壳。

## 组成

| 项目 | 作用 | 原始对应 |
| --- | --- | --- |
| `Xdows.Security.Core` | 扫描引擎：PE 解析、启发式规则、MD5 黑名单 | `Bin/Function/Scan.e` |
| `Xdows.Security.Server` | 内置 Web 服务、路由、`/Function/*` 功能分发、插件加载 | `Xdows-Security.e`（窗口1 + 服务器组件） |
| `Xdows.Security.Plugin.Abstractions` | 插件契约 | 插件 DLL 的导出函数约定 |
| `Xdows.Security.Plugins.Files` / `.Process` | 内置插件「文件粉碎」「进程管理」 | `Plugins/Files`、`Plugins/Process` |
| `Xdows.Security.Host` | 控制台宿主：启动服务、命令行扫描 | 主程序启动逻辑 + `Scan.e` 调试入口 |
| `Xdows.Security.Client` | WinUI3 客户端：自定义标题栏 + WebView2 + 进度窗口 | `Bin/Function/Window_Client.e` |

## 目录结构

```
assets/web/           网页资源，输出到 Bin\Html（与原布局一致）
assets/bin-function/  Clean.bat 等附属脚本，输出到 Bin\Function
assets/plugins/       插件自带的界面 HTML，输出到 Plugins\<插件名>\Files
src/                  各 C# 项目
tests/                Core 的单元测试
Build/publish.ps1     发布脚本
```

## 构建

需要 .NET 10 SDK 与 Windows App SDK（由 NuGet 还原）。

```
dotnet build Xdows.Security.slnx -c Debug
```

解决方案统一使用 **x64** 平台，Debug 配置即可编译全部项目（含 WinUI3 客户端）。

## 运行

启动 Web 服务（默认 `http://127.0.0.1:20000/`）：

```
Xdows.Security.Host.exe serve
Xdows.Security.Host.exe serve --open          # 启动后用默认浏览器打开
Xdows.Security.Host.exe serve --client        # 同时启动 WinUI3 客户端（默认查找 Client\Xdows.Security.Client.exe）
```

命令行扫描（等价于原 `Scan.e` 的调试输出）：

```
Xdows.Security.Host.exe scan "C:\Windows\System32\notepad.exe" --deep
```

## 发布

```
powershell -ExecutionPolicy Bypass -File Build/publish.ps1
```

产物输出到 `publish\`：宿主与资源在根目录，WinUI3 客户端在 `publish\Client\`。
直接运行 `publish\Xdows.Security.Host.exe serve --client` 即可同时启动服务与客户端。

## 与原始实现的差异

移植过程中对少数无法直接等价的地方做了调整，逐条说明见 [docs/port-map.md](docs/port-map.md)。

## 许可证

本项目使用 [MIT](LICENSE) 许可证。
