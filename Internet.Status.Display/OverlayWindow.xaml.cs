using Microsoft.Expression.Shapes;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Internet.Status.Display
{
    /// <summary>
    /// Interaktionslogik für OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private Color colorGreen = Color.FromRgb(68, 206, 72);
        private Color colorYellow = Color.FromRgb(229, 201, 41);
        private Color colorRed = Color.FromRgb(234, 42, 32);

        private Dictionary<string, Arc> hosts;
        private Dictionary<string, PingStatusResult> lastPings;
        private DispatcherTimer timer;

        private string selectedHost = string.Empty;
        private string lastSelectedHost = string.Empty;

        public OverlayWindow()
        {
            InitializeComponent();

            if (Config.Hosts.Count == 0)
            {
                new OptionWindow().ShowDialog();
            }

            this.Reload();
        }

        public void Reload()
        {
            this.AttachToScreenCorner(Config.Corner);

            if (timer != null) { timer.Stop(); }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(Config.PingInterval);
            timer.Tick += Timer_Tick;

            hosts = new Dictionary<string, Arc>();
            lastPings = new Dictionary<string, PingStatusResult>();
            grid.Children.Clear();

            //Create arcs
            if (Config.Hosts.Count == 0)
            {
                return;
            }

            int angle = 360 / Config.Hosts.Count;
            int angleSum = 0;

            for (int i = 0; i < Config.Hosts.Count; i++)
            {
                string h = Config.Hosts[i];
                Arc a = new Arc() { ArcThickness = 1, ArcThicknessUnit = Microsoft.Expression.Media.UnitType.Percent, Stretch = Stretch.None, Fill = new SolidColorBrush(Colors.White) };

                a.StartAngle = i * angle;
                a.EndAngle = (i + 1) * angle;

                a.Tag = h;

                a.MouseEnter += A_MouseEnter;

                angleSum += (i + 1) * angle;

                if (i + 1 == Config.Hosts.Count)
                {
                    if (angleSum > 360 || angleSum < 360)
                    {
                        a.EndAngle = 360;
                    }
                }

                grid.Children.Add(a);

                hosts.Add(h, a);
                lastPings.Add(h, null);
            }

            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            this.UpdateInfoLabel();

            List<Task<PingStatusResult>> tasks = new List<Task<PingStatusResult>>();

            foreach (string h in hosts.Keys)
            {
                tasks.Add(DoPing(h));
            }

            while (tasks.Count > 0)
            {
                Task<PingStatusResult> task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                string h = task.Result.Host;

                if (hosts.ContainsKey(h))
                {
                    switch (task.Result.Status)
                    {
                        case PingStatus.Green:
                            hosts[h].Fill = new SolidColorBrush(colorGreen);
                            break;
                        case PingStatus.Red:
                            hosts[h].Fill = new SolidColorBrush(colorRed);
                            break;
                        case PingStatus.Yellow:
                            hosts[h].Fill = new SolidColorBrush(colorYellow);
                            break;
                    }
                }

                lastPings[h] = task.Result;
            }
        }

        private async Task<PingStatusResult> DoPing(string h)
        {
            PingReply result = null;

            Ping ping = new Ping();
            result = await Task.Run(() =>
            {
                try
                {
                    return ping.Send(h);
                }
                catch (Exception)
                {
                    return null;
                }
            });

            if (result == null || result.Status != IPStatus.Success)
            {
                return await Task.FromResult<PingStatusResult>(new PingStatusResult(h, PingStatus.Red, 0));
            }

            long time = result.RoundtripTime;
            if (time <= Config.PingGreen)
            {
                return await Task.FromResult<PingStatusResult>(new PingStatusResult(h, PingStatus.Green, time));
            }
            else if (time <= Config.PingYellow)
            {
                return await Task.FromResult<PingStatusResult>(new PingStatusResult(h, PingStatus.Yellow, time));
            }
            else
            {
                return await Task.FromResult<PingStatusResult>(new PingStatusResult(h, PingStatus.Red, time));
            }
        }

        public void AttachToScreenCorner(ScreenCorner c = ScreenCorner.BottomRight)
        {
            Size r = SystemParameters.WorkArea.Size;

            if (c == ScreenCorner.TopLeft)
            {
                this.Left = Config.ScreenEdgePadding;
                this.Top = Config.ScreenEdgePadding;
            }
            else if (c == ScreenCorner.TopRight)
            {
                this.Left = r.Width - this.Width - Config.ScreenEdgePadding;
                this.Top = Config.ScreenEdgePadding;
            }
            else if (c == ScreenCorner.BottomLeft)
            {
                this.Left = Config.ScreenEdgePadding;
                this.Top = r.Height - this.Height - Config.ScreenEdgePadding;
            }
            else if (c == ScreenCorner.BottomRight)
            {
                this.Left = r.Width - this.Width - Config.ScreenEdgePadding;
                this.Top = r.Height - this.Height - Config.ScreenEdgePadding;
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            new OptionWindow().ShowDialog();

            this.Reload();
        }

        #region Info Label
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            selectedHost = string.Empty;
            BackgroundOpacity = 0;
            this.UpdateInfoLabel();
        }
        private void UpdateInfoLabel()
        {
            if (selectedHost == string.Empty)
            {
                txtInfo.Text = "";
            }
            else
            {
                txtInfo.Text = selectedHost + "\n" + (lastPings[selectedHost] == null ? "-" : lastPings[selectedHost].Time + " ms");


                if (lastPings[selectedHost] == null)
                {
                    lastPings[selectedHost] = new PingStatusResult(selectedHost, PingStatus.Red);
                }
                else if (lastPings[selectedHost].Time <= Config.PingGreen)
                {
                    txtInfo.Foreground = new SolidColorBrush(colorGreen);
                }
                else if (lastPings[selectedHost].Time <= Config.PingYellow)
                {
                    txtInfo.Foreground = new SolidColorBrush(colorYellow);
                }
                else
                {
                    txtInfo.Foreground = new SolidColorBrush(colorRed);
                }
            }

            //Animation steuern
            if (lastSelectedHost == String.Empty && selectedHost != String.Empty)
            {
                BackgroundOpacity = Config.OverlayOpacityWhilePermanent;
            }
            else if (lastSelectedHost != String.Empty && selectedHost == String.Empty)
            {
                BackgroundOpacity = 0;
            }
            else if(lastSelectedHost == String.Empty && selectedHost == String.Empty)
            {
                BackgroundOpacity = 0;
            }
            else if(lastSelectedHost != String.Empty && selectedHost != String.Empty)
            {
                BackgroundOpacity = Config.OverlayOpacityWhilePermanent;
            }

            //Aktualisierung um Änderungen zu erkennen
            lastSelectedHost = selectedHost;
        }
        private void A_MouseEnter(object sender, MouseEventArgs e)
        {
            Arc a = sender as Arc;
            selectedHost = a.Tag as string;

            this.UpdateInfoLabel();
        }
        private double BackgroundOpacity
        {
            set
            {
                if ((this.Background as SolidColorBrush).Opacity != value)
                {
                    DoubleAnimation animation = new DoubleAnimation(this.Background.Opacity, value, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.HoldEnd);
                    this.Background.BeginAnimation(Brush.OpacityProperty, animation);
                }
            }
        }
        #endregion
    }

    public enum ScreenCorner
    {
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 0,
        BottomRight = 1
    }
    public enum PingStatus
    {
        Green,
        Yellow,
        Red
    }

    public class PingStatusResult
    {
        private string host;
        private PingStatus status;
        private long time;

        public PingStatusResult(string host, PingStatus status)
        {
            this.host = host;
            this.status = status;
        }

        public PingStatusResult(string host, PingStatus status, long time)
        {
            this.host = host;
            this.status = status;
            this.time = time;
        }

        public string Host
        {
            get
            {
                return host;
            }

            set
            {
                host = value;
            }
        }

        public PingStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
            }
        }

        public long Time
        {
            get
            {
                return time;
            }

            set
            {
                time = value;
            }
        }
    }
}
