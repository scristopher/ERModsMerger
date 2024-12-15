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

namespace ERModsManager.UCs
{
    /// <summary>
    /// Logique d'interaction pour HelpUC.xaml
    /// </summary>
    public partial class HelpUC : UserControl
    {
        public HelpUC()
        {
            InitializeComponent();
        }

        private void BtnNexus_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.nexusmods.com/eldenring/mods/5441",
                UseShellExecute = true
            });
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/MadTekN1/ERModsMerger",
                UseShellExecute = true
            });
        }

        private void BtnDiscord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/servername",
                UseShellExecute = true
            });
        }

        private void BtnSoulsMods_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/soulsmods",
                UseShellExecute = true
            });
        }

        private void BtnSmithBox_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/vawser/Smithbox",
                UseShellExecute = true
            });
        }

        private void BtnUXM_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nordgaren/UXM-Selective-Unpack",
                UseShellExecute = true
            });
        }
    }
}
