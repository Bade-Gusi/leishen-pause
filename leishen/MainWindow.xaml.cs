using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Media.Animation;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Text;
using Application = System.Windows.Application;

namespace leishen
{
    public partial class MainWindow : Window
    {
        // ======================== Win32 API ========================
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // ======================== 结构体 ========================
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public MOUSEINPUT mi; }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        // ======================== 热键 ========================
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_P = 0x50;
        private const int WM_HOTKEY = 0x0312;

        // ======================== 字段 ========================
        private DispatcherTimer? _timer;
        private bool _isGameRunning = false;
        private bool _isMonitoring = false;

        private string _configPath = "";
        private int _manualX = 160;
        private int _manualY = 360;

        private const string REG_RUN_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "PUBGMonitor";

        private ObservableCollection<string> _logMessages = new ObservableCollection<string>();

        // 统计
        private int _todayPauseCount = 0;
        private int _totalPauseCount = 0;
        private int _sessionCount = 0;
        private DateTime _lastGameExitTime;
        private bool _isDarkMode = true;

        // 托盘
        private WindowInteropHelper? _windowHelper;

        // ======================== 构造函数 ========================
        public MainWindow()
        {
            InitializeComponent();
            // 修复XAML中可能的拼写问题
            FixXamlIssues();

            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coord_config.json");
            LogList.ItemsSource = _logMessages;
            LoadConfig();
            UpdateCoordDisplay();
            CheckAutoStartStatus();
            LoadStatistics();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            this.StateChanged += MainWindow_StateChanged;
            AddLog("程序启动 · v2.0");
            AnimateCardsIn();
        }

        /// <summary>
        /// 修复XAML中已知的渲染问题
        /// </summary>
        private void FixXamlIssues()
        {
            // 无运行时修复需要
        }

