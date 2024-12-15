using ERModsMerger.Core;
using ERModsMerger.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
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
    /// Logique d'interaction pour FileTreeUC.xaml
    /// </summary>
    public partial class FileTreeUC : UserControl
    {
        public bool FileEnabled { get; set; }

        private string _currentPath = string.Empty;

        public FileTreeUC? ParentFileTree { get; set; }
        public List<FileTreeUC> SonsFileTrees { get; set; }

        public List<FileToMerge> Paths { get; set; }

        public FileToMerge? Path { get; set; }



        public FileTreeUC()
        {
            InitializeComponent();
            Paths = new List<FileToMerge>();
            SonsFileTrees = new List<FileTreeUC>();
            DataContext = this;

            FileEnabled = true;
        }

        public void Load(string folderPath, bool showHeadFolder , ModConfig modConfig)
        {
            _currentPath = folderPath;

            int foundIndex = Paths.FindIndex(x => x.Path == _currentPath);

            if(foundIndex == -1 && showHeadFolder)
            {
                Paths.Add(new FileToMerge(_currentPath, _currentPath.Replace(modConfig.DirPath + "\\", "")));
                Path = Paths.Last();
            }
            else if(showHeadFolder)
            {
                FileEnabled = Paths[foundIndex].Enabled;
                Path = Paths[foundIndex];
            }
                

            if (!showHeadFolder)
            {
                Header.Visibility = Visibility.Hidden;
                StackPanelSubFileTree.Margin = new Thickness(0);
                StackPanelSubFileTree.Height = double.NaN;
            }
                

            List<string> paths = new List<string>();

            Utils.FindAllFiles(folderPath, ref paths, false, true);


            HeaderName.Content = folderPath.Split("\\").Last();

            foreach (string path in paths)
            {
                FileAttributes attr = File.GetAttributes(path);

                FileTreeUC filetree = new FileTreeUC();
                filetree.Paths = Paths;
                filetree.ParentFileTree = this;
                SonsFileTrees.Add(filetree);

                StackPanelSubFileTree.Children.Add(filetree);

                filetree.Load(path, true, modConfig);

                if (!attr.HasFlag(FileAttributes.Directory)) // path is a directory / folder
                {
                    filetree.imgFolder.Visibility = Visibility.Hidden;
                    filetree.imgFile.Visibility = Visibility.Visible;
                }
            }
        }

        public void FindConflictingFileTrees(List<FileConflict> conflicts)
        {
            HeaderName.Foreground = (Brush)this.FindResource("WindowTextBrush");
            HeaderName.ToolTip = null;


            var conflictFound = conflicts.Find(x => Path != null && x.FilesToMerge[0].ModRelativePath == Path.ModRelativePath);
            if (conflictFound != null && conflictFound.FilesToMerge.Count(x => x.Path == Path.Path) > 0)
            {

                if(conflictFound.SupportedFormat)
                {
                    HeaderName.Foreground = new SolidColorBrush(Colors.Orange);
                    HeaderName.ToolTip = "Supported conflict, will merge:\n\n";
                }
                else
                {
                    HeaderName.Foreground = new SolidColorBrush(Colors.Red);
                    HeaderName.ToolTip = "Unsupported conflict, overwrite will occur:\n\n";
                }
                    
                conflictFound.FilesToMerge.ForEach(x => HeaderName.ToolTip += x.Path.Replace("\\" + x.ModRelativePath, "").Split("\\").Last() + ": " + x.ModRelativePath + "\n");

                var parent = ParentFileTree;
                while (parent != null)
                {
                    parent.HeaderName.Foreground = HeaderName.Foreground;
                    parent.HeaderName.ToolTip = HeaderName.ToolTip;
                    parent = parent.ParentFileTree;
                }
            }
                

            foreach (var son in SonsFileTrees)
                    son.FindConflictingFileTrees(conflicts);
        }

        public FileTreeUC? FindFileByRelativePath(string relativePath, bool returnIfDisabled =false)
        {
            if(_currentPath.Contains(relativePath) && FileEnabled) return this;

            HeaderName.Foreground = (Brush)this.FindResource("WindowTextBrush"); //reset color in case of not correponding

            foreach (var sonFileTree in SonsFileTrees)
            {
                var result = sonFileTree.FindFileByRelativePath(relativePath);
                if (result != null) return result;
            }

            return null;
        }

        private void Header_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(StackPanelSubFileTree.Height != 0)
                StackPanelSubFileTree.Height = 0;
            else
                StackPanelSubFileTree.Height = double.NaN;
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }


        private void SwitchEnableRecursive(bool enabled, bool direct = false)
        {
            CheckBoxEnableFileFolder.IsChecked = enabled;
            foreach (var child in StackPanelSubFileTree.Children)
            {
                ((FileTreeUC)child).SwitchEnableRecursive(enabled);
            }

            //recursive up when enabled
            if(enabled && ParentFileTree != null && !ParentFileTree.FileEnabled)
                ParentFileTree.SwitchEnableRecursiveUp();

            var fileConfig = Paths.Find(x=>x.Path == _currentPath);
            if (fileConfig != null)
            {
                fileConfig.Enabled = enabled;
            }
        }

        private void SwitchEnableRecursiveUp()
        {
            CheckBoxEnableFileFolder.IsChecked = true;
            //recursive up when enabled
            if (ParentFileTree != null && !ParentFileTree.FileEnabled)
                ParentFileTree.SwitchEnableRecursiveUp();

            var fileConfig = Paths.Find(x => x.Path == _currentPath);
            if (fileConfig != null)
            {
                fileConfig.Enabled = true;
            }
        }

        private void CheckBoxEnableFileFolder_Click(object sender, RoutedEventArgs e)
        {
            SwitchEnableRecursive(FileEnabled);
            ModsMergerConfig.LoadedConfig.Save();
        }
    }
}
