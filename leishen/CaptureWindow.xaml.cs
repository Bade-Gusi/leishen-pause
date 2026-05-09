using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace leishen
{
    public partial class CaptureWindow : Window
    {
        public System.Windows.Point? CapturedPoint { get; private set; }

        public CaptureWindow()
        {
            InitializeComponent();
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