        // ======================== 日志 ========================
        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logMessages.Count > 100) _logMessages.RemoveAt(0);
                LogList.ScrollIntoView(_logMessages.LastOrDefault());
            });
        }

        // ======================== 窗口加载 ========================
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHelper = new WindowInteropHelper(this);
            bool registered = RegisterHotKey(_windowHelper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_P);
            if (!registered)
                AddLog("热键 Ctrl+Shift+P 注册失败，可能被占用");

            HwndSource source = HwndSource.FromHwnd(_windowHelper.Handle);
            if (source != null)
                source.AddHook(HwndHook);

            UpdateStatisticsDisplay();

            // 自动开始监控
            StartMonitoring();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (_windowHelper != null)
                UnregisterHotKey(_windowHelper.Handle, HOTKEY_ID);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                AddLog("最小化到系统托盘");
            }
        }

        // ======================== 热键钩子 ========================
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotKeyPressed();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void HotKeyPressed()
        {
            Dispatcher.Invoke(() => CaptureCurrentMousePosition());
        }

        // ======================== UI 动画 ========================
        private void AnimateCardsIn()
        {
            var cards = new[] { CardCoord, CardOptions, CardLog };
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                card.Opacity = 0;
                card.RenderTransform = new TranslateTransform(0, 25);
                var sb = Resources["CardFadeIn"] as Storyboard;
                if (sb != null)
                {
                    var story = sb.Clone();
                    story.BeginTime = TimeSpan.FromMilliseconds(150 + i * 100);
                    story.Begin(card);
                }
            }
        }

        private void StartSpinner()
        {
            SpinnerBorder.Visibility = Visibility.Visible;
            var spin = Resources["SpinAnimation"] as Storyboard;
            spin?.Begin(Spinner);
        }

        private void StopSpinner()
        {
            var spin = Resources["SpinAnimation"] as Storyboard;
            spin?.Stop(Spinner);
            SpinnerBorder.Visibility = Visibility.Collapsed;
        }

        // ======================== 状态更新 ========================
        private void SetStatus(string text, string colorHex, string icon, bool pulseRing = false)
        {
            StatusText.Text = text;
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            StatusText.Foreground = new SolidColorBrush(c);
            StatusIcon.Text = icon;

            var ringColor = new SolidColorBrush(c);
            StatusRing.Stroke = ringColor;
            StatusGlow.Fill = ringColor;
            StatusDot.Fill = ringColor;

            if (pulseRing)
            {
                var pulse = Resources["PulseGlow"] as Storyboard;
                pulse?.Begin(StatusRing);
                pulse?.Begin(StatusGlow);
            }
            else
            {
                var pulse = Resources["PulseGlow"] as Storyboard;
                pulse?.Stop(StatusRing);
                pulse?.Stop(StatusGlow);
                StatusRing.Opacity = 0.3;
                StatusGlow.Opacity = 0.12;
            }
        }

        // ======================== 监控 ========================
        private void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += async (s, ev) => await CheckProcessAsync();
            _timer.Start();

            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            BtnStop.Background = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
            BtnStop.Foreground = Brushes.White;

            SetStatus("扫描中", "#00E5FF", "⟳");
            StartSpinner();
            GameNameText.Text = "正在检测 PUBG 进程...";
            AddLog("▶ 监控已启动");
        }

        private void StopMonitor()
        {
            _isMonitoring = false;
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            BtnStop.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44));
            BtnStop.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0x88));

            SetStatus("未运行", "#00C853", "⏸");
            StopSpinner();
            GameNameText.Text = "监控已停止";
            AddLog("⏹ 监控已停止");
        }

        private async Task CheckProcessAsync()
        {
            bool found = await Task.Run(() =>
            {
                try { return Process.GetProcessesByName("TslGame").Any(); }
                catch { return false; }
            });

            Dispatcher.Invoke(() =>
            {
                if (found)
                {
                    if (!_isGameRunning)
                    {
                        _isGameRunning = true;
                        StopSpinner();
                        SetStatus("游戏中", "#FF1744", "▶", true);
                        GameNameText.Text = "正在为你保驾护航 💪";
                        GameNameText.Foreground = Brushes.OrangeRed;
                        AddLog("🎮 检测到 PUBG 启动");
                        _sessionCount++;
                        SaveStatistics();
                    }
                }
                else
                {
                    if (_isGameRunning)
                    {
                        _isGameRunning = false;
                        _lastGameExitTime = DateTime.Now;
                        _todayPauseCount++;
                        _totalPauseCount++;

                        SetStatus("已暂停", "#00C853", "✓");
                        StopSpinner();
                        GameNameText.Text = "检测到游戏关闭 · 执行暂停 ✓";
                        GameNameText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0x53));

                        AddLog($"🛑 PUBG 已关闭，执行暂停操作 (今日第{_todayPauseCount}次)");

                        // 异步执行暂停（不阻塞UI）
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(500);
                            DoPause();
                            await Task.Delay(300);
                            Dispatcher.Invoke(() =>
                            {
                                UpdateStatisticsDisplay();
                                SaveStatistics();
                                if (ChkShowReminder.IsChecked == true)
                                    ShowReminderWindow();
                            });
                        });
                    }
                }
            });
        }

        // ======================== 暂停操作 ========================
        private void DoPause()
        {
            string[] titles = { "雷神加速器", "雷神加速器 - 加速游戏", "LeiShen", "雷神" };
            IntPtr hwnd = IntPtr.Zero;
            foreach (string title in titles)
            {
                hwnd = FindWindow(null, title);
                if (hwnd != IntPtr.Zero) break;
            }

            if (hwnd == IntPtr.Zero)
            {
                AddLog("❌ 未找到雷神加速器窗口");
                return;
            }

            AddLog($"✅ 找到雷神加速器窗口 (句柄: {hwnd})");

            // 策略1：UI Automation 直接调用
            bool success = TryAutoClick(hwnd);
            if (!success)
            {
                AddLog("自动识别失败，使用预设坐标模拟点击");
                ManualClick(hwnd);
            }
        }

        private bool TryAutoClick(IntPtr hwnd)
        {
            try
            {
                AutomationElement root = AutomationElement.FromHandle(hwnd);
                if (root == null) return false;

                PropertyCondition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                AutomationElementCollection buttons = root.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement btn in buttons)
                {
                    string name = btn.Current.Name;
                    string automationId = btn.Current.AutomationId;
                    if ((!string.IsNullOrEmpty(name) && (name.Contains("暂停") || name.Contains("Pause"))) ||
                        (!string.IsNullOrEmpty(automationId) && (automationId.Contains("pause") || automationId.Contains("Pause"))))
                    {
                        AddLog($"📌 找到暂停按钮：Name={name}");

                        if (btn.TryGetCurrentPattern(InvokePattern.Pattern, out object patternObj))
                        {
                            ((InvokePattern)patternObj).Invoke();
                            AddLog("✅ InvokePattern 调用成功 ✓");
                            return true;
                        }

                        // Fallback: 使用按钮包围盒自动计算坐标
                        Rect rect = btn.Current.BoundingRectangle;
                        if (!rect.IsEmpty && GetWindowRect(hwnd, out RECT windowRect))
                        {
                            int rx = (int)(rect.Left - windowRect.Left + rect.Width / 2);
                            int ry = (int)(rect.Top - windowRect.Top + rect.Height / 2);
                            AddLog($"📐 自动识别坐标：({rx}, {ry})");
                            return SimulateMouseClick(hwnd, rx, ry);
                        }
                    }
                }
                AddLog("未找到匹配的暂停按钮");
                return false;
            }
            catch (Exception ex)
            {
                AddLog($"自动识别异常：{ex.Message}");
                return false;
            }
        }

        private void ManualClick(IntPtr hwnd)
        {
            if (!GetWindowRect(hwnd, out RECT rect))
            {
                AddLog("无法获取窗口矩形");
                return;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (_manualX < 0 || _manualX > width || _manualY < 0 || _manualY > height)
            {
                AddLog($"⚠ 坐标({_manualX},{_manualY})不在窗口内，自动校准到中心");
                _manualX = width / 2;
                _manualY = height / 2;
                SaveConfig();
                UpdateCoordDisplay();
            }
            SimulateMouseClick(hwnd, _manualX, _manualY);
        }

        private bool SimulateMouseClick(IntPtr hwnd, int relativeX, int relativeY)
        {
            AddLog($"🖱 模拟点击：相对坐标({relativeX}, {relativeY})");

            if (!GetWindowRect(hwnd, out RECT windowRect))
            {
                AddLog("无法获取窗口矩形");
                return false;
            }

            int screenX = windowRect.Left + relativeX;
            int screenY = windowRect.Top + relativeY;

            GetCursorPos(out POINT oldPos);

            // 激活窗口（最多3次）
            for (int i = 0; i < 3; i++)
            {
                ShowWindow(hwnd, SW_SHOW);
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
                System.Threading.Thread.Sleep(150);
                if (GetForegroundWindow() == hwnd) break;
            }

            SetCursorPos(screenX, screenY);
            System.Threading.Thread.Sleep(100);

            // SendInput 发送点击（最多3次）
            bool success = false;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                INPUT[] inputs = new INPUT[2];
                inputs[0].type = INPUT_MOUSE;
                inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
                inputs[1].type = INPUT_MOUSE;
                inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

                uint result = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
                if (result == 2)
                {
                    success = true;
                    AddLog($"✅ 点击成功 (尝试 {attempt + 1})");
                    break;
                }
                System.Threading.Thread.Sleep(50);
            }

            System.Threading.Thread.Sleep(100);
            SetCursorPos(oldPos.X, oldPos.Y);
            return success;
        }

        // ======================== 提醒窗口 ========================
        private void ShowReminderWindow()
        {
            try
            {
                var reminder = new ReminderWindow(_todayPauseCount);
                reminder.Show();
                AddLog("📢 显示全屏提醒窗口");
            }
            catch (Exception ex)
            {
                AddLog($"提醒窗口异常：{ex.Message}");
            }
        }

        // ======================== 坐标捕获 ========================
        private void StartMouseCapture()
        {
            WindowState = WindowState.Minimized;
            AddLog("进入鼠标捕获模式");
            var captureWindow = new CaptureWindow();
            captureWindow.Owner = this;
            bool? result = captureWindow.ShowDialog();

            if (result == true && captureWindow.CapturedPoint.HasValue)
            {
                var winPoint = captureWindow.CapturedPoint.Value;
                POINT pt = new POINT { X = (int)winPoint.X, Y = (int)winPoint.Y };

                IntPtr hwnd = WindowFromPoint(pt);
                if (hwnd != IntPtr.Zero)
                {
                    var title = new StringBuilder(256);
                    GetWindowText(hwnd, title, title.Capacity);
                    string windowTitle = title.ToString();

                    if (windowTitle.Contains("雷神加速器"))
                    {
                        if (GetWindowRect(hwnd, out RECT windowRect))
                        {
                            _manualX = pt.X - windowRect.Left;
                            _manualY = pt.Y - windowRect.Top;
                            SaveConfig();
                            UpdateCoordDisplay();
                            AddLog($"✅ 坐标捕获成功：({_manualX}, {_manualY})");
                        }
                    }
                    else
                    {
                        AddLog($"捕获失败：窗口不是雷神加速器 ({windowTitle})");
                    }
                }
            }
            WindowState = WindowState.Normal;
        }

        private void CaptureCurrentMousePosition()
        {
            GetCursorPos(out POINT cursorPos);
            IntPtr hwnd = WindowFromPoint(cursorPos);
            if (hwnd == IntPtr.Zero) { AddLog("捕获失败：未找到窗口"); return; }

            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            string windowTitle = title.ToString();

            if (!windowTitle.Contains("雷神加速器"))
            {
                AddLog($"捕获失败：当前窗口不是雷神加速器 ({windowTitle})");
                return;
            }

            if (!GetWindowRect(hwnd, out RECT windowRect))
            { AddLog("捕获失败：无法获取窗口位置"); return; }

            _manualX = cursorPos.X - windowRect.Left;
            _manualY = cursorPos.Y - windowRect.Top;
            SaveConfig();
            UpdateCoordDisplay();
            AddLog($"⌨ 热键捕获坐标：({_manualX}, {_manualY})");
        }

        private void UpdateCoordDisplay()
        {
            TxtCoordDisplay.Text = $"({_manualX}, {_manualY})";
        }

        // ======================== 配置持久化 ========================
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var coord = JsonSerializer.Deserialize<CoordConfig>(json);
                    if (coord != null) { _manualX = coord.X; _manualY = coord.Y; }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                var coord = new CoordConfig { X = _manualX, Y = _manualY };
                string json = JsonSerializer.Serialize(coord);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        // ======================== 统计 ========================
        private void LoadStatistics()
        {
            try
            {
                string statPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stats.json");
                if (File.Exists(statPath))
                {
                    var stats = JsonSerializer.Deserialize<AppStatistics>(File.ReadAllText(statPath));
                    if (stats != null)
                    {
                        _todayPauseCount = stats.TodayCount;
                        _totalPauseCount = stats.TotalCount;
                        _sessionCount = stats.SessionCount;
                        if (stats.LastDate != DateTime.Now.ToString("yyyy-MM-dd"))
                            _todayPauseCount = 0;
                    }
                }
            }
            catch { }
        }

        private void SaveStatistics()
        {
            try
            {
                var stats = new AppStatistics
                {
                    TodayCount = _todayPauseCount,
                    TotalCount = _totalPauseCount,
                    SessionCount = _sessionCount,
                    LastDate = DateTime.Now.ToString("yyyy-MM-dd")
                };
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stats.json"),
                    JsonSerializer.Serialize(stats));
            }
            catch { }
        }

        private void UpdateStatisticsDisplay()
        {
            TxtTodayCount.Text = _todayPauseCount.ToString();
            int savedMin = _totalPauseCount * 60;
            if (savedMin >= 60)
                TxtSavedTime.Text = $"{savedMin / 60}h{savedMin % 60}m";
            else
                TxtSavedTime.Text = $"{savedMin} 分钟";
            TxtVersionInfo.Text = $"v2.0 · 累计启动 {_sessionCount} 次 · 暂停 {_totalPauseCount} 次";
        }

        // ======================== 检查更新（从 GitHub Releases） ========================
        // ⚠ 请将下面的 repoOwner 和 repoName 替换为你自己的 GitHub 仓库信息
        private const string GitHubRepoOwner = "Bade-Gusi";
        private const string GitHubRepoName = "leishen-pause";
        private const string CurrentVersion = "v2.0";

        private async Task CheckForUpdatesAsync()
        {
            AddLog("正在检查更新...");
            try
            {
                // 使用系统代理设置（自动适配 IE/Windows 代理配置）
                var handler = new System.Net.Http.HttpClientHandler
                {
                    // 自动使用系统代理（大多数代理软件会设置系统代理）
                    UseProxy = true,
                    // 如果直连失败，自动使用默认代理
                    Proxy = System.Net.Http.HttpClient.DefaultProxy
                };
                using var client = new System.Net.Http.HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PUBGMonitor/2.0");
                client.Timeout = TimeSpan.FromSeconds(15);

                string url = $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest";
                var response = await client.GetStringAsync(url);

                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                if (release?.TagName != null && string.Compare(release.TagName, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    AddLog($"✨ 发现新版本: {release.TagName}");

                    var result = MessageBox.Show(
                        $"发现新版本 {release.TagName}！\n\n{release.Body}\n\n是否下载更新？",
                        "发现更新",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // 找到第一个 .exe 或 .zip 的下载链接
                        var asset = release.Assets?.FirstOrDefault(a =>
                            a.Name?.EndsWith(".exe") == true || a.Name?.EndsWith(".zip") == true);
                        if (asset?.BrowserDownloadUrl != null)
                        {
                            AddLog($"⬇ 开始下载更新: {asset.Name}");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = asset.BrowserDownloadUrl,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            // 直接打开 releases 页面
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = release.HtmlUrl ?? $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/releases",
                                UseShellExecute = true
                            });
                        }
                    }
                }
                else
                {
                    AddLog("✓ 已是最新版本");
                }
            }
            catch (Exception ex)
            {
                AddLog($"检查更新失败: {ex.Message}");
            }
        }

        // ======================== 事件处理 ========================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // 最小化到托盘
            WindowState = WindowState.Minimized;
        }

        private void StartMonitor_Click(object sender, RoutedEventArgs e) => StartMonitoring();
        private void StopMonitor_Click(object sender, RoutedEventArgs e) => StopMonitor();
        private void CaptureCoord_Click(object sender, RoutedEventArgs e) => StartMouseCapture();

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
        }

        private void TestManualClick_Click(object sender, RoutedEventArgs e)
        {
            AddLog("=== 测试点击开始 ===");
            string[] titles = { "雷神加速器", "雷神加速器 - 加速游戏", "LeiShen", "雷神" };
            IntPtr hwnd = IntPtr.Zero;
            foreach (string title in titles)
            {
                hwnd = FindWindow(null, title);
                if (hwnd != IntPtr.Zero) { AddLog($"找到窗口：{title}"); break; }
            }

            if (hwnd == IntPtr.Zero) { AddLog("未找到雷神加速器窗口"); return; }

            if (TryAutoClick(hwnd))
                AddLog("UI Automation 点击成功");
            else
                ManualClick(hwnd);
            AddLog("=== 测试点击结束 ===");
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e) => _logMessages.Clear();

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
        }

        private void DarkMode_Toggled(object sender, RoutedEventArgs e)
        {
            _isDarkMode = ChkDarkMode.IsChecked == true;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                BtnThemeToggle.Content = "🌙";
                ChkDarkMode.IsChecked = true;
            }
            else
            {
                BtnThemeToggle.Content = "☀️";
                AddLog("切换主题 (实验性)");
            }
        }

        private void AutoStart_Checked(object sender, RoutedEventArgs e) => SetAutoStart(true);
        private void AutoStart_Unchecked(object sender, RoutedEventArgs e) => SetAutoStart(false);

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_RUN_PATH, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string appPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                            if (!string.IsNullOrEmpty(appPath))
                            {
                                key.SetValue(APP_NAME, appPath);
                                AddLog("✅ 已设置开机自启动");
                            }
                        }
                        else
                        {
                            key.DeleteValue(APP_NAME, false);
                            AddLog("已取消开机自启动");
                        }
                    }
                }
            }
            catch (Exception ex) { AddLog($"设置失败：{ex.Message}"); }
        }

        private void CheckAutoStartStatus()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_RUN_PATH))
                {
                    if (key != null)
                        ChkAutoStart.IsChecked = !string.IsNullOrEmpty(key.GetValue(APP_NAME) as string);
                }
            }
            catch { }
        }
    }

    // ======================== 数据模型 ========================
    public class CoordConfig { public int X { get; set; } public int Y { get; set; } }

    public class AppStatistics
    {
        public int TodayCount { get; set; }
        public int TotalCount { get; set; }
        public int SessionCount { get; set; }
        public string? LastDate { get; set; }
    }

    public class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? Body { get; set; }
        public string? HtmlUrl { get; set; }
        public List<GitHubAsset>? Assets { get; set; }
    }

    public class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public struct POINT { public int X; public int Y; }
}
