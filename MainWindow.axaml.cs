using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private Timer? _timer = null;

        private StackPanel _stackPanel;
        private StackPanel _progressPanel;

        private WebsocketClient? ws=null;

        struct BarInfo
        {
            public ProgressBar bar;
            public Stopwatch lastUpdate;
        }
        private ConcurrentDictionary<int, BarInfo> bars = new ConcurrentDictionary<int, BarInfo>();

        public MainWindow()
        {
            InitializeComponent();

            StartProgressUpdate();

            MainWindow1.Background = new SolidColorBrush(Color.FromRgb(127, 127, 127));

            _stackPanel = MainStackPanel;
            _progressPanel = StackProgressPanel;

            String uri = "ws://localhost:27059/Main";
            Input1.Text = uri;
            Input1.KeyUp += Input1_KeyUp; ;

            ws = new WebsocketClient(new Uri(uri));
            ws.ReconnectTimeout = TimeSpan.FromSeconds(5);
            ws.LostReconnectTimeout = TimeSpan.FromSeconds(5);
            ws.ReconnectTimeout= TimeSpan.FromSeconds(5);
            ws.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);

            onInfo("Connecting " + uri);

            ws.MessageReceived.Subscribe(msg =>
            {
                if (msg == null) return;
                try
                {
                    onWebSocketMessage(msg.Text);
                }
                catch (Exception ex)
                {
                    onError(ex.Message);
                }
            });
            ws.DisconnectionHappened.Subscribe(info =>
            {
                onError(info.ToString());
                ClearAll();
            });
            ws.Start();

        }

        private void Input1_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if(e.Key==Avalonia.Input.Key.Enter)
            {
                ws.Url = new Uri(Input1.Text);
                ws.Reconnect();
            }
        }


        public void onError(String err)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine(err);
                uiAddLine(err);
            });
        }

        void ClearAll()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _stackPanel.Children.Clear();
                _progressPanel.Children.Clear();
                bars.Clear();
            });
        }
            

        public void uiAddLine(String line)
        {
            var textBlock = new TextBlock
            {
                Text = line,
                FontSize = 16,               // Set font size
                //Margin = new Thickness(10),  // Add some margin
                Foreground = Brushes.Black,    // Set text color
                Background=Brushes.LightGray,
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

        public void removeBar(ProgressBar bar)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _progressPanel.Children.Remove(bar);
            });
        }

        public void addUpdateProgressBar(int id,double progress,String msg)
        {
            if(!bars.TryGetValue(id, out var barNfo))
            {
                var bar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = progress,
                    Height = 20,
                    Margin = new Thickness(0, 10, 0, 0),
                    //Foreground = id % 2 == 0 ? Brushes.Green : Brushes.Blue,
                    //Background=Brushes.LightGray,
                    Foreground= id % 2==0?Brushes.Orange:Brushes.ForestGreen,
                    
                    ShowProgressText = false,
                };
                barNfo.bar= bar;
                barNfo.lastUpdate = Stopwatch.StartNew();
                bars[id] = barNfo;
                _progressPanel.Children.Add(bar);
            }

            barNfo.lastUpdate.Restart();

            bool isNoProgress = progress < 0;
            if (isNoProgress) progress = 0;
            {
                var bar = barNfo.bar;
                bar.ShowProgressText = true;
                bar.Value = progress;

                if (progress < 30)
                {
                    bar.Background = Brushes.LightPink;  // Early progress - Light Pink
                }
                else if (progress < 70)
                {
                    bar.Background = Brushes.LightYellow;  // Mid progress - Light Yellow
                }
                else
                {
                    bar.Background = Brushes.LightGreen;  // Near completion - Light Green
                }

                if (isNoProgress) bar.ProgressTextFormat = msg;
                else bar.ProgressTextFormat = msg + " {0:F2}%";
            }
        }

        public void onWebSocketMessage(String msg)
        {
            JObject? js = JObject.Parse(msg);
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
            _timer.Elapsed += OnUpdate;
            _timer.Start();
        }

        private void OnUpdate(object? sender, ElapsedEventArgs e)
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

                int maxRecords = 1000;
                while(_stackPanel.Children.Count> maxRecords)
                {
                    _stackPanel.Children.RemoveRange(maxRecords, _stackPanel.Children.Count - maxRecords);
                }

                foreach(var barInfo in bars.ToImmutableList())
                {
                    if (barInfo.Value.lastUpdate.ElapsedMilliseconds > 1000)
                    {
                        removeBar(barInfo.Value.bar);//remove from ui
                        bars.TryRemove(barInfo);
                    }
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