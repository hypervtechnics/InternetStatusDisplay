using Microsoft.Expression.Shapes;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private bool lockMode = false;

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

            this.UpdateNotifyIcon();
        }

        private async Task<PingStatusResult> DoPing(string h)
        {
            PingReply result = null;

            Ping ping = new Ping();
            result = await Task.Run(() =>
            {
                try
                {
                    return ping.Send(h, Config.PingYellow);
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

        #region Info Label
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!lockMode)
            {
                selectedHost = string.Empty;
                BackgroundOpacity = 0;
                this.UpdateInfoLabel(); 
            }
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
            if (!lockMode)
            {
                Arc a = sender as Arc;
                selectedHost = a.Tag as string;

                this.UpdateInfoLabel(); 
            }
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

        #region NotifyIcon
        private void UpdateNotifyIcon()
        {
            if (notifyIcon == null)
            {
                notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.Visible = true;

                notifyIcon.Click += this.NotifyIcon_Click;
            }

            bool success = false;
            int successfullLastPings = 0;

            foreach (PingStatusResult result in lastPings.Values)
            {
                if (result.Status == PingStatus.Green || result.Status == PingStatus.Yellow)
                {
                    successfullLastPings++;
                }
            }

            if (successfullLastPings >= (lastPings.Count / 2))
            {
                success = true;
            }

            if (success)
            {
                notifyIcon.Icon = Properties.Resources.online;
            }
            else
            {
                notifyIcon.Icon = Properties.Resources.offline;
            }

        }
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem exitButton = new MenuItem()
            {
                Header = "Beenden"
            };
            exitButton.Click += this.ContextMenuExitApp;

            MenuItem settingsButton = new MenuItem()
            {
                Header = "Einstellungen"
            };
            settingsButton.Click += this.ContextMenuSettingsButton;

            MenuItem lockModeButton = new MenuItem()
            {
                Header = lockMode ? "UI entsperren" : "UI sperren",
                IsCheckable = true,
                IsChecked = lockMode
            };
            lockModeButton.Click += this.ContextMenuLockModeButton;

            MenuItem focusOnButton = new MenuItem()
            {
                Header = "Dauerhafte Anzeige von..."
            };
            foreach(string host in hosts.Keys)
            {
                MenuItem hostButton = new MenuItem();
                hostButton.Header = host;
                hostButton.Click += this.ContextMenuHostButton;

                focusOnButton.Items.Add(hostButton);
            }

            menu.Items.Add(lockModeButton);
            menu.Items.Add(focusOnButton);
            menu.Items.Add(settingsButton);
            menu.Items.Add(exitButton);

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            menu.IsOpen = true;
        }

        private void ContextMenuHostButton(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            selectedHost = (string) item.Header;
        }
        private void ContextMenuLockModeButton(object sender, RoutedEventArgs e)
        {
            lockMode = !lockMode;
            (sender as MenuItem).IsChecked = lockMode;
        }
        private void ContextMenuSettingsButton(object sender, RoutedEventArgs e)
        {
            new OptionWindow().ShowDialog();

            this.Reload();
        }
        private void ContextMenuExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.SaveConfig();
            notifyIcon.Dispose();
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
