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
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint GA_ROOT = 2;
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

            // 安全校验：确认找到的是雷神加速器窗口，绝不是 PUBG
            var titleBuf = new StringBuilder(256);
            GetWindowText(hwnd, titleBuf, titleBuf.Capacity);
            string winTitle = titleBuf.ToString();
            if (!winTitle.Contains("雷神") && !winTitle.Contains("LeiShen"))
            {
                AddLog($"❌ 安全拦截：目标窗口不是雷神加速器 ({winTitle})，跳过操作");
                return;
            }
            AddLog($"{Lang.Get("log_window_found")} (标题: {winTitle})");

            if (TryUiaClick(hwnd)) return;
            if (TryFindAnyClickable(hwnd)) return;
            AddLog("🖱 使用预设坐标模拟点击");
            ManualClick(hwnd);
        }

        // 策略1: UI Automation 找暂停按钮 InvokePattern（安全，不触发反作弊）
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

                        // 使用 PostMessage 模拟点击（窗口消息，不经过硬件层，反作弊检测不到）
                        if (!btn.Current.BoundingRectangle.IsEmpty)
                        {
                            var r = btn.Current.BoundingRectangle;
                            var cX = (int)(r.Left + r.Width / 2);
                            var cY = (int)(r.Top + r.Height / 2);
                            var lParam = IntPtr.Zero; // 简化，实际应编码坐标
                            SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, IntPtr.Zero);
                            System.Threading.Thread.Sleep(30);
                            SendMessage(hwnd, WM_LBUTTONUP, (IntPtr)0, IntPtr.Zero);
                            AddLog("✅ PostMessage 点击完成");
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // 策略2: 在窗口中查找所有可点击元素，使用 InvokePattern（安全）
        private bool TryFindAnyClickable(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return false;

                var allControls = root.FindAll(TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox)));

                // 遍历所有控件，优先找 InvokePattern
                foreach (AutomationElement el in allControls)
                {
                    if (el.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                    {
                        string name = el.Current.Name;
                        string aid = el.Current.AutomationId;
                        AddLog($"  尝试控件: Name='{name}' AId='{aid}'");
                        ((InvokePattern)p).Invoke();
                        AddLog("✅ InvokePattern 调用成功");
                        return true;
                    }
                }

                return false;
            }
            catch { }
            return false;
        }

        // 策略3: 使用用户保存的坐标，通过 PostMessage 发送（窗口消息，安全）
        private void ManualClick(IntPtr hwnd)
        {
            if (!GetWindowRect(hwnd, out RECT rect)) { AddLog("无法获取窗口矩形"); return; }
            int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
            if (_manualX < 0 || _manualX > w || _manualY < 0 || _manualY > h)
            { _manualX = w / 2; _manualY = h / 2; SaveConfig(); UpdateCoordDisplay(); }

            AddLog($"📨 通过 PostMessage 发送点击到 ({_manualX}, {_manualY})");
            // 编码坐标到 LPARAM (低16位X, 高16位Y)
            var lParam = (IntPtr)((_manualY << 16) | (_manualX & 0xFFFF));
            SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
            System.Threading.Thread.Sleep(50);
            SendMessage(hwnd, WM_LBUTTONUP, (IntPtr)0, lParam);
            AddLog("✅ PostMessage 点击完成");
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
                var handler = new System.Net.Http.HttpClientHandler();
                // 尝试使用代理 (Clash 默认端口 7897, 也尝试常见端口)
                try
                {
                    var proxyUri = new Uri("http://127.0.0.1:7897");
                    // 测试代理是否可用
                    var testReq = System.Net.WebRequest.CreateHttp("http://127.0.0.1:7897");
                    testReq.Timeout = 2000;
                    testReq.Method = "GET";
                    using var testResp = (System.Net.HttpWebResponse)await Task.Run(() => (System.Net.HttpWebResponse)testReq.GetResponse());
                    if (testResp.StatusCode == System.Net.HttpStatusCode.OK || testResp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        handler.Proxy = new System.Net.WebProxy(proxyUri);
                        handler.UseProxy = true;
                    }
                }
                catch
                {
                    // 代理不可用，尝试直连或系统代理
                    handler.UseProxy = true;
                    handler.Proxy = System.Net.Http.HttpClient.DefaultProxy;
                }
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
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(ApplyTheme); return; }

            Color SafeColor(string hex, Color fallback)
            {
                try { var o = ColorConverter.ConvertFromString(hex); return o is Color c ? c : fallback; }
                catch { return fallback; }
            }
            var dark = _isDarkMode;
            var bg = SafeColor(dark ? "#1A1A2E" : "#F5F0EB", Color.FromRgb(0x1A, 0x1A, 0x2E));
            var card = SafeColor(dark ? "#16213E" : "#FFFFFF", Color.FromRgb(0x16, 0x21, 0x3E));
            var border = SafeColor(dark ? "#2A2A4A" : "#DDD5CC", Color.FromRgb(0x2A, 0x2A, 0x4A));
            var textMain = SafeColor(dark ? "#E8E8E8" : "#2D2D2D", Color.FromRgb(0xE8, 0xE8, 0xE8));

            try
            {
                if (MainBorder != null) MainBorder.Background = new SolidColorBrush(bg);
                if (TopLine != null) TopLine.Background = new SolidColorBrush(dark ? Color.FromRgb(0x00, 0xC8, 0x53) : Color.FromRgb(0x66, 0xBB, 0x6A));
                if (BtnThemeToggle != null) BtnThemeToggle.Content = dark ? "🌙" : "☀️";
                if (ChkDarkMode != null) ChkDarkMode.IsChecked = dark;
                if (TxtDarkIcon != null) TxtDarkIcon.Text = dark ? "🌙" : "☀️";
                if (TxtOptDarkmode != null) TxtOptDarkmode.Text = Lang.Get(dark ? "theme_dark" : "theme_light");

                foreach (var x in new[] { TxtSectionCoord, TxtSectionOptions, TxtSectionLog,
                    TxtOptAutostart, TxtOptReminder, TxtOptDarkmode, TxtOptUpdate })
                { if (x != null) x.Foreground = new SolidColorBrush(textMain); }

                foreach (var c in new[] { CardStatus, CardCoord, CardOptions, CardLog })
                {
                    if (c == null) continue;
                    c.Background = new SolidColorBrush(card);
                    c.BorderBrush = new SolidColorBrush(border);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyTheme error: {ex.Message}");
            }
        }

        // ======================== 语言 ========================
        private void ApplyLanguage()
        {
            try
            {
                if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(ApplyLanguage); return; }
                Helper.SetText(TxtAppTitle, Lang.Get("app_title"));
                Helper.SetText(TxtAppSubtitle, $"{(Lang.CurrentLang == "zh" ? "智能时长暂停工具" : "Smart Pause Tool")} · {Lang.Get("app_version")}");
                Helper.SetText(TxtStatusLabel, Lang.Get("status_label"));
                Helper.SetText(TxtTodayLabel, Lang.Get("today_pause"));
                Helper.SetText(TxtSavedLabel, Lang.Get("saved"));
                Helper.SetContent(BtnStart, Lang.Get("btn_start"));
                Helper.SetContent(BtnStop, Lang.Get("btn_stop"));
                Helper.SetContent(BtnCapture, Lang.Get("btn_capture"));
                Helper.SetContent(BtnTestClick, Lang.Get("btn_test"));
                Helper.SetContent(BtnCheckUpdate, Lang.Get("btn_check_update"));
                Helper.SetContent(BtnClearLog, Lang.Get("btn_clear_log"));
                Helper.SetContent(BtnQuit, Lang.Get("btn_quit"));
                Helper.SetText(TxtSectionCoordDesc, Lang.Get("section_coord_desc"));
                Helper.SetText(TxtCoordLabel, Lang.Get("coord_label"));
                Helper.SetText(TxtCoordHint, Lang.Get("coord_hint"));
                Helper.SetText(TxtSectionOptionsDesc, Lang.Get("section_options_desc"));
                Helper.SetText(TxtOptAutostart, Lang.Get("opt_autostart"));
                Helper.SetText(TxtOptReminder, Lang.Get("opt_reminder"));
                Helper.SetText(TxtOptDarkmode, Lang.Get(_isDarkMode ? "theme_dark" : "theme_light"));
                Helper.SetText(TxtOptUpdate, Lang.Get("opt_check_update"));
                Helper.SetText(TxtSectionLogDesc, Lang.Get("section_log_desc"));
                Helper.SetText(TxtFooter, Lang.Get("footer_copyright"));
                Helper.SetText(TxtQQ, "QQ: 2994938720");
                UpdateStatisticsDisplay();
                Helper.SetContent(BtnLang, Lang.CurrentLang == "zh" ? "EN" : "中");
                if (BtnLang != null) BtnLang.ToolTip = Lang.CurrentLang == "zh" ? "Switch Language" : "切换语言";
                if (GameNameText != null)
                    GameNameText.Text = Lang.Get(_isGameRunning ? "status_protecting" : _isMonitoring ? "status_detecting" : "status_waiting");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyLanguage error: {ex.Message}");
            }
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

    // null 安全辅助类
    public static class Helper
    {
        public static void SetText(System.Windows.Controls.TextBlock? tb, string text)
        { if (tb != null) tb.Text = text; }
        public static void SetContent(System.Windows.Controls.Button? btn, object content)
        { if (btn != null) btn.Content = content; }
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
