using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace leishen
{
    public partial class ReminderWindow : Window
    {
        private int _pauseCount;

        public ReminderWindow(int pauseCount = 0)
        {
            InitializeComponent();
            _pauseCount = pauseCount;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLanguage();

            var pulse = Resources["ReminderPulse"] as Storyboard;
            pulse?.Begin(this);

            var btnAnim = Resources["ScaleInButton"] as Storyboard;
            if (btnAnim != null)
            {
                btnAnim.BeginTime = TimeSpan.FromMilliseconds(300);
                btnAnim.Begin(BtnConfirm);
            }

            TxtPauseTime.Text = $"{Lang.Get("reminder_time")}：{DateTime.Now:HH:mm:ss}";
            TxtTodayStats.Text = $"{Lang.Get("reminder_today")} {_pauseCount} 次";

            var closeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            closeTimer.Tick += (s, args) => { closeTimer.Stop(); Close(); };
            closeTimer.Start();
        }

        private void ApplyLanguage()
        {
            if (TxtReminderTitle != null) TxtReminderTitle.Text = Lang.Get("reminder_title");
            if (TxtReminderSub != null) TxtReminderSub.Text = Lang.Get("reminder_subtitle");
            if (TxtReminderDetail != null) TxtReminderDetail.Text = Lang.Get("reminder_detail");
            if (BtnConfirm != null) BtnConfirm.Content = Lang.Get("reminder_btn");
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
