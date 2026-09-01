# LockKeyFlyout

从 FluentFlyout 的“键盘锁定键浮窗”功能独立实现的轻量 Windows 常驻程序。纯 Win32 窗口与 GDI 绘制，不使用 WinUI、WPF、WebView 或常驻轮询。

## 特性

- 显示 Caps Lock、Num Lock、Scroll Lock 与 Insert 按键状态。
- 复刻 FluentFlyout 的 160×50 DIP 亚克力浮窗、矢量锁图标与圆角状态条。
- NativeAOT 单文件原生程序，空闲时由 Win32 消息循环驱动，无轮询线程。
- 浮窗隐藏后关闭 Acrylic 合成，降低后台 GPU 开销。
- 使用最高权限登录计划任务，在管理员窗口获得焦点时仍可监听锁定键。
- 支持多显示器、显示时间、粗体、动画和系统托盘设置。
- 传统 Win32 安装程序，不使用 UWP、MSIX 或 GDK 封装。

## 使用

- 启动后常驻通知区域，按 Caps Lock、Num Lock、Scroll Lock 或 Insert 显示状态。
- 双击托盘图标打开设置；右键可暂停、设置或退出。
- 配置保存在 `%LOCALAPPDATA%\LockKeyFlyout\settings.ini`。
- 单实例运行；程序以管理员权限启动，因此在聚焦管理员窗口时也能收到锁定键事件。
- “登录时启动”使用最高权限计划任务，不使用权限不足的注册表 Run 项。
- 交付物是普通 Win32 `.exe`，不使用 UWP、MSIX、GDK、WinUI、WPF 或 WebView 封装。

## 构建

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

输出位于 `bin\Release\net8.0-windows\win-x64\publish`。NativeAOT 构建需要 Visual Studio C++ Build Tools；若机器未安装，可暂时使用 `dotnet build -c Release` 生成依赖已安装 .NET 8 Desktop Runtime 的版本。

安装包使用 Inno Setup 6 编译：

```powershell
iscc installer.iss
```

## 图标

应用图标采用 Microsoft Fluent UI System Icons 的 `Lock Closed 48 Color`，依据 MIT License 使用，详见 `THIRD-PARTY-NOTICES.md`。

本项目参考 FluentFlyout 的功能行为并遵循 GPL-3.0-or-later。
