# PUBG助手 — 智能时长暂停工具

自动监控 PUBG 游戏进程，在游戏退出时自动暂停雷神加速器，节省加速时长。

- **作者**: 巴德古斯
- **QQ**: 2994938720
- **GitHub**: [Bade-Gusi/leishen-pause](https://github.com/Bade-Gusi/leishen-pause)

## 功能特性

- 🎮 **自动监控** — 实时检测 PUBG 进程（TslGame.exe），3秒轮询
- ⏸ **一键暂停** — 游戏退出自动点击雷神加速器暂停按钮
- 🎯 **智能点击** — 三层降级策略：UI Automation → 自动坐标 → 手动坐标
- 📊 **数据统计** — 记录暂停次数、节省时长、运行次数
- 🖥 **系统托盘** — 关闭时最小化到托盘，后台运行
- 🔔 **全屏提醒** — 游戏退出时闪烁红色警告，自动消失
- ⌨ **快捷键** — Ctrl+Shift+P 快速捕获坐标
- 🚀 **开机自启** — 可选开机自动启动
- 🔄 **自动更新** — 点击检查更新自动下载 GitHub Releases 最新版

## 如何使用

1. 启动程序，点击「**开始监控**」
2. 打开「**雷神加速器**」并加速 PUBG
3. 点击「**捕获坐标**」，鼠标移到雷神加速器的「暂停」按钮上点击左键
4. 或者按 `Ctrl+Shift+P` 直接捕获当前鼠标位置的坐标
5. 当 PUBG 退出时，程序会自动暂停雷神加速器

## 构建发布

使用发布脚本（生成单个 exe）:
```bash
publish.bat
```

或手动执行:
```bash
dotnet publish -c Release -r win-x86 --self-contained true /p:PublishSingleFile=true
```

## 自动更新机制

程序通过 GitHub Releases API 检查更新。发布新版本时：
1. 在 GitHub 创建 Release，tag 命名为 `v2.x`（大于当前版本号）
2. 上传编译好的 `.exe` 或 `.zip` 作为 Release Asset
3. 用户点击「检查更新」按钮即可自动下载

## 技术栈

- .NET 8 + WPF
- Windows API (user32.dll)
- UI Automation (UIA)
- GitHub Releases API
- Hardcodet.NotifyIcon.Wpf

## 项目结构

```
leishen/
├── MainWindow.xaml / .cs      # 主界面 + 核心逻辑
├── CaptureWindow.xaml / .cs   # 坐标捕获窗口
├── ReminderWindow.xaml / .cs  # 全屏提醒窗口
├── App.xaml / .cs             # 应用入口
├── leishen.csproj             # 项目文件
└── README.md                  # 本文件

