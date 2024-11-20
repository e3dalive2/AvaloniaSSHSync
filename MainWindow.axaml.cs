using System;
using System.Text.Json.Nodes;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using Websocket.Client;


namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        private Timer _timer;
        private double _progressValue1 = 0;
        private double _progressValue2 = 50;

        private StackPanel _stackPanel;

        WebsocketClient ws;

        public MainWindow()
        {
            InitializeComponent();

            StartProgressUpdate();

            _stackPanel = MainStackPanel;
            AddProgressBars(4);

            String uri = "ws://localhost:27059/Main";
            ws = new WebsocketClient(new Uri(uri));
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

            /*ws.OnError += (sender, e) =>
            {
                try
                {
                    onError(e.Message);
                }
                catch (Exception ex)
                {
                    onError(ex.Message);
                }
            };
            ws.OnMessage += (sender, e) =>
            {
                try
                {
                    onWebSocketMessage(e.Data);
                }
                catch (Exception ex)
                {
                    onError(ex.Message);
                }
            };*/
        }

        public void onError(String err)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine(err);
            });
        }

        public void onInfo(String info)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine(info);
            });
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
            _timer = new Timer(1000/* / 60*/); // 60 times a second
            _timer.Elapsed += UpdateProgressBars;
            _timer.Start();
        }

        private void UpdateProgressBars(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                onInfo("status: "+ws.IsRunning);

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