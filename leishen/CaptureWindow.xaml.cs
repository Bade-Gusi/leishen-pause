using System;
using System.Windows;
using System.Windows.Input;

namespace leishen
{
    public partial class CaptureWindow : Window
    {
        public System.Windows.Point? CapturedPoint { get; private set; }

        public CaptureWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            if (TxtCaptureHint != null)
                TxtCaptureHint.Text = $"🎯 {Lang.Get("capture_hint")}";
            if (TxtCaptureEsc != null)
                TxtCaptureEsc.Text = Lang.Get("capture_esc");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CapturedPoint = PointToScreen(e.GetPosition(this));
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
