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
        // Win32 API
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
        [DllImport("user32.dll")] static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public MOUSEINPUT mi; }
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const int SW_SHOW = 5;
        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_P = 0x50;
        private const int WM_HOTKEY = 0x0312;

        private DispatcherTimer? _timer;
        private bool _isGameRunning = false;
        private bool _isMonitoring = false;

        private string _configPath = "";
        private int _manualX = 160;
        private int _manualY = 360;
        private const string REG_RUN_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "PUBGMonitor";
        private ObservableCollection<string> _logMessages = new();
        private int _todayPauseCount = 0;
        private int _totalPauseCount = 0;
        private int _sessionCount = 0;
        private bool _isDarkMode = true;
        private bool _isQuitting = false;
        private WindowInteropHelper? _windowHelper;

        public MainWindow()
        {
            InitializeComponent();
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coord_config.json");
            LogList.ItemsSource = _logMessages;
            LoadConfig();
            UpdateCoordDisplay();
            CheckAutoStartStatus();
            LoadStatistics();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            this.StateChanged += MainWindow_StateChanged;
            ApplyLanguage();
            AddLog(Lang.Get("log_startup"));
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logMessages.Count > 100) _logMessages.RemoveAt(0);
                LogList.ScrollIntoView(_logMessages.LastOrDefault());
            });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHelper = new WindowInteropHelper(this);
            RegisterHotKey(_windowHelper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_P);
            var source = HwndSource.FromHwnd(_windowHelper.Handle);
            if (source != null) source.AddHook(HwndHook);

            UpdateStatisticsDisplay();
            AnimateCardsIn();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (!_isQuitting && _windowHelper != null)
                UnregisterHotKey(_windowHelper.Handle, HOTKEY_ID);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                AddLog(Lang.Get("log_tray_minimized"));
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            { Dispatcher.Invoke(() => CaptureCurrentMousePosition()); handled = true; }
            return IntPtr.Zero;
        }

        private void AnimateCardsIn()
        {
            var cards = new[] { CardCoord, CardOptions, CardLog };
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                card.Opacity = 0;
                card.RenderTransform = new TranslateTransform(0, 25);
                card.RenderTransformOrigin = new Point(0.5, 0);
                var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.4),
                    BeginTime = TimeSpan.FromMilliseconds(150 + i * 100), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                card.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                var slideUp = new DoubleAnimation { From = 25, To = 0, Duration = TimeSpan.FromSeconds(0.4),
                    BeginTime = TimeSpan.FromMilliseconds(150 + i * 100), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                ((TranslateTransform)card.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideUp);
            }
        }

        // ======================== 状态 ========================
        private void SetStatus(string textKey, string colorHex, string icon, bool pulseRing = false)
        {
            StatusText.Text = Lang.Get(textKey);
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            StatusText.Foreground = new SolidColorBrush(c);
            StatusIcon.Text = icon;
            var ringColor = new SolidColorBrush(c);
            StatusRing.Stroke = ringColor; StatusGlow.Fill = ringColor; StatusDot.Fill = ringColor;
            var pulse = Resources["PulseGlow"] as Storyboard;
            if (pulseRing) { pulse?.Begin(StatusRing); pulse?.Begin(StatusGlow); }
            else { pulse?.Stop(StatusRing); pulse?.Stop(StatusGlow); StatusRing.Opacity = 0.3; StatusGlow.Opacity = 0.12; }
        }

        private void StartSpinner() { SpinnerBorder.Visibility = Visibility.Visible; (Resources["SpinAnimation"] as Storyboard)?.Begin(Spinner); }
        private void StopSpinner() { (Resources["SpinAnimation"] as Storyboard)?.Stop(Spinner); SpinnerBorder.Visibility = Visibility.Collapsed; }

        // ======================== 监控 ========================
        private void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += async (s, ev) => await CheckProcessAsync();
            _timer.Start();
            BtnStart.IsEnabled = false; BtnStop.IsEnabled = true;
            BtnStop.Background = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); BtnStop.Foreground = Brushes.White;
            SetStatus("status_scanning", "#00E5FF", "⟳"); StartSpinner();
            GameNameText.Text = Lang.Get("status_detecting");
            AddLog(Lang.Get("log_monitor_started"));
        }

        private void StopMonitor()
        {
            _isMonitoring = false;
            if (_timer != null) { _timer.Stop(); _timer = null; }
            BtnStart.IsEnabled = true; BtnStop.IsEnabled = false;
            BtnStop.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44));
            BtnStop.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0x88));
            SetStatus("status_idle", "#00C853", "⏸"); StopSpinner();
            GameNameText.Text = Lang.Get("status_stopped");
            AddLog(Lang.Get("log_monitor_stopped"));
        }

        private async Task CheckProcessAsync()
        {
            bool found = await Task.Run(() => { try { return Process.GetProcessesByName("TslGame").Any(); } catch { return false; } });
            Dispatcher.Invoke(() =>
            {
                if (found)
                {
                    if (!_isGameRunning)
                    {
                        _isGameRunning = true; StopSpinner();
                        SetStatus("status_gaming", "#FF1744", "▶", true);
                        GameNameText.Text = Lang.Get("status_protecting");
                        GameNameText.Foreground = Brushes.OrangeRed;
                        AddLog(Lang.Get("log_game_detected"));
                        _sessionCount++; SaveStatistics();
                    }
                }
                else if (_isGameRunning)
                {
                    _isGameRunning = false;
                    _todayPauseCount++; _totalPauseCount++;
                    SetStatus("status_paused", "#00C853", "✓"); StopSpinner();
                    GameNameText.Text = Lang.Get("status_game_closed");
                    GameNameText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0x53));
                    AddLog($"{Lang.Get("log_game_closed")} (今日第{_todayPauseCount}次)");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        DoPause();
                        await Task.Delay(300);
                        Dispatcher.Invoke(() => { UpdateStatisticsDisplay(); SaveStatistics();
                            if (ChkShowReminder.IsChecked == true) ShowReminderWindow(); });
                    });
                }
            });
        }

        // ======================== 暂停（多策略） ========================
        private void DoPause()
        {
            string[] titles = { "雷神加速器", "雷神加速器 - 加速游戏", "LeiShen", "雷神" };
            IntPtr hwnd = IntPtr.Zero;
            foreach (string t in titles) { hwnd = FindWindow(null, t); if (hwnd != IntPtr.Zero) break; }
            if (hwnd == IntPtr.Zero) { AddLog(Lang.Get("log_window_not_found")); return; }
            AddLog($"{Lang.Get("log_window_found")} (句柄: {hwnd})");

            if (TryUiaClick(hwnd)) return;
            if (TryFindAnyClickable(hwnd)) return;
            AddLog("🖱 使用预设坐标模拟点击");
            ManualClick(hwnd);
        }

        // 策略1: UI Automation 找暂停按钮 InvokePattern
        private bool TryUiaClick(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return false;
                var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                foreach (AutomationElement btn in root.FindAll(TreeScope.Descendants, cond))
                {
                    string name = btn.Current.Name;
                    string aid = btn.Current.AutomationId;
                    if ((!string.IsNullOrEmpty(name) && (name.Contains("暂停") || name.Contains("Pause") || name.Contains("pause") || name.Contains("一時停止"))) ||
                        (!string.IsNullOrEmpty(aid) && aid.IndexOf("pause", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        AddLog($"{Lang.Get("log_auto_click")}: Name={name}");
                        if (btn.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                        { ((InvokePattern)p).Invoke(); AddLog(Lang.Get("log_pause_success")); return true; }
                        if (!btn.Current.BoundingRectangle.IsEmpty && GetWindowRect(GetAncestor(hwnd, GA_ROOT), out RECT wr))
                        {
                            var r = btn.Current.BoundingRectangle;
                            return SimulateMouseClick(GetAncestor(hwnd, GA_ROOT), (int)(r.Left - wr.Left + r.Width / 2), (int)(r.Top - wr.Top + r.Height / 2));
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // 策略2: 在窗口中查找所有可点击元素，尝试识别暂停功能
        private bool TryFindAnyClickable(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return false;

                // 查找所有 ControlType.Button 和 ControlType.CheckBox
                var allControls = root.FindAll(TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox)));

                // 尝试所有按钮，打印名称供调试
                int index = 0;
                foreach (AutomationElement el in allControls)
                {
                    string name = el.Current.Name;
                    string aid = el.Current.AutomationId;
                    if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(aid))
                    {
                        AddLog($"  控件[{index}]: Name='{name}' AId='{aid}' Type={el.Current.ControlType.ProgrammaticName}");
                        index++;
                    }
                }

                // 如果没找到，继续尝试查找并点击第一个按钮（可能是暂停按钮）
                if (allControls.Count > 0)
                {
                    var firstBtn = allControls[0];
                    // 尝试 InvokePattern
                    if (firstBtn.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                    { ((InvokePattern)p).Invoke(); AddLog("✅ 点击了第一个按钮"); return true; }
                }
            }
            catch { }
            return false;
        }

        private void ManualClick(IntPtr hwnd)
        {
            if (!GetWindowRect(hwnd, out RECT rect)) { AddLog("无法获取窗口矩形"); return; }
            int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
            if (_manualX < 0 || _manualX > w || _manualY < 0 || _manualY > h)
            { _manualX = w / 2; _manualY = h / 2; SaveConfig(); UpdateCoordDisplay(); }
            SimulateMouseClick(hwnd, _manualX, _manualY);
        }

        private bool SimulateMouseClick(IntPtr hwnd, int rx, int ry)
        {
            AddLog($"{Lang.Get("log_manual_click")} ({rx}, {ry})");
            if (!GetWindowRect(hwnd, out RECT wr)) return false;
            int sx = wr.Left + rx, sy = wr.Top + ry;
            GetCursorPos(out POINT oldPos);
            for (int i = 0; i < 3; i++)
            { ShowWindow(hwnd, SW_SHOW); BringWindowToTop(hwnd); SetForegroundWindow(hwnd); System.Threading.Thread.Sleep(150); if (GetForegroundWindow() == hwnd) break; }
            SetCursorPos(sx, sy); System.Threading.Thread.Sleep(100);
            for (int a = 0; a < 3; a++)
            {
                var inputs = new INPUT[2];
                inputs[0].type = INPUT_MOUSE; inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
                inputs[1].type = INPUT_MOUSE; inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;
                if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 2) { SetCursorPos(oldPos.X, oldPos.Y); AddLog($"✅ 点击成功 (尝试 {a + 1})"); return true; }
                System.Threading.Thread.Sleep(50);
            }
            SetCursorPos(oldPos.X, oldPos.Y);
            return false;
        }

        // ======================== 提醒窗口 ========================
        private void ShowReminderWindow()
        {
            try { new ReminderWindow(_todayPauseCount).Show(); AddLog("📢 显示全屏提醒窗口"); }
            catch (Exception ex) { AddLog($"提醒窗口异常：{ex.Message}"); }
        }

        // ======================== 坐标捕获 ========================
        private void StartMouseCapture()
        {
            WindowState = WindowState.Minimized; AddLog("进入鼠标捕获模式");
            var cw = new CaptureWindow { Owner = this };
            if (cw.ShowDialog() == true && cw.CapturedPoint.HasValue)
            {
                var pt = new POINT { X = (int)cw.CapturedPoint.Value.X, Y = (int)cw.CapturedPoint.Value.Y };
                var hwnd = WindowFromPoint(pt);
                if (hwnd != IntPtr.Zero)
                {
                    var title = new StringBuilder(256); GetWindowText(hwnd, title, title.Capacity);
                    if (title.ToString().Contains("雷神加速器") && GetWindowRect(hwnd, out RECT wr))
                    { _manualX = pt.X - wr.Left; _manualY = pt.Y - wr.Top; SaveConfig(); UpdateCoordDisplay(); AddLog($"{Lang.Get("log_coord_captured")} ({_manualX}, {_manualY})"); }
                    else AddLog($"捕获失败：窗口不是雷神加速器 ({title})");
                }
            }
            WindowState = WindowState.Normal;
        }

        private void CaptureCurrentMousePosition()
        {
            GetCursorPos(out POINT cp); var hwnd = WindowFromPoint(cp);
            if (hwnd == IntPtr.Zero) return;
            var t = new StringBuilder(256); GetWindowText(hwnd, t, t.Capacity);
            if (!t.ToString().Contains("雷神加速器")) return;
            if (!GetWindowRect(hwnd, out RECT wr)) return;
            _manualX = cp.X - wr.Left; _manualY = cp.Y - wr.Top;
            SaveConfig(); UpdateCoordDisplay();
            AddLog($"⌨ 热键捕获坐标：({_manualX}, {_manualY})");
        }

        private void UpdateCoordDisplay() => TxtCoordDisplay.Text = $"({_manualX}, {_manualY})";

        // ======================== 配置 ========================
        private void LoadConfig() { try { if (File.Exists(_configPath)) { var c = JsonSerializer.Deserialize<CoordConfig>(File.ReadAllText(_configPath)); if (c != null) { _manualX = c.X; _manualY = c.Y; } } } catch { } }
        private void SaveConfig() { try { File.WriteAllText(_configPath, JsonSerializer.Serialize(new CoordConfig { X = _manualX, Y = _manualY })); } catch { } }

        // ======================== 统计 ========================
        private void LoadStatistics()
        {
            try
            {
                var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stats.json");
                if (File.Exists(p))
                {
                    var s = JsonSerializer.Deserialize<AppStatistics>(File.ReadAllText(p));
                    if (s != null) { _todayPauseCount = s.TodayCount; _totalPauseCount = s.TotalCount; _sessionCount = s.SessionCount; if (s.LastDate != DateTime.Now.ToString("yyyy-MM-dd")) _todayPauseCount = 0; }
                }
            }
            catch { }
        }
        private void SaveStatistics()
        {
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stats.json"), JsonSerializer.Serialize(new AppStatistics { TodayCount = _todayPauseCount, TotalCount = _totalPauseCount, SessionCount = _sessionCount, LastDate = DateTime.Now.ToString("yyyy-MM-dd") })); }
            catch { }
        }
        private void UpdateStatisticsDisplay()
        {
            TxtTodayCount.Text = _todayPauseCount.ToString();
            int sm = _totalPauseCount * 60;
            TxtSavedTime.Text = sm >= 60 ? $"{sm / 60}h{sm % 60}m" : $"{sm} {Lang.Get("minutes")}";
            TxtVersionInfo.Text = string.Format(Lang.Get("footer_stats"), _sessionCount, _totalPauseCount);
        }

        // ======================== 检查更新 ========================
        private const string GitHubRepoOwner = "Bade-Gusi";
        private const string GitHubRepoName = "leishen-pause";
        private const string CurrentVersion = "v2.0";

        private async Task CheckForUpdatesAsync()
        {
            AddLog(Lang.Get("log_update_checking"));
            try
            {
                var handler = new System.Net.Http.HttpClientHandler { UseProxy = true, Proxy = System.Net.Http.HttpClient.DefaultProxy };
                using var client = new System.Net.Http.HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PUBGMonitor/2.0");
                client.Timeout = TimeSpan.FromSeconds(15);
                var resp = await client.GetStringAsync($"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest");
                var rel = JsonSerializer.Deserialize<GitHubRelease>(resp);
                if (rel?.TagName != null && string.Compare(rel.TagName, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    AddLog($"{Lang.Get("log_update_found")}: {rel.TagName}");
                    if (MessageBox.Show(string.Format(Lang.Get("update_msg"), rel.TagName, rel.Body ?? ""), Lang.Get("update_title"), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        var asset = rel.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".exe") == true || a.Name?.EndsWith(".zip") == true);
                        Process.Start(new ProcessStartInfo { FileName = asset?.BrowserDownloadUrl ?? rel.HtmlUrl ?? $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/releases", UseShellExecute = true });
                    }
                }
                else AddLog(Lang.Get("log_update_latest"));
            }
            catch (Exception ex) { AddLog($"{Lang.Get("log_update_failed")}: {ex.Message}"); }
        }

        // ======================== 主题 ========================
        private void ApplyTheme()
        {
            var bg = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#1A1A2E" : "#F5F0EB");
            var card = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#16213E" : "#FFFFFF");
            var border = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#2A2A4A" : "#DDD5CC");
            var text = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#E8E8E8" : "#2D2D2D");
            var text2 = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#667788" : "#888888");
            var textMuted = (Color)ColorConverter.ConvertFromString(_isDarkMode ? "#556677" : "#AAAAAA");

            MainBorder.Background = new SolidColorBrush(bg);
            TopLine.Background = new SolidColorBrush(_isDarkMode ? Color.FromRgb(0x00, 0xC8, 0x53) : Color.FromRgb(0x66, 0xBB, 0x6A));
            BtnThemeToggle.Content = _isDarkMode ? "🌙" : "☀️";
            ChkDarkMode.IsChecked = _isDarkMode;
            TxtDarkIcon.Text = _isDarkMode ? "🌙" : "☀️";
            TxtOptDarkmode.Text = Lang.Get(_isDarkMode ? "theme_dark" : "theme_light");

            // 更新所有卡片
            foreach (var cardBorder in new[] { CardStatus, CardCoord, CardOptions, CardLog })
            {
                cardBorder.Background = new SolidColorBrush(card);
                cardBorder.BorderBrush = new SolidColorBrush(border);
            }
            // 更新所有内嵌背景
            foreach (var innerBg in FindVisualChildren<System.Windows.Controls.Border>(this))
            {
                if (innerBg.Background is SolidColorBrush sb && sb.Color == Color.FromRgb(0x1A, 0x1A, 0x2E))
                    innerBg.Background = new SolidColorBrush(bg);
            }
            // 更新文字颜色
            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(this))
            {
                if (tb.Foreground is SolidColorBrush s)
                {
                    if (s.Color == Color.FromRgb(0xE8, 0xE8, 0xE8)) tb.Foreground = new SolidColorBrush(text);
                    else if (s.Color == Color.FromRgb(0xCC, 0xCC, 0xDD)) tb.Foreground = new SolidColorBrush(text);
                    else if (s.Color == Color.FromRgb(0x66, 0x77, 0x88)) tb.Foreground = new SolidColorBrush(text2);
                    else if (s.Color == Color.FromRgb(0x55, 0x66, 0x77)) tb.Foreground = new SolidColorBrush(textMuted);
                }
            }
        }

        private System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var c in FindVisualChildren<T>(child)) yield return c;
            }
        }

        // ======================== 语言 ========================
        private void ApplyLanguage()
        {
            // 切换 XAML 中所有绑定的文字
            TxtAppTitle.Text = Lang.Get("app_title");
            TxtAppSubtitle.Text = $"{(Lang.CurrentLang == "zh" ? "智能时长暂停工具" : "Smart Pause Tool")} · {Lang.Get("app_version")}";
            TxtStatusLabel.Text = Lang.Get("status_label");
            TxtTodayLabel.Text = Lang.Get("today_pause");
            TxtSavedLabel.Text = Lang.Get("saved");
            BtnStart.Content = Lang.Get("btn_start");
            BtnStop.Content = Lang.Get("btn_stop");
            BtnCapture.Content = Lang.Get("btn_capture");
            BtnTestClick.Content = Lang.Get("btn_test");
            BtnCheckUpdate.Content = Lang.Get("btn_check_update");
            BtnClearLog.Content = Lang.Get("btn_clear_log");
            BtnQuit.Content = Lang.Get("btn_quit");
            TxtSectionCoordDesc.Text = Lang.Get("section_coord_desc");
            TxtCoordLabel.Text = Lang.Get("coord_label");
            TxtCoordHint.Text = Lang.Get("coord_hint");
            TxtSectionOptionsDesc.Text = Lang.Get("section_options_desc");
            TxtOptAutostart.Text = Lang.Get("opt_autostart");
            TxtOptReminder.Text = Lang.Get("opt_reminder");
            TxtOptDarkmode.Text = Lang.Get(_isDarkMode ? "theme_dark" : "theme_light");
            TxtOptUpdate.Text = Lang.Get("opt_check_update");
            TxtSectionLogDesc.Text = Lang.Get("section_log_desc");
            TxtFooter.Text = Lang.Get("footer_copyright");
            TxtQQ.Text = "QQ: 2994938720";
            UpdateStatisticsDisplay();
            BtnLang.Content = Lang.CurrentLang == "zh" ? "EN" : "中";
            BtnLang.ToolTip = Lang.CurrentLang == "zh" ? "Switch Language" : "切换语言";
            GameNameText.Text = Lang.Get(_isGameRunning ? "status_protecting" : _isMonitoring ? "status_detecting" : "status_waiting");
        }

        // ======================== 事件 ========================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Close_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            _isQuitting = true;
            if (_windowHelper != null) UnregisterHotKey(_windowHelper.Handle, HOTKEY_ID);
            Application.Current.Shutdown();
        }

        private void StartMonitor_Click(object sender, RoutedEventArgs e) => StartMonitoring();
        private void StopMonitor_Click(object sender, RoutedEventArgs e) => StopMonitor();
        private void CaptureCoord_Click(object sender, RoutedEventArgs e) => StartMouseCapture();
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync();
        private void ClearLog_Click(object sender, RoutedEventArgs e) => _logMessages.Clear();

        private void TestManualClick_Click(object sender, RoutedEventArgs e)
        {
            AddLog("=== 测试点击 ===");
            string[] titles = { "雷神加速器", "雷神加速器 - 加速游戏", "LeiShen", "雷神" };
            IntPtr hwnd = IntPtr.Zero;
            foreach (string t in titles) { hwnd = FindWindow(null, t); if (hwnd != IntPtr.Zero) break; }
            if (hwnd == IntPtr.Zero) { AddLog("未找到雷神加速器窗口"); return; }
            if (TryUiaClick(hwnd) || TryFindAnyClickable(hwnd)) AddLog("✅ 自动点击成功");
            else ManualClick(hwnd);
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e) { _isDarkMode = !_isDarkMode; ApplyTheme(); ApplyLanguage(); }
        private void DarkMode_Toggled(object sender, RoutedEventArgs e) { _isDarkMode = ChkDarkMode.IsChecked == true; ApplyTheme(); ApplyLanguage(); }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            var langs = Lang.Languages.Keys.ToList();
            int idx = langs.IndexOf(Lang.CurrentLang);
            if (idx < 0 || idx >= langs.Count - 1) idx = 0; else idx++;
            Lang.CurrentLang = langs[idx];
            // 循环切换
            ApplyLanguage();
            AddLog($"Language: {Lang.Languages[Lang.CurrentLang]}");
        }

        private void AutoStart_Checked(object sender, RoutedEventArgs e) => SetAutoStart(true);
        private void AutoStart_Unchecked(object sender, RoutedEventArgs e) => SetAutoStart(false);
        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REG_RUN_PATH, true);
                if (key == null) return;
                if (enable) { key.SetValue(APP_NAME, Process.GetCurrentProcess().MainModule?.FileName ?? ""); AddLog("✅ 已设置开机自启动"); }
                else { key.DeleteValue(APP_NAME, false); AddLog("已取消开机自启动"); }
            }
            catch (Exception ex) { AddLog($"设置失败：{ex.Message}"); }
        }
        private void CheckAutoStartStatus()
        {
            try { using var key = Registry.CurrentUser.OpenSubKey(REG_RUN_PATH); if (key != null) ChkAutoStart.IsChecked = !string.IsNullOrEmpty(key.GetValue(APP_NAME) as string); }
            catch { }
        }
    }

    // 数据模型
    public class CoordConfig { public int X { get; set; } public int Y { get; set; } }
    public class AppStatistics { public int TodayCount { get; set; } public int TotalCount { get; set; } public int SessionCount { get; set; } public string? LastDate { get; set; } }
    public class GitHubRelease { public string? TagName { get; set; } public string? Body { get; set; } public string? HtmlUrl { get; set; } public List<GitHubAsset>? Assets { get; set; } }
    public class GitHubAsset { public string? Name { get; set; } public string? BrowserDownloadUrl { get; set; } }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public struct POINT { public int X; public int Y; }
}
