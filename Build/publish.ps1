param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "publish"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host "发布控制台宿主到 $output"
dotnet publish (Join-Path $root "src\Xdows.Security.Host\Xdows.Security.Host.csproj") `
    -c $Configuration -r win-x64 --self-contained true -o $output

Write-Host "发布 WinUI3 客户端到 $output\Client"
dotnet publish (Join-Path $root "src\Xdows.Security.Client\Xdows.Security.Client.csproj") `
    -c $Configuration -o (Join-Path $output "Client")

Write-Host "完成：$output"
Write-Host "运行 $output\Xdows.Security.Host.exe serve --client 即可同时启动服务与客户端。"
