using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Internet.Status.Display
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class OptionWindow : Window
    {
        public OptionWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Config.Corner = (ScreenCorner) cmbScreenCorner.SelectedIndex;
            Config.Hosts = new List<string>(lbHosts.Items.OfType<string>());
            Config.OverlayOpacityWhilePermanent = slOpacity.Value / 100;

            try
            {
                Config.PingGreen = int.Parse(txtPingGreen.Text);
                Config.PingInterval = int.Parse(txtPingInterval.Text);
                Config.PingYellow = int.Parse(txtPingYellow.Text);
                Config.ScreenEdgePadding = int.Parse(txtEdgePadding.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter valid numbers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Config.SaveConfig();

            this.DialogResult = true;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateHostList();

            txtEdgePadding.Text = "" + Config.ScreenEdgePadding;
            txtPingGreen.Text = "" + Config.PingGreen;
            txtPingYellow.Text = "" + Config.PingYellow;
            txtPingInterval.Text = "" + Config.PingInterval;
            slOpacity.Value = Config.OverlayOpacityWhilePermanent * 100;

            cmbScreenCorner.SelectedIndex = (int) Config.Corner;
        }

        private void UpdateHostList()
        {
            lbHosts.Items.Clear();

            foreach (string s in Config.Hosts)
            {
                lbHosts.Items.Add(s);
            }
        }

        private void btnAddHost_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInputHost.Text))
            {
                lbHosts.Items.Add(txtInputHost.Text);
                txtInputHost.Clear();
            }
        }

        private void btnRemHost_Click(object sender, RoutedEventArgs e)
        {
            if (lbHosts.SelectedIndex >= 0)
            {
                lbHosts.Items.RemoveAt(lbHosts.SelectedIndex);
            }
        }

        private void txtInputHost_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(txtInputHost.Text))
                {
                    lbHosts.Items.Add(txtInputHost.Text);
                    txtInputHost.Clear();
                }
            }
        }
    }
}
