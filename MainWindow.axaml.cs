using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using Tmds.DBus.Protocol;
using Websocket.Client;
using static System.Net.Mime.MediaTypeNames;


namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        private Timer _timer;
        private double _progressValue1 = 0;
        private double _progressValue2 = 50;

        private StackPanel _stackPanel;
        private StackPanel _progressPanel;

        WebsocketClient ws;

        Dictionary<int, ProgressBar> bars = new Dictionary<int, ProgressBar>();

        public MainWindow()
        {
            InitializeComponent();

            StartProgressUpdate();

            _stackPanel = MainStackPanel;
            _progressPanel = StackProgressPanel;

            String uri = "ws://localhost:27059/Main";
            ws = new WebsocketClient(new Uri(uri));
            ws.ReconnectTimeout = TimeSpan.FromSeconds(5);
            ws.LostReconnectTimeout = TimeSpan.FromSeconds(5);

            onInfo("Connecting " + uri);
            //ws.Connect();

            ws.MessageReceived.Subscribe(msg =>
            {
                try
                {
                    onWebSocketMessage(msg.Text);
                }
                catch (Exception ex)
                {
                    onError(ex.Message);
                }
            });
            ws.Start();

        }

        public void onError(String err)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine(err);
                uiAddLine(err);
            });
        }

        public void uiAddLine(String line)
        {
            var textBlock = new TextBlock
            {
                Text = line,
                FontSize = 16,               // Set font size
                //Margin = new Thickness(10),  // Add some margin
                Foreground = Brushes.Blue    // Set text color
            };
            _stackPanel.Children.Insert(0,textBlock);
        }

        public void onInfo(String info)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine(info);
                uiAddLine(info);
            });
        }

        public void addUpdateProgressBar(int id,double progress,String msg)
        {
            if(!bars.TryGetValue(id, out var bar))
            {
                bar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = progress,
                    Height = 20,
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = id % 2 == 0 ? Brushes.Green : Brushes.Blue,
                    ShowProgressText = false,
                };
                bars[id] = bar;
                _progressPanel.Children.Add(bar);
            }
            if (progress < 0) progress = 0;
            bar.ShowProgressText = true;
            bar.Value = progress;
            bar.ProgressTextFormat = msg+ "{0:F2}%";
        }

        public void onWebSocketMessage(String msg)
        {
            JObject js = JObject.Parse(msg);
            if (!js.ContainsKey("type"))
            {
                onError("invalid json " + msg);
                return;
            }
            String type = js["type"].ToString();
            if (type == "message")
            {
                String message = js["message"].ToString();
                onInfo(message);
            }
            else if (type == "progress")
            {
                String message = js["message"].ToString();
                double tick = (double)js["progress"];
                int id = (int)js["id"];
                //onInfo(msg);
                Dispatcher.UIThread.InvokeAsync(() => addUpdateProgressBar(id, tick, message));
            }
            else if (type == "progress_update")
            {
                String message = js["message"].ToString();
                int id = (int)js["id"];
                Dispatcher.UIThread.InvokeAsync(() => addUpdateProgressBar(id, -1, message));
            }
        }

        

        private void StartProgressUpdate()
        {
            _timer = new Timer(1000/* / 60*/); // 60 times a second
            _timer.Elapsed += UpdateProgressBars;
            _timer.Start();
        }

        private void UpdateProgressBars(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!ws.IsRunning)
                {
                    onInfo("ws status: " + ws.IsRunning);
                }

                // Update the progress bar values
                /*_progressValue1 = (_progressValue1 + 1) % 101; // Loop from 0 to 100
                _progressValue2 = (_progressValue2 + 0.5) % 101; // Loop from 0 to 100

                ProgressBar1.Value = _progressValue1;
                ProgressBar2.Value = _progressValue2;*/

                while(_stackPanel.Children.Count>100)
                {
                    _stackPanel.Children.RemoveRange(100, _stackPanel.Children.Count - 100);
                }

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