# LockKeyFlyout

从 FluentFlyout 的“键盘锁定键浮窗”功能独立实现的 Windows 常驻程序。键盘监听与托盘保持原生 Win32；浮窗使用 WPF 的 DirectWrite/XAML 合成渲染，不使用 WinUI、WebView 或常驻轮询。

## 特性

- 显示 Caps Lock、Num Lock、Scroll Lock 与 Insert 按键状态。
- 复刻 FluentFlyout 的 160×50 DIP 亚克力浮窗、矢量锁图标与圆角状态条。
- 浮窗动画使用 WPF Storyboard 的保留模式合成：从任务栏方向上浮并轻微回弹，消失时连续下沉淡出；没有逐帧重绘计时器。
- WPF DirectWrite 文本与矢量 Path 图标，避免 GDI 位图文字和圆角的锯齿。
- 空闲时由 Windows 消息循环驱动；仅浮窗可见期间参与桌面合成。
- 浮窗隐藏后关闭 Acrylic 合成，降低后台 GPU 开销。
- 使用最高权限登录计划任务，在管理员窗口获得焦点时仍可监听锁定键。
- 支持多显示器、显示时间、粗体、动画和系统托盘设置。
- 传统 Win32 安装程序，不使用 UWP、MSIX 或 GDK 封装。

## 使用

- 启动后常驻通知区域，按 Caps Lock、Num Lock、Scroll Lock 或 Insert 显示状态。
- 双击托盘图标打开设置；右键可暂停、设置或退出。
- 配置保存在 `%LOCALAPPDATA%\LockKeyFlyout\settings.ini`。
- 单实例运行；程序以管理员权限启动，因此在聚焦管理员窗口时也能收到锁定键事件。
- “登录时启动”使用 Windows 任务计划程序标准接口注册最高权限任务；允许电池供电时启动、不会在切换到电池时停止，并且没有 72 小时运行上限。注册表 Run 项无法在不弹 UAC 的情况下提供管理员权限，因此不使用。
- 交付物是普通 Windows `.exe`，不使用 UWP、MSIX、GDK、WinUI 或 WebView 封装。

## 构建

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

输出位于 `bin\Release\net8.0-windows\win-x64\publish`。项目使用 WPF 的 Windows Desktop Runtime；`--self-contained true` 会将其打包进单文件，普通 `dotnet build -c Release` 则使用已安装的 .NET 8 Desktop Runtime。

安装包使用 Inno Setup 6 编译：

```powershell
iscc installer.iss
```

## 图标

应用图标由项目提供的高分辨率蓝紫渐变锁图制作。ICO 内含 16、20、24、32、48、64、128 和 256 px 八种独立采样尺寸，并保留透明 Alpha 抗锯齿边缘。

本项目参考 FluentFlyout 的功能行为并遵循 GPL-3.0-or-later。
