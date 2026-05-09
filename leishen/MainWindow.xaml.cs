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
using WpfBorder = System.Windows.Controls.Border;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfControl = System.Windows.Controls.Control;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfContextMenu = System.Windows.Controls.ContextMenu;

namespace leishen
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
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
        private WindowInteropHelper? _windowHelper;

        // 颜色缓存（主题切换用）
        private Color _bg, _card, _border, _textMain, _textSec, _textMuted;
        private Color _accentDark = Color.FromRgb(0x00, 0xC8, 0x53);
        private Color _accentLight = Color.FromRgb(0x66, 0xBB, 0x6A);

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
            UpdateColors();
            ApplyTheme();
            ApplyLanguage();
            AddLog(Lang.Get("log_startup"));
        }

        private void UpdateColors()
        {
            Color Sc(string hex, Color fb) { try { var o = ColorConverter.ConvertFromString(hex); return o is Color c ? c : fb; } catch { return fb; } }
            _bg = Sc(_isDarkMode ? "#1A1A2E" : "#F5F0EB", Color.FromRgb(0x1A, 0x1A, 0x2E));
            _card = Sc(_isDarkMode ? "#16213E" : "#FFFFFF", Color.FromRgb(0x16, 0x21, 0x3E));
            _border = Sc(_isDarkMode ? "#2A2A4A" : "#DDD5CC", Color.FromRgb(0x2A, 0x2A, 0x4A));
            _textMain = Sc(_isDarkMode ? "#E8E8E8" : "#2D2D2D", Color.FromRgb(0xE8, 0xE8, 0xE8));
            _textSec = Sc(_isDarkMode ? "#667788" : "#888888", Color.FromRgb(0x66, 0x77, 0x88));
            _textMuted = Sc(_isDarkMode ? "#556677" : "#AAAAAA", Color.FromRgb(0x55, 0x66, 0x77));
        }

        private Color Accent => _isDarkMode ? _accentDark : _accentLight;

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
            // 自动开始监控
            StartMonitoring();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (_windowHelper != null)
                UnregisterHotKey(_windowHelper.Handle, HOTKEY_ID);
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
            SafeUI(() => {
                if (StatusText != null) StatusText.Text = Lang.Get(textKey);
                var c = (Color)ColorConverter.ConvertFromString(colorHex);
                if (StatusText != null) StatusText.Foreground = new SolidColorBrush(c);
                if (StatusIcon != null) StatusIcon.Text = icon;
                var rc = new SolidColorBrush(c);
                if (StatusRing != null) StatusRing.Stroke = rc;
                if (StatusGlow != null) StatusGlow.Fill = rc;
                if (StatusDot != null) StatusDot.Fill = rc;
                var pulse = Resources["PulseGlow"] as Storyboard;
                if (pulseRing) { pulse?.Begin(StatusRing); pulse?.Begin(StatusGlow); }
                else { pulse?.Stop(StatusRing); pulse?.Stop(StatusGlow);
                    if (StatusRing != null) StatusRing.Opacity = 0.3;
                    if (StatusGlow != null) StatusGlow.Opacity = 0.12; }
            });
        }

        private void StartSpinner() { SafeUI(() => { SpinnerBorder.Visibility = Visibility.Visible; (Resources["SpinAnimation"] as Storyboard)?.Begin(Spinner); }); }
        private void StopSpinner() { SafeUI(() => { (Resources["SpinAnimation"] as Storyboard)?.Stop(Spinner); SpinnerBorder.Visibility = Visibility.Collapsed; }); }

        private void SafeUI(Action a) { try { if (!Dispatcher.CheckAccess()) Dispatcher.Invoke(a); else a(); } catch (Exception ex) { Debug.WriteLine(ex.Message); } }

        // ======================== 监控 ========================
        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += async (s, ev) => await CheckProcessAsync();
            _timer.Start();
            SafeUI(() => {
                if (BtnStop != null) { BtnStop.Background = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); BtnStop.Foreground = Brushes.White; }
            });
            SetStatus("status_scanning", "#00E5FF", "⟳"); StartSpinner();
            SafeUI(() => { if (GameNameText != null) GameNameText.Text = Lang.Get("status_detecting"); });
            AddLog(Lang.Get("log_monitor_started"));
        }

        public void StopMonitor()
        {
            _isMonitoring = false;
            if (_timer != null) { _timer.Stop(); _timer = null; }
            SafeUI(() => {
                if (BtnStop != null) {
                    BtnStop.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44));
                    BtnStop.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0x88));
                }
            });
            SetStatus("status_idle", "#00C853", "⏸"); StopSpinner();
            SafeUI(() => { if (GameNameText != null) GameNameText.Text = Lang.Get("status_stopped"); });
            AddLog(Lang.Get("log_monitor_stopped"));
        }

        private async Task CheckProcessAsync()
        {
            bool found = await Task.Run(() => { try { return Process.GetProcessesByName("TslGame").Any(); } catch { return false; } });
            SafeUI(() =>
            {
                if (found)
                {
                    if (!_isGameRunning)
                    {
                        _isGameRunning = true; StopSpinner();
                        SetStatus("status_gaming", "#FF1744", "▶", true);
                        SafeUI(() => { if (GameNameText != null) { GameNameText.Text = Lang.Get("status_protecting"); GameNameText.Foreground = Brushes.OrangeRed; } });
                        AddLog(Lang.Get("log_game_detected"));
                        _sessionCount++; SaveStatistics();
                    }
                }
                else if (_isGameRunning)
                {
                    _isGameRunning = false;
                    _todayPauseCount++; _totalPauseCount++;
                    SetStatus("status_paused", "#00C853", "✓"); StopSpinner();
                    SafeUI(() => { if (GameNameText != null) GameNameText.Text = Lang.Get("status_game_closed"); });
                    AddLog($"{Lang.Get("log_game_closed")} (今日第{_todayPauseCount}次)");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        DoPause();
                        await Task.Delay(300);
                        SafeUI(() => { UpdateStatisticsDisplay(); SaveStatistics();
                            if (ChkShowReminder != null && ChkShowReminder.IsChecked == true) ShowReminderWindow(); });
                    });
                }
            });
        }

        // ======================== 暂停 ========================
        private void DoPause()
        {
            string[] titles = { "雷神加速器", "雷神加速器 - 加速游戏", "LeiShen", "雷神" };
            IntPtr hwnd = IntPtr.Zero;
            foreach (string t in titles) { hwnd = FindWindow(null, t); if (hwnd != IntPtr.Zero) break; }
            if (hwnd == IntPtr.Zero) { AddLog(Lang.Get("log_window_not_found")); return; }

            var titleBuf = new StringBuilder(256);
            GetWindowText(hwnd, titleBuf, titleBuf.Capacity);
            string winTitle = titleBuf.ToString();
            if (!winTitle.Contains("雷神") && !winTitle.Contains("LeiShen"))
            { AddLog($"❌ 安全拦截：目标窗口不是雷神加速器 ({winTitle})"); return; }
            AddLog($"{Lang.Get("log_window_found")} ({winTitle})");

            if (TryUiaClick(hwnd)) return;
            if (TryFindAnyClickable(hwnd)) return;
            AddLog("🖱 使用预设坐标 PostMessage");
            ManualClick(hwnd);
        }

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
                        if (!btn.Current.BoundingRectangle.IsEmpty)
                        {
                            SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, IntPtr.Zero);
                            System.Threading.Thread.Sleep(30);
                            SendMessage(hwnd, WM_LBUTTONUP, (IntPtr)0, IntPtr.Zero);
                            AddLog("✅ PostMessage 点击完成"); return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private bool TryFindAnyClickable(IntPtr hwnd)
        {
            try
            {
                var root = AutomationElement.FromHandle(hwnd);
                if (root == null) return false;
                var all = root.FindAll(TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox)));
                foreach (AutomationElement el in all)
                    if (el.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                    { ((InvokePattern)p).Invoke(); AddLog("✅ InvokePattern 调用成功"); return true; }
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
            AddLog($"📨 PostMessage ({_manualX}, {_manualY})");
            SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            SendMessage(hwnd, WM_LBUTTONUP, (IntPtr)0, IntPtr.Zero);
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
            AddLog("进入鼠标捕获模式");
            // 捕获窗口用全屏覆盖模式，不需要最小化主窗口
            // 直接打开全屏透明捕获窗口，用户点击后自动关闭
            var cw = new CaptureWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            cw.ShowInTaskbar = false;
            // 隐藏主窗口但保持在任务栏
            SafeUI(() => { Opacity = 0; ShowInTaskbar = true; });

            bool? result = null;
            try { result = cw.ShowDialog(); }
            finally { SafeUI(() => { Opacity = 1; ShowInTaskbar = true; Activate(); }); }

            if (result == true && cw.CapturedPoint.HasValue)
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

        private void UpdateCoordDisplay() => SafeUI(() => { if (TxtCoordDisplay != null) TxtCoordDisplay.Text = $"({_manualX}, {_manualY})"; });

        private void LoadConfig() { try { if (File.Exists(_configPath)) { var c = JsonSerializer.Deserialize<CoordConfig>(File.ReadAllText(_configPath)); if (c != null) { _manualX = c.X; _manualY = c.Y; } } } catch { } }
        private void SaveConfig() { try { File.WriteAllText(_configPath, JsonSerializer.Serialize(new CoordConfig { X = _manualX, Y = _manualY })); } catch { } }

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
            SafeUI(() => {
                if (TxtTodayCount != null) TxtTodayCount.Text = _todayPauseCount.ToString();
                int sm = _totalPauseCount * 60;
                if (TxtSavedTime != null) TxtSavedTime.Text = sm >= 60 ? $"{sm / 60}h{sm % 60}m" : $"{sm} {Lang.Get("minutes")}";
                if (TxtVersionInfo != null) TxtVersionInfo.Text = string.Format(Lang.Get("footer_stats"), _sessionCount, _totalPauseCount);
            });
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
                try
                {
                    var proxyUri = new Uri("http://127.0.0.1:7897");
                    var testReq = System.Net.WebRequest.CreateHttp("http://127.0.0.1:7897");
                    testReq.Timeout = 2000; testReq.Method = "GET";
                    using var testResp = (System.Net.HttpWebResponse)await Task.Run(() => (System.Net.HttpWebResponse)testReq.GetResponse());
                    if (testResp.StatusCode == System.Net.HttpStatusCode.OK || testResp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    { handler.Proxy = new System.Net.WebProxy(proxyUri); handler.UseProxy = true; }
                }
                catch { handler.UseProxy = true; handler.Proxy = System.Net.Http.HttpClient.DefaultProxy; }
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

        // ======================== 主题切换（完整覆盖+渐变动画） ========================
        private void ApplyTheme()
        {
            SafeUI(() =>
            {
                if (!_isInitialized) return; // 防止初始化前调用
                UpdateColors();

                // 主容器
                SetColor(MainBorder, WpfBorder.BackgroundProperty, _bg);
                SetColor(TopLine, WpfBorder.BackgroundProperty, Accent);

                // 按钮
                if (BtnThemeToggle != null) BtnThemeToggle.Content = _isDarkMode ? "🌙" : "☀️";
                if (ChkDarkMode != null) ChkDarkMode.IsChecked = _isDarkMode;
                if (TxtDarkIcon != null) TxtDarkIcon.Text = _isDarkMode ? "🌙" : "☀️";
                if (TxtOptDarkmode != null) TxtOptDarkmode.Text = Lang.Get(_isDarkMode ? "theme_dark" : "theme_light");

                // 卡片背景
                foreach (var c in new[] { CardStatus, CardCoord, CardOptions, CardLog })
                    if (c != null) { SetColor(c, WpfBorder.BackgroundProperty, _card); SetColor(c, WpfBorder.BorderBrushProperty, _border); }

                // 选项内嵌背景
                foreach (var b in new[] { CoordInnerBg, Opt1Bg, Opt2Bg, Opt3Bg, Opt4Bg })
                    if (b != null) SetColor(b, WpfBorder.BackgroundProperty, _bg);

                // 标题文字
                foreach (var x in new[] { TxtSectionCoord, TxtSectionOptions, TxtSectionLog,
                    TxtOptAutostart, TxtOptReminder, TxtOptDarkmode, TxtOptUpdate })
                    if (x != null) SetColor(x, WpfTextBlock.ForegroundProperty, _textMain);

                // log list 背景
                if (LogList != null) SetColor(LogList, WpfControl.BackgroundProperty, _bg);
            });
        }

        private bool _isInitialized = false;
        private void MarkInitialized() { _isInitialized = true; }

        private void SetColor(DependencyObject target, DependencyProperty prop, Color toColor)
        {
            if (target == null) return;
            if (prop == WpfBorder.BackgroundProperty && target is WpfBorder b) b.Background = new SolidColorBrush(toColor);
            else if (prop == WpfBorder.BorderBrushProperty && target is WpfBorder br) br.BorderBrush = new SolidColorBrush(toColor);
            else if (prop == WpfTextBlock.ForegroundProperty && target is WpfTextBlock tb) tb.Foreground = new SolidColorBrush(toColor);
            else if (prop == WpfControl.BackgroundProperty && target is WpfControl c) c.Background = new SolidColorBrush(toColor);
        }

        // ======================== 语言选择（弹出列表） ========================
        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            var menu = new WpfContextMenu();
            foreach (var lang in Lang.Languages)
            {
                var item = new WpfMenuItem
                {
                    Header = $"{lang.Value} ({(lang.Key == "zh" ? "中文" : lang.Key)})",
                    IsChecked = lang.Key == Lang.CurrentLang,
                    FontWeight = lang.Key == Lang.CurrentLang ? FontWeights.Bold : FontWeights.Normal
                };
                string code = lang.Key;
                item.Click += (s, ev) =>
                {
                    Lang.CurrentLang = code;
                    ApplyLanguage();
                    ApplyTheme();
                    AddLog($"Language: {Lang.Languages[code]}");
                };
                menu.Items.Add(item);
            }
            if (sender is System.Windows.Controls.Button btn)
                menu.PlacementTarget = btn;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ======================== 事件 ========================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.OriginalSource is FrameworkElement fe && fe.Name != "BtnClose" && fe.Name != "BtnLang" && fe.Name != "BtnThemeToggle")
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // × 完全退出
            if (_windowHelper != null) UnregisterHotKey(_windowHelper.Handle, HOTKEY_ID);
            Application.Current.Shutdown();
        }

        private void StopMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (_isMonitoring) StopMonitor();
            else StartMonitoring();
        }

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

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
            ApplyLanguage();
        }

        private void DarkMode_Toggled(object sender, RoutedEventArgs e)
        {
            _isDarkMode = ChkDarkMode?.IsChecked == true;
            ApplyTheme();
            ApplyLanguage();
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

        // ======================== 语言 ========================
        private void ApplyLanguage()
        {
            SafeUI(() =>
            {
                Helper.SetText(TxtAppTitle, Lang.Get("app_title"));
                Helper.SetText(TxtAppSubtitle, $"{(Lang.CurrentLang == "zh" ? "智能时长暂停工具" : "Smart Pause Tool")} · {Lang.Get("app_version")}");
                Helper.SetText(TxtStatusLabel, Lang.Get("status_label"));
                Helper.SetText(TxtTodayLabel, Lang.Get("today_pause"));
                Helper.SetText(TxtSavedLabel, Lang.Get("saved"));
                Helper.SetContent(BtnCapture, Lang.Get("btn_capture"));
                Helper.SetContent(BtnTestClick, Lang.Get("btn_test"));
                Helper.SetContent(BtnCheckUpdate, Lang.Get("btn_check_update"));
                Helper.SetContent(BtnClearLog, Lang.Get("btn_clear_log"));
                Helper.SetText(TxtSectionCoordDesc, Lang.Get("section_coord_desc"));
                Helper.SetText(TxtCoordLabel, Lang.Get("coord_label"));
                Helper.SetText(TxtCoordHint, Lang.Get("coord_hint"));
                Helper.SetText(TxtSectionOptionsDesc, Lang.Get("section_options_desc"));
                Helper.SetText(TxtOptAutostart, Lang.Get("opt_autostart"));
                Helper.SetText(TxtOptReminder, Lang.Get("opt_reminder"));
                Helper.SetText(TxtOptDarkmode, Lang.Get(_isDarkMode ? "theme_dark" : "theme_light"));
                Helper.SetText(TxtOptUpdate, Lang.Get("opt_check_update"));
                Helper.SetText(TxtSectionLogDesc, Lang.Get("section_log_desc"));
                Helper.SetText(TxtSectionCoord, Lang.Get("section_coord"));
                Helper.SetText(TxtSectionOptions, Lang.Get("section_options"));
                Helper.SetText(TxtSectionLog, Lang.Get("section_log"));
                Helper.SetText(TxtFooter, Lang.Get("footer_copyright"));
                Helper.SetText(TxtQQ, "QQ: 2994938720");
                Helper.SetText(TxtOptAutostart, Lang.Get("opt_autostart"));
                Helper.SetText(TxtOptReminder, Lang.Get("opt_reminder"));
                Helper.SetText(TxtOptUpdate, Lang.Get("opt_check_update"));
                UpdateStatisticsDisplay();
                if (StatusText != null)
                    StatusText.Text = Lang.Get(_isGameRunning ? "status_gaming" : _isMonitoring ? "status_scanning" : "status_idle");
                if (GameNameText != null)
                    GameNameText.Text = Lang.Get(_isGameRunning ? "status_protecting" : _isMonitoring ? "status_detecting" : "status_waiting");
                if (BtnStop != null) BtnStop.Content = _isMonitoring ? Lang.Get("btn_stop") : Lang.Get("btn_start");
            });
        }
    }

    public static class Helper
    {
        public static void SetText(System.Windows.Controls.TextBlock? tb, string text) { if (tb != null) tb.Text = text; }
        public static void SetContent(System.Windows.Controls.Button? btn, object content) { if (btn != null) btn.Content = content; }
    }

    public class CoordConfig { public int X { get; set; } public int Y { get; set; } }
    public class AppStatistics { public int TodayCount { get; set; } public int TotalCount { get; set; } public int SessionCount { get; set; } public string? LastDate { get; set; } }
    public class GitHubRelease { public string? TagName { get; set; } public string? Body { get; set; } public string? HtmlUrl { get; set; } public List<GitHubAsset>? Assets { get; set; } }
    public class GitHubAsset { public string? Name { get; set; } public string? BrowserDownloadUrl { get; set; } }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public struct POINT { public int X; public int Y; }
}
