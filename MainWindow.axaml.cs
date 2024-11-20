using System;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        private Timer _timer;
        private double _progressValue1 = 0;
        private double _progressValue2 = 50;

        private StackPanel _stackPanel;
        public MainWindow()
        {
            InitializeComponent();

            StartProgressUpdate();

            _stackPanel = MainStackPanel;
            AddProgressBars(4);

        }


        private void AddProgressBars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var newProgressBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = i * 20, // Example: Assign an initial value
                    Height = 20,
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = i % 2 == 0 ? Brushes.Green : Brushes.Blue,
                    ShowProgressText=true
                };

                // Optionally set a name if you need to reference it later
                newProgressBar.Name = $"DynamicProgressBar{i}";

                // Add the ProgressBar to the StackPanel
                _stackPanel.Children.Insert(0,newProgressBar);
            }
        }

        private void StartProgressUpdate()
        {
            _timer = new Timer(1000 / 60); // 60 times a second
            _timer.Elapsed += UpdateProgressBars;
            _timer.Start();
        }

        private void UpdateProgressBars(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Update the progress bar values
                /*_progressValue1 = (_progressValue1 + 1) % 101; // Loop from 0 to 100
                _progressValue2 = (_progressValue2 + 0.5) % 101; // Loop from 0 to 100

                ProgressBar1.Value = _progressValue1;
                ProgressBar2.Value = _progressValue2;*/

            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}