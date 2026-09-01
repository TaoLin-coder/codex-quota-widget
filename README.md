# Codex Quota Widget for Windows 11

一个贴在 Windows 11 主任务栏左侧的 Codex 用量看板。当前版本：**v0.3（2026-09-01）**。

## 功能

- 两行显示 GPT 与 GPT-5.3-Codex-Spark 的一周剩余额度。
- GPT 或 Spark 的周剩余额度不高于 20% 时显示红点。
- Spark 的 5 小时剩余额度低于 10% 时显示黄点，提示该模型可能暂时缓慢或不可用。
- 单击任务栏组件打开详细看板。
- 每 60 秒自动刷新。
- 右键可刷新、开机启动、隐藏托盘图标或退出。
- 任务栏隐藏时组件跟随隐藏。
- 支持 Per-Monitor V2 DPI，连接、断开显示器或改变缩放后自动适配。
- 按接口返回的窗口秒数识别短期和周额度，不存在的窗口显示为 `--`。

## 直接运行

下载 [`dist/CodexQuotaWidget.exe`](dist/CodexQuotaWidget.exe)，双击运行。程序不需要管理员权限。

## 登录信息

程序依次从以下位置读取 Codex 登录信息：

1. `%CODEX_HOME%\auth.json`
2. `%USERPROFILE%\.codex\auth.json`
3. 当前目录下的 `.codex\auth.json`

访问令牌只在内存中使用，不会写入日志或其他配置文件。

## 构建

在 Windows PowerShell 中运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

构建脚本使用 Windows 自带的 .NET Framework 4.x C# 编译器，不需要额外安装 NuGet 包。生成文件位于 `dist\CodexQuotaWidget.exe`。

## 技术说明

- 用量数据来自 `https://chatgpt.com/backend-api/wham/usage`。
- GPT-5.3-Codex-Spark 使用接口返回的 `additional_rate_limits` 独立窗口。
- 该接口不是公开稳定 API，字段未来可能发生变化。
- Win11 没有正式开放向任务栏空白区嵌入任意控件的接口。本项目通过 `SetParent` 将窗口附加到 `Shell_TrayWnd`，属于实验性实现，Windows 更新后可能需要适配。

## 隐私

仓库不包含任何 Codex 登录令牌、账户信息或本机 `auth.json`。
