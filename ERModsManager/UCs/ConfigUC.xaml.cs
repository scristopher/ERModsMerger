using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using ERModsMerger.Core;
using System.Diagnostics;

namespace ERModsManager.UCs
{
    /// <summary>
    /// Logique d'interaction pour ConfigUC.xaml
    /// </summary>
    public partial class ConfigUC : UserControl
    {
        public ConfigUC()
        {
            InitializeComponent();

            if (ModsMergerConfig.LoadedConfig != null)
            {
                LabelGamePath.Content = ModsMergerConfig.LoadedConfig.GamePath;
                labelVersion.Content = "Version: " + ModsMergerConfig.LoadedConfig.ToolVersion;
            }
                
        }

        private void BtnGamePath_Click(object sender, RoutedEventArgs e)
        {
            string path = SearchForEldenRingPath();
            LabelGamePath.Content = path;

            if(ModsMergerConfig.LoadedConfig != null)
                ModsMergerConfig.LoadedConfig.GamePath = path;

            ModsMergerConfig.SaveConfig(Global.ConfigFilePath);
        }

        private void BtnOpenAppData_Click(object sender, RoutedEventArgs e)
        {
            //string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Process.Start("explorer.exe",Global.ERMMAppDataFolder);
        }


        public string SearchForEldenRingPath()
        {
            var dialog = new OpenFileDialog();

            dialog.DefaultExt = ".exe";
            dialog.Multiselect = false;
            dialog.Title = "Where is eldenring.exe?";
            dialog.Filter = "Elden Ring (*.exe)|*.exe";

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                bool goodDir = new DirectoryInfo(dialog.FileName).Parent.GetFiles().Any(x => x.Name == "regulation.bin");

                if (goodDir)
                {
                    return new DirectoryInfo(dialog.FileName).Parent.FullName;
                }
                else
                {
                    MessageBox.Show("This is not Elden Ring Game folder, try again!");
                    return SearchForEldenRingPath();
                }

            }
            else
                MessageBox.Show("Game Path is not set, this will probably not work very good...");

            return "";

        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("You're about to completely reset datas of this tool, every mods dropped here, merges, profiles, config will be lost.\n\nContinue?", "", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (result == MessageBoxResult.Yes)
            {
                Directory.Delete(ModsMergerConfig.LoadedConfig.AppDataFolderPath, true);
                Process.Start(Environment.ProcessPath);
                Application.Current.Shutdown();
            }
           
        }
    }
}
