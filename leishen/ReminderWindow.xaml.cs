using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace leishen
{
    public partial class ReminderWindow : Window
    {
        private int _pauseCount;
        private DateTime _startTime;

        public ReminderWindow(int pauseCount = 0)
        {
            InitializeComponent();
            _pauseCount = pauseCount;
            _startTime = DateTime.Now;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var pulse = Resources["ReminderPulse"] as Storyboard;
            pulse?.Begin(this);

            var btnAnim = Resources["ScaleInButton"] as Storyboard;
            if (btnAnim != null)
            {
                btnAnim.BeginTime = TimeSpan.FromMilliseconds(300);
                btnAnim.Begin(BtnConfirm);
            }

            // Update dynamic info
            TxtPauseTime.Text = $"暂停时间：{DateTime.Now:HH:mm:ss}";
            TxtTodayStats.Text = $"今日已暂停 {_pauseCount} 次";
            TxtDetail.Text = $"已自动暂停雷神加速器 · 节省 {_pauseCount * 60} 秒";

            // Auto-close after 15 seconds
            var closeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            closeTimer.Tick += (s, args) =>
            {
                closeTimer.Stop();
                Close();
            };
            closeTimer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
